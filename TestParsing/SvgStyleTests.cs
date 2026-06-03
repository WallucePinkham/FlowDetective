using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class SvgStyleTests
    {
        [Fact]
        public void CssAnalyzer_DetectsPxInSvgInlineStyle()
        {
            string svg = "<svg><rect style=\"width:10px;height:5px\" /></svg>";

            var list = CssAnalyzer.Analyze(svg);
            Assert.NotNull(list);

            Assert.Contains(list, r => string.Equals(r.Property, "width", System.StringComparison.OrdinalIgnoreCase) && r.Value.Contains("10px"));
            Assert.Contains(list, r => string.Equals(r.Property, "height", System.StringComparison.OrdinalIgnoreCase) && r.Value.Contains("5px"));

            // Inline SVG declarations should be flagged as layout-affecting
            Assert.All(list.Where(r => string.Equals(r.Property, "width", System.StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(r.Property, "height", System.StringComparison.OrdinalIgnoreCase)),
                       r => Assert.True(r.AffectsLayout));
        }

        [Fact]
        public void HTMLProcessor_ParsesSvgInlineStyle_ReturnsLayoutEntriesOnly()
        {
            string svg = "<svg><rect style=\"width:10px;height:5px;stroke:1px\" /></svg>";

            var results = HTMLProcessor.ProcessText(svg);
            Assert.NotNull(results);

            Assert.Contains(results, r => string.Equals(r.Property, "width", System.StringComparison.OrdinalIgnoreCase) && r.Value.Contains("10px"));
            Assert.Contains(results, r => string.Equals(r.Property, "height", System.StringComparison.OrdinalIgnoreCase) && r.Value.Contains("5px"));

            // visual-only or non-layout props (if present) should have been filtered by processor
            Assert.DoesNotContain(results, r => string.Equals(r.Property, "stroke", System.StringComparison.OrdinalIgnoreCase));
            Assert.All(results, r => Assert.True(r.AffectsLayout));
        }
    }
}