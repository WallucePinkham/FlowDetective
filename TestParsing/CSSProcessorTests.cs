using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class CSSProcessorTests
    {
        [Fact]
        public void CSSProcessor_FindsPxProperties()
        {
            string css = @"
                .cls { margin: 10px; padding: 2px; box-shadow: 0 4px 8px rgba(0,0,0,0.2); }
                .other { width: 100px; }";

            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".css");
            File.WriteAllText(path, css);

            try
            {
                var results = CSSProcessor.ProcessFile(path);
                Assert.NotNull(results);
                Assert.NotEmpty(results);

                // layout-affecting properties expected
                Assert.Contains(results, r => string.Equals(r.Property, "margin", StringComparison.OrdinalIgnoreCase));
                Assert.Contains(results, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase));
                // visual-only should not be returned (existing test expected no box-shadow entry)
                Assert.DoesNotContain(results, r => string.Equals(r.Property, "box-shadow", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void CSSProcessor_NonCssFile_Ignored()
        {
            string text = "div { width: 10px; }";
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(path, text);

            try
            {
                var results = CSSProcessor.ProcessFile(path);
                Assert.NotNull(results);
                Assert.Empty(results);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void CSSProcessor_MissingFile_ThrowsFileNotFoundException()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".css");
            if (File.Exists(path)) File.Delete(path);

            Assert.Throws<FileNotFoundException>(() => CSSProcessor.ProcessFile(path));
        }
    }
}