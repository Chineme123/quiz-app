using System.IO;
using System.IO.Compression;

namespace Quiztin.Modules.Assessment.Tests
{
    /// <summary>
    /// Builds a minimal .docx (a zip with word/document.xml) in memory for the source extractor
    /// tests. The body text must be XML safe (the callers use plain letters and spaces).
    /// </summary>
    internal static class TestDocx
    {
        public static byte[] Build(string bodyText) => BuildZip("word/document.xml", DocumentXml(bodyText));

        /// <summary>A valid zip that is not a Word document (no word/document.xml).</summary>
        public static byte[] BuildWithoutWordDocument() => BuildZip("notes/readme.txt", "not a word document");

        private static byte[] BuildZip(string entryName, string content)
        {
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = zip.CreateEntry(entryName);
                using var writer = new StreamWriter(entry.Open());
                writer.Write(content);
            }
            return ms.ToArray();
        }

        private static string DocumentXml(string bodyText) =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" +
            "<w:body><w:p><w:r><w:t xml:space=\"preserve\">" + bodyText + "</w:t></w:r></w:p></w:body></w:document>";
    }
}
