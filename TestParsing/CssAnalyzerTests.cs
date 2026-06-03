using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class CssAnalyzerTests
    {
        [Fact]
        public void CssAnalyzer_Analyze_ReturnsEntriesAndFlagsAffectsLayout()
        {
            string css = ".a { box-shadow: 0 1px 2px rgba(0,0,0,.2); width: 100px; margin: 5px; }";

            var list = CssAnalyzer.Analyze(css);
            Assert.NotNull(list);

            // Should contain width and margin
            Assert.Contains(list, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(list, r => string.Equals(r.Property, "margin", StringComparison.OrdinalIgnoreCase));

            // Visual-only property should be present but flagged as non-layout-affecting
            var shadow = list.FirstOrDefault(r => string.Equals(r.Property, "box-shadow", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(shadow);
            Assert.False(shadow.AffectsLayout);
        }

        [Fact]
        public void CssAnalyzer_Analyze_NoPx_ReturnsEmpty()
        {
            string css = ".a { margin: 1em; padding: .5rem; }";

            var list = CssAnalyzer.Analyze(css);
            Assert.NotNull(list);
            Assert.Empty(list);
        }
    }
}