using System;
using System.Text;
using Microsoft.Extensions.Options;
using Quiztin.Modules.Assessment.Domain.Interfaces;
using Quiztin.Modules.Assessment.Infrastructure.Configuration;
using Quiztin.Modules.Assessment.Infrastructure.Parsing;
using Xunit;

namespace Quiztin.Modules.Assessment.Tests
{
    /// <summary>
    /// The source material extractor (spec 0009, AC-7): format by magic bytes (not extension),
    /// bounded and XXE-safe docx text extraction, and graceful failure on garbage. No DB. PDF
    /// extraction runs on native pdfium and is verified in CI and live upload; here the PDF path's
    /// magic-byte routing and its graceful failure are covered.
    /// </summary>
    public class SourceMaterialExtractorTests
    {
        private static SourceMaterialExtractor Extractor(int maxChars = 10000) =>
            new(Options.Create(new GenerationOptions { MaxSourceChars = maxChars }));

        [Fact]
        public void Extracts_text_from_a_docx()
        {
            var docx = TestDocx.Build("The mitochondria is the powerhouse of the cell.");

            var result = Extractor().Extract(docx);

            Assert.Equal(SourceExtractionStatus.Ok, result.Status);
            Assert.Contains("mitochondria", result.Text);
        }

        [Fact]
        public void Caps_docx_text_at_the_configured_limit()
        {
            var docx = TestDocx.Build(new string('A', 500));

            var result = Extractor(maxChars: 20).Extract(docx);

            Assert.Equal(SourceExtractionStatus.Ok, result.Status);
            Assert.True(result.Text.Length <= 20);
        }

        [Fact]
        public void A_zip_that_is_not_a_word_document_is_unsupported()
        {
            Assert.Equal(SourceExtractionStatus.UnsupportedType,
                Extractor().Extract(TestDocx.BuildWithoutWordDocument()).Status);
        }

        [Fact]
        public void Random_bytes_are_an_unsupported_type()
        {
            Assert.Equal(SourceExtractionStatus.UnsupportedType,
                Extractor().Extract(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }).Status);
        }

        [Fact]
        public void An_empty_file_is_a_parse_failure()
        {
            Assert.Equal(SourceExtractionStatus.ParseFailed, Extractor().Extract(Array.Empty<byte>()).Status);
        }

        [Fact]
        public void A_file_claiming_to_be_pdf_but_corrupt_fails_gracefully()
        {
            // "%PDF-" magic then garbage: routed to the PDF path, which returns ParseFailed rather
            // than throwing, whether or not native pdfium loads on this platform.
            var bytes = Encoding.ASCII.GetBytes("%PDF-1.4 then total garbage, not a real pdf at all");
            Assert.Equal(SourceExtractionStatus.ParseFailed, Extractor().Extract(bytes).Status);
        }

        [Fact]
        public void A_decompression_bomb_docx_is_refused()
        {
            // A small zip whose document.xml expands past the decompressed cap: the bounded reader
            // trips before it can exhaust memory, so it fails rather than hangs or OOMs.
            var bomb = TestDocx.Build(new string('A', 2_000_000));
            Assert.Equal(SourceExtractionStatus.ParseFailed, Extractor().Extract(bomb).Status);
        }
    }
}
