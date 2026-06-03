using System;
using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class UnitlessNumbersTests
    {
        [Fact]
        public void CssAnalyzer_UnitlessNumbers_AreDetectedAndFlaggedCorrectly()
        {
            string css = ".a { width: 100; height: 0; font-weight: 700; line-height: 1.2; }";

            var list = CssAnalyzer.Analyze(css);
            Assert.NotNull(list);

            var width = list.FirstOrDefault(r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase));
            var height = list.FirstOrDefault(r => string.Equals(r.Property, "height", StringComparison.OrdinalIgnoreCase));
            var fw = list.FirstOrDefault(r => string.Equals(r.Property, "font-weight", StringComparison.OrdinalIgnoreCase));
            var lh = list.FirstOrDefault(r => string.Equals(r.Property, "line-height", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(width);
            Assert.NotNull(height);
            Assert.NotNull(fw);
            Assert.NotNull(lh);

            // Unitless width/height should be reported and flagged as layout-affecting
            Assert.True(width.AffectsLayout);
            Assert.True(height.AffectsLayout);

            // Unitless font-weight should be reported but NOT flagged as layout-affecting
            Assert.False(fw.AffectsLayout);

            // Line-height is in the layout-affecting list and should be flagged
            Assert.True(lh.AffectsLayout);
        }

        [Fact]
        public void CSSProcessor_FiltersOutNonLayoutUnitlessProperties()
        {
            string css = ".a { width: 100; font-weight: 700; }";

            var processed = CSSProcessor.ProcessText(css);
            Assert.NotNull(processed);

            // Processor only returns layout-affecting props, so width should be present and font-weight absent
            Assert.Contains(processed, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(processed, r => string.Equals(r.Property, "font-weight", StringComparison.OrdinalIgnoreCase));
        }
    }
}