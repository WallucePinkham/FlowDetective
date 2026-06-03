using System;
using System.IO;
using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class LinkIncludeTests
    {
        [Fact]
        public void HTMLProcessor_IgnoresLinkedCss_ByDefault()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string cssPath = Path.Combine(tempDir, "site.css");
                File.WriteAllText(cssPath, ".a { width: 42px; }");

                string html = $"<html><head><link rel=\"stylesheet\" href=\"site.css\"></head><body></body></html>";
                string htmlPath = Path.Combine(tempDir, "index.html");
                File.WriteAllText(htmlPath, html);

                var results = HTMLProcessor.ProcessFile(htmlPath); // followLinks = false
                Assert.NotNull(results);
                Assert.DoesNotContain(results, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void HTMLProcessor_FollowsLinkedCss_WhenRequested()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string cssPath = Path.Combine(tempDir, "site.css");
                File.WriteAllText(cssPath, ".a { width: 42px; }");

                string html = $"<html><head><link rel=\"stylesheet\" href=\"site.css\"></head><body></body></html>";
                string htmlPath = Path.Combine(tempDir, "index.html");
                File.WriteAllText(htmlPath, html);

                var results = HTMLProcessor.ProcessFile(htmlPath, followLinks: true);
                Assert.NotNull(results);
                Assert.Contains(results, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("42px"));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}