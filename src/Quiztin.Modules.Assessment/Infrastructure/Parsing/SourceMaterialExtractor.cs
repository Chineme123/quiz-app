using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Docnet.Core;
using Docnet.Core.Models;
using Microsoft.Extensions.Options;
using Quiztin.Modules.Assessment.Domain.Interfaces;
using Quiztin.Modules.Assessment.Infrastructure.Configuration;

namespace Quiztin.Modules.Assessment.Infrastructure.Parsing
{
    /// <summary>
    /// Extracts plain text from an uploaded PDF or docx for generation (spec 0009, AC-7). The
    /// format comes from the file's real content (magic bytes), never the client content type or
    /// extension. PDF goes through Docnet.Core (pdfium). docx is read with the built-in zip and a
    /// bounded, XXE-safe XmlReader over word/document.xml, so no extra dependency and full control
    /// of the bounds. Extraction is bounded three ways: a request-pipeline size cap (elsewhere),
    /// a text cap, a decompressed-byte cap, and a wall-clock time limit, so a decompression bomb
    /// cannot exhaust memory or hang. Nothing is stored.
    /// </summary>
    public class SourceMaterialExtractor : ISourceMaterialExtractor
    {
        private static readonly byte[] PdfMagic = { 0x25, 0x50, 0x44, 0x46, 0x2D };   // %PDF-
        private static readonly byte[] ZipMagic = { 0x50, 0x4B, 0x03, 0x04 };         // PK\x03\x04 (docx is a zip)

        private readonly GenerationOptions _options;

        public SourceMaterialExtractor(IOptions<GenerationOptions> options)
        {
            _options = options.Value;
        }

        public SourceExtractionResult Extract(byte[] fileBytes)
        {
            if (fileBytes is null || fileBytes.Length == 0)
                return SourceExtractionResult.ParseFailed("The uploaded file was empty.");

            var maxChars = Math.Max(1, _options.MaxSourceChars);
            var timeout = TimeSpan.FromSeconds(Math.Max(1, _options.ExtractionTimeoutSeconds));

            // Format from the real bytes, never the client's declared type or extension (AC-7).
            if (StartsWith(fileBytes, PdfMagic))
                return RunBounded(() => ExtractPdf(fileBytes, maxChars), timeout);
            if (StartsWith(fileBytes, ZipMagic))
                return RunBounded(() => ExtractDocx(fileBytes, maxChars), timeout);

            return SourceExtractionResult.UnsupportedType();
        }

        // Wall-clock bound so a pathological file cannot hang the extractor. The work runs on a
        // worker; on overrun we return ParseFailed (the request-pipeline size cap already bounds
        // how large the input can be).
        private static SourceExtractionResult RunBounded(Func<SourceExtractionResult> work, TimeSpan timeout)
        {
            try
            {
                var task = Task.Run(work);
                return task.Wait(timeout)
                    ? task.Result
                    : SourceExtractionResult.ParseFailed("The file took too long to read.");
            }
            catch (Exception)
            {
                return SourceExtractionResult.ParseFailed("The file could not be read.");
            }
        }

        private static SourceExtractionResult ExtractPdf(byte[] bytes, int maxChars)
        {
            try
            {
                var sb = new StringBuilder();
                // DocLib.Instance is a process-wide native singleton; dispose the reader, not it.
                using var reader = DocLib.Instance.GetDocReader(bytes, new PageDimensions(1, 1));
                var pages = reader.GetPageCount();
                for (var i = 0; i < pages && sb.Length < maxChars; i++)
                {
                    using var page = reader.GetPageReader(i);
                    sb.Append(page.GetText());
                    sb.Append('\n');
                }
                return Finish(sb, maxChars, "No readable text was found in the PDF.");
            }
            catch (Exception)
            {
                return SourceExtractionResult.ParseFailed("The PDF could not be read.");
            }
        }

        private static SourceExtractionResult ExtractDocx(byte[] bytes, int maxChars)
        {
            try
            {
                using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
                var entry = zip.GetEntry("word/document.xml");
                if (entry is null)
                    return SourceExtractionResult.UnsupportedType(); // a zip, but not a Word document

                // Cap the decompressed bytes we will read, so a small zip that expands to gigabytes
                // (a decompression bomb) cannot exhaust memory. The wrapper throws past the cap.
                var maxDecompressed = Math.Max(maxChars * 8L, 1_000_000L);
                using var raw = entry.Open();
                using var bounded = new BoundedReadStream(raw, maxDecompressed);

                // XXE-safe: docx is a zip of XML, so refuse DTDs and external entities.
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    IgnoreComments = true,
                    IgnoreProcessingInstructions = true
                };

                var sb = new StringBuilder();
                var inText = false;
                using var xml = XmlReader.Create(bounded, settings);
                while (xml.Read() && sb.Length < maxChars)
                {
                    switch (xml.NodeType)
                    {
                        // Body text lives in <w:t> elements; a self-closing one carries none.
                        case XmlNodeType.Element when xml.LocalName == "t" && !xml.IsEmptyElement:
                            inText = true;
                            break;
                        case XmlNodeType.EndElement when xml.LocalName == "t":
                            inText = false;
                            sb.Append(' ');
                            break;
                        case XmlNodeType.Text when inText:
                            sb.Append(xml.Value);
                            break;
                    }
                }
                return Finish(sb, maxChars, "No readable text was found in the document.");
            }
            catch (Exception)
            {
                return SourceExtractionResult.ParseFailed("The document could not be read.");
            }
        }

        private static SourceExtractionResult Finish(StringBuilder sb, int maxChars, string emptyMessage)
        {
            var text = sb.ToString().Trim();
            if (text.Length > maxChars) text = text.Substring(0, maxChars);
            return string.IsNullOrWhiteSpace(text)
                ? SourceExtractionResult.ParseFailed(emptyMessage)
                : SourceExtractionResult.Ok(text);
        }

        private static bool StartsWith(byte[] data, byte[] prefix)
        {
            if (data.Length < prefix.Length) return false;
            for (var i = 0; i < prefix.Length; i++)
                if (data[i] != prefix[i]) return false;
            return true;
        }

        // A read-only stream that throws once more than a fixed number of bytes has been read, so
        // decompressing a zip entry cannot run away (decompression bomb guard).
        private sealed class BoundedReadStream : Stream
        {
            private readonly Stream _inner;
            private readonly long _max;
            private long _read;

            public BoundedReadStream(Stream inner, long max)
            {
                _inner = inner;
                _max = max;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var n = _inner.Read(buffer, offset, count);
                _read += n;
                if (_read > _max)
                    throw new InvalidOperationException("The document expanded past its allowed size.");
                return n;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => _read; set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
