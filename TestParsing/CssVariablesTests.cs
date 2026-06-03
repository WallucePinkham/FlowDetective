using System;
using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class CssVariablesTests
    {
        [Fact]
        public void CssAnalyzer_ReportsCustomPropertyAndVarUsage()
        {
            string css = ":root { --gap: 8px; } .a { gap: var(--gap); }";

            var list = CssAnalyzer.Analyze(css);
            Assert.NotNull(list);

            // Custom property should be present with its px value
            Assert.Contains(list, r => string.Equals(r.Property, "--gap", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("8px"));

            // Usage should be present (raw value remains var(...)) but detection used the resolved value
            Assert.Contains(list, r => string.Equals(r.Property, "gap", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("var(--gap)"));

            var gap = list.First(r => string.Equals(r.Property, "gap", StringComparison.OrdinalIgnoreCase));
            Assert.True(gap.AffectsLayout);

            var custom = list.First(r => string.Equals(r.Property, "--gap", StringComparison.OrdinalIgnoreCase));
            // Custom properties are reported but are not considered layout-affecting by default
            Assert.False(custom.AffectsLayout);
        }

        [Fact]
        public void CssAnalyzer_UsesFallbackWhenCustomPropertyMissing()
        {
            string css = ".a { gap: var(--missing, 10px); }";

            var list = CssAnalyzer.Analyze(css);
            Assert.NotNull(list);

            // gap should be detected because the fallback resolves to px
            var gap = list.FirstOrDefault(r => string.Equals(r.Property, "gap", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(gap);
            Assert.True(gap.AffectsLayout);

            // No custom property declaration for --missing should exist
            Assert.DoesNotContain(list, r => string.Equals(r.Property, "--missing", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void CSSProcessor_FiltersVisualOnlyEvenWhenVarResolvesToPx()
        {
            string css = @"
                :root { --s: 0 1px 2px rgba(0,0,0,.2); }
                .a { box-shadow: var(--s); width: var(--w, 50px); }";

            // Analyzer should include box-shadow (visual-only) and width (layout)
            var analyzed = CssAnalyzer.Analyze(css);
            Assert.Contains(analyzed, r => string.Equals(r.Property, "box-shadow", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(analyzed, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase));

            // Processor should filter visual-only properties (box-shadow) but keep width
            var processed = CSSProcessor.ProcessText(css);
            Assert.DoesNotContain(processed, r => string.Equals(r.Property, "box-shadow", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(processed, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase));
        }
    }
}