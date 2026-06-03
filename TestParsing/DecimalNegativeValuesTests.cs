using System;
using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class DecimalNegativeValuesTests
    {
        [Fact]
        public void CssAnalyzer_DetectsDecimalAndNegativePxAndUnitless()
        {
            string css = ".a { margin-top: -2.5px; left: 0.0; top: -5; }";

            var list = CssAnalyzer.Analyze(css);
            Assert.NotNull(list);

            var mt = list.FirstOrDefault(r => string.Equals(r.Property, "margin-top", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(mt);
            Assert.Contains("-2.5px", mt.Value);
            Assert.True(mt.AffectsLayout);

            var left = list.FirstOrDefault(r => string.Equals(r.Property, "left", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(left);
            Assert.Contains("0.0", left.Value);
            Assert.True(left.AffectsLayout);

            var top = list.FirstOrDefault(r => string.Equals(r.Property, "top", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(top);
            Assert.Contains("-5", top.Value);
            Assert.True(top.AffectsLayout);
        }

        [Fact]
        public void CSSProcessor_IncludesDecimalAndNegativeLayoutProps()
        {
            string css = ".a{margin-top:-2.5px; left:0.0; top:-5;}";

            var processed = CSSProcessor.ProcessText(css);
            Assert.NotNull(processed);

            Assert.Contains(processed, r => string.Equals(r.Property, "margin-top", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("-2.5px"));
            Assert.Contains(processed, r => string.Equals(r.Property, "left", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("0.0"));
            Assert.Contains(processed, r => string.Equals(r.Property, "top", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("-5"));
        }
    }
}