using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class HTMLProcessorTests
    {
        [Fact]
        public void HTMLProcessor_FindsPxProperties()
        {
            string html = @"
                <html>
                  <head>
                    <style>
                      .cls { margin: 10px; padding: 2px; }
                    </style>
                  </head>
                  <body>
                    <div style=""width:100px; height:50px;"">Content</div>
                  </body>
                </html>";

            List<HTMLProcessor.PxProperty> results = HTMLProcessor.ProcessText(html);

            Assert.NotNull(results);
            Assert.NotEmpty(results);

            // All returned entries should be layout-affecting
            Assert.All(results, r => Assert.True(r.AffectsLayout));

            Assert.Contains(results, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("px"));
            Assert.Contains(results, r => string.Equals(r.Property, "height", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("px"));
            Assert.Contains(results, r => string.Equals(r.Property, "margin", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("px"));
        }

        [Fact]
        public void HTMLProcessor_NoPxProperties_ReturnsEmpty()
        {
            string html = @"
                <html>
                  <head>
                    <style>
                      .cls { margin: 1em; padding: 0.5rem; }
                    </style>
                  </head>
                  <body>
                    <div style=""width:100%; height:auto;"">Content</div>
                  </body>
                </html>";

            var results = HTMLProcessor.ProcessText(html);
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public void HTMLProcessor_MissingFile_ThrowsFileNotFoundException()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".html");
            if (File.Exists(path)) File.Delete(path);

            Assert.Throws<FileNotFoundException>(() => HTMLProcessor.ProcessFile(path));
        }

        [Fact]
        public void HTMLProcessor_NonHtmlFile_Ignored()
        {
            string text = "div { width: 10px; }";
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(path, text);

            try
            {
                var results = HTMLProcessor.ProcessFile(path);
                Assert.NotNull(results);
                Assert.Empty(results);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void HTMLProcessor_IgnoresVisualOnlyProperties()
        {
            string html = @"
                <html>
                  <body>
                    <div style=""box-shadow: 0 4px 8px 0 rgba(0,0,0,0.2); width: 120px;"">x</div>
                  </body>
                </html>";

            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".html");
            File.WriteAllText(path, html);

            try
            {
                var results = HTMLProcessor.ProcessFile(path);

                Assert.NotNull(results);
                // Should include width but not box-shadow
                Assert.Contains(results, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase));
                Assert.DoesNotContain(results, r => string.Equals(r.Property, "box-shadow", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}