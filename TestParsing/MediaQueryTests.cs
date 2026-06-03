
using System;
using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class MediaQueryTests
    {
        [Fact]
        public void CssAnalyzer_DetectsPxInsideMediaQuery()
        {
            string css = "@media (max-width:600px){ .a{width:200px;} }";

            var list = CssAnalyzer.Analyze(css);
            Assert.NotNull(list);

            // media feature max-width should be detected (600px)
            Assert.Contains(list, r => string.Equals(r.Property, "max-width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("600px"));
            // nested rule declaration should be detected (width:200px)
            Assert.Contains(list, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("200px"));

            // both are layout-affecting per PropertyLists
            Assert.All(list.Where(r => string.Equals(r.Property, "max-width", StringComparison.OrdinalIgnoreCase)
                                       || string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase)),
                       r => Assert.True(r.AffectsLayout));
        }

        [Fact]
        public void CSSProcessor_IncludesLayoutPropsFromMediaQuery_ButFiltersVisualOnly()
        {
            string css = "@media (max-width:600px){ .a{width:200px; box-shadow:0 1px 2px rgba(0,0,0,.2);} }";

            var processed = CSSProcessor.ProcessText(css);
            Assert.NotNull(processed);

            // width should be included
            Assert.Contains(processed, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("200px"));
            // visual-only box-shadow should be filtered out by processor
            Assert.DoesNotContain(processed, r => string.Equals(r.Property, "box-shadow", StringComparison.OrdinalIgnoreCase));
        }
    }
}