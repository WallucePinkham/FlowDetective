using System;
using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class PropertyListsTests
    {
        [Fact]
        public void CssAnalyzer_IncludesVisualOnly_ButProcessorsFilterThem()
        {
            string css = ".a { box-shadow: 0 1px 2px rgba(0,0,0,.2); width: 100px; }";

            var list = CssAnalyzer.Analyze(css);
            Assert.Contains(list, r => string.Equals(r.Property, "box-shadow", StringComparison.OrdinalIgnoreCase));

            var box = list.First(r => string.Equals(r.Property, "box-shadow", StringComparison.OrdinalIgnoreCase));
            Assert.False(box.AffectsLayout);

            var cssFiltered = CSSProcessor.ProcessText(css);
            Assert.DoesNotContain(cssFiltered, r => string.Equals(r.Property, "box-shadow", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(cssFiltered, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase));

            string html = "<div style=\"box-shadow:0 1px 2px rgba(0,0,0,.2); width:50px;\"></div>";
            var htmlFiltered = HTMLProcessor.ProcessText(html);
            Assert.DoesNotContain(htmlFiltered, r => string.Equals(r.Property, "box-shadow", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(htmlFiltered, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void PropertyLists_ContainsExpectedEntries()
        {
            Assert.Contains("box-shadow", PropertyLists.VisualOnlyProps);
            Assert.Contains("width", PropertyLists.LayoutAffectingProps);
        }
    }
}
