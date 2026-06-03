using System;
using System.IO;
using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class CliOptionsTests
    {
        [Fact]
        public void Cli_IgnoresLinkedCss_ByDefault()
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

                using var sw = new StringWriter();
                Cli.Run(new[] { htmlPath }, sw);
                var outText = sw.ToString();

                Assert.DoesNotContain("42px", outText);
                Assert.Contains("Scanning HTML/JSX/TSX", outText);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Cli_FollowsLinkedCss_WhenFlagProvided()
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

                using var sw = new StringWriter();
                Cli.Run(new[] { "--follow-links", htmlPath }, sw);
                var outText = sw.ToString();

                Assert.Contains("42px", outText);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Cli_PrintsHelp_WithHelpFlag()
        {
            using var sw = new StringWriter();
            Cli.Run(new[] { "--help" }, sw);
            var outText = sw.ToString();
            Assert.Contains("Usage:", outText);
            Assert.Contains("--follow-links", outText);
        }
    }
}