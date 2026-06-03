using System;
using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class MinifiedCssTests
    {
        [Fact]
        public void CssAnalyzer_MinifiedMultipleDeclarations_Parsed()
        {
            string css = ".a{margin:10px;padding:5px}.b{width:100px;box-shadow:0 4px 8px rgba(0,0,0,.2)}";

            var analyzed = CssAnalyzer.Analyze(css);
            Assert.NotNull(analyzed);

            // Analyzer should find the layout-affecting declarations
            Assert.Contains(analyzed, r => string.Equals(r.Property, "margin", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("10px"));
            Assert.Contains(analyzed, r => string.Equals(r.Property, "padding", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("5px"));
            Assert.Contains(analyzed, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("100px"));

            // Visual-only property should be present in analyzer output but flagged non-layout
            var shadow = analyzed.FirstOrDefault(r => string.Equals(r.Property, "box-shadow", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(shadow);
            Assert.False(shadow.AffectsLayout);

            // Processors should filter out visual-only properties
            var processed = CSSProcessor.ProcessText(css);
            Assert.DoesNotContain(processed, r => string.Equals(r.Property, "box-shadow", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(processed, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void HTMLProcessor_InlineMultipleDeclarations_SameLine()
        {
            string html = "<div style=\"margin:10px;padding:5px;width:100px;box-shadow:0 1px 2px rgba(0,0,0,.2)\">x</div>";

            var results = HTMLProcessor.ProcessText(html);
            Assert.NotNull(results);

            // Inline style tokens on a single line should be parsed
            Assert.Contains(results, r => string.Equals(r.Property, "margin", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("10px"));
            Assert.Contains(results, r => string.Equals(r.Property, "padding", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("5px"));
            Assert.Contains(results, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("100px"));

            // Visual-only must be filtered by processor
            Assert.DoesNotContain(results, r => string.Equals(r.Property, "box-shadow", StringComparison.OrdinalIgnoreCase));
        }
    }
}