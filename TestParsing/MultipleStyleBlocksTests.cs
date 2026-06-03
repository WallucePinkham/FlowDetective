using System;
using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class MultipleStyleBlocksTests
    {
        [Fact]
        public void CssAnalyzer_CollectsDeclarationsFromMultipleStyleBlocks()
        {
            string html = @"
                <html>
                  <head>
                    <style>.a { width: 100px; }</style>
                  </head>
                  <body>
                    <style>.a { width: 200px; }</style>
                  </body>
                </html>";

            var list = CssAnalyzer.Analyze(html);
            Assert.NotNull(list);

            // Both width declarations (100px and 200px) should be present
            Assert.Contains(list, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("100px"));
            Assert.Contains(list, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("200px"));

            // Exactly two width entries expected (same property, different values)
            Assert.Equal(2, list.Count(r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public void HTMLProcessor_ProcessesAllStyleBlocks_ReturnsLayoutEntriesOnly()
        {
            string html = @"
                <html>
                  <head>
                    <style>.a { width: 100px; box-shadow: 0 1px 2px rgba(0,0,0,.2); }</style>
                  </head>
                  <body>
                    <style>.a { width: 200px; }</style>
                    <div class='a' style='padding:10px;'></div>
                  </body>
                </html>";

            var results = HTMLProcessor.ProcessText(html);
            Assert.NotNull(results);

            // widths from both style blocks present and inline padding present
            Assert.Contains(results, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("100px"));
            Assert.Contains(results, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("200px"));
            Assert.Contains(results, r => string.Equals(r.Property, "padding", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("10px"));

            // visual-only box-shadow must be filtered by HTMLProcessor
            Assert.DoesNotContain(results, r => string.Equals(r.Property, "box-shadow", StringComparison.OrdinalIgnoreCase));
        }
    }
}