namespace Quiztin.Modules.Assessment.Domain.Interfaces
{
    /// <summary>
    /// Extracts plain text from an uploaded source file for generation (spec 0009, AC-7). The
    /// format is decided by the file's real content (magic bytes), never the client supplied
    /// content type or extension. The extractor is bounded (a text cap and a time limit) so a
    /// decompression bomb cannot exhaust memory or hang, and it stores nothing.
    /// </summary>
    public interface ISourceMaterialExtractor
    {
        SourceExtractionResult Extract(byte[] fileBytes);
    }

    public enum SourceExtractionStatus
    {
        Ok,
        /// <summary>The bytes are neither a PDF nor a docx (by magic bytes). Maps to 415.</summary>
        UnsupportedType,
        /// <summary>A supported type that could not be read (corrupt, empty, or over a bound). Maps to 400.</summary>
        ParseFailed
    }

    public class SourceExtractionResult
    {
        public SourceExtractionStatus Status { get; }
        public string Text { get; }
        public string? Error { get; }

        private SourceExtractionResult(SourceExtractionStatus status, string text, string? error)
        {
            Status = status;
            Text = text;
            Error = error;
        }

        public static SourceExtractionResult Ok(string text) => new(SourceExtractionStatus.Ok, text, null);
        public static SourceExtractionResult UnsupportedType() => new(SourceExtractionStatus.UnsupportedType, string.Empty,
            "Only PDF and Word (docx) files are supported.");
        public static SourceExtractionResult ParseFailed(string error) => new(SourceExtractionStatus.ParseFailed, string.Empty, error);
    }
}
