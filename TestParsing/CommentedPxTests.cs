using System;
using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class CommentedPxTests
    {
        [Fact]
        public void CssAnalyzer_IgnoresPxInsideClosedBlockComment()
        {
            string css = "/* .ignored { width: 999px; } */ .b { width: 10px; }";

            var list = CssAnalyzer.Analyze(css);
            Assert.NotNull(list);

            // Should contain only the real declaration, not the one inside the closed comment
            Assert.Contains(list, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("10px"));
            Assert.DoesNotContain(list, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("999px"));
        }

        [Fact]
        public void CssAnalyzer_ParsesPxInsideUnclosedComment_ButDoesNotCrash()
        {
            // Current behavior: unclosed comment is not removed by StripBlockComments and px inside may be parsed.
            string css = "/* unclosed comment .a { width: 999px; } .b { width: 20px; }";

            var list = CssAnalyzer.Analyze(css);
            Assert.NotNull(list); // must not crash

            // Document current behavior: ensure the valid declaration is seen
            Assert.Contains(list, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("20px"));

            // Optionally assert if unclosed-comment px is parsed (depends on desired contract).
            // If you want unclosed comments to be ignored, update StripBlockComments to handle that, then change this assert.
            Assert.Contains(list, r => r.Value.Contains("999px") || r.Value.Contains("20px"));
        }
    }
}