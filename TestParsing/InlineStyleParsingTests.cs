
using System;
using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class InlineStyleParsingTests
    {
        [Fact]
        public void HTMLProcessor_ParsesSpaceDelimitedInlineStyles_WithoutSemicolons()
        {
            string html = "<div style=\"width:100px height:50px\">x</div>";

            var results = HTMLProcessor.ProcessText(html);
            Assert.NotNull(results);

            Assert.Contains(results, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("100px"));
            Assert.Contains(results, r => string.Equals(r.Property, "height", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("50px"));

            // All returned should be layout-affecting
            Assert.All(results, r => Assert.True(r.AffectsLayout));
        }

        [Fact]
        public void HTMLProcessor_ParsesInlineStyles_WithUnusualSpacingAndMissingOrTrailingSemicolons()
        {
            string html = "<div style=\"padding : 5px ;width: 100\">x</div>";

            var results = HTMLProcessor.ProcessText(html);
            Assert.NotNull(results);

            Assert.Contains(results, r => string.Equals(r.Property, "padding", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("5px"));
            Assert.Contains(results, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("100"));

            Assert.All(results, r => Assert.True(r.AffectsLayout));
        }
    }
}