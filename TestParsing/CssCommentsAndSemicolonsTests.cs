
using System;
using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class CssCommentsAndSemicolonsTests
    {
        [Fact]
        public void CssAnalyzer_HandlesBlockComments()
        {
            string css = "/* comment */ .a{ width:10px; /* inline comment */ padding: 5px; }";

            var list = CssAnalyzer.Analyze(css);
            Assert.NotNull(list);
            Assert.NotEmpty(list);

            // Should find width and padding despite comments
            Assert.Contains(list, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("10px"));
            Assert.Contains(list, r => string.Equals(r.Property, "padding", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("5px"));
        }

        [Fact]
        public void CssAnalyzer_HandlesMultipleBlockComments_ComplexNesting()
        {
            string css = "/* outer */ .a{ width:10px /* mid */ ; /* after */ padding: 5px; } /* end */";

            var list = CssAnalyzer.Analyze(css);
            Assert.NotNull(list);

            Assert.Contains(list, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(list, r => string.Equals(r.Property, "padding", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void CssAnalyzer_HandlesMissingSemicolons()
        {
            string css = ".a { padding: 5px }";

            var list = CssAnalyzer.Analyze(css);
            Assert.NotNull(list);

            // Should find padding even without trailing semicolon
            Assert.Contains(list, r => string.Equals(r.Property, "padding", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("5px"));
        }

        [Fact]
        public void CssAnalyzer_HandlesMissingSemicolonsBeforeClosingBrace()
        {
            string css = ".a { width: 100px; height: 50px }";

            var list = CssAnalyzer.Analyze(css);
            Assert.NotNull(list);

            Assert.Contains(list, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("100px"));
            Assert.Contains(list, r => string.Equals(r.Property, "height", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("50px"));
        }

        [Fact]
        public void CssAnalyzer_CommentsAndMissingSemicolons_Combined()
        {
            string css = @"
                /* header comment */
                .a {
                    margin: 10px /* mid-comment */ ;
                    padding: 5px /* end comment without semicolon */ }
                .b { width: 100px }
                /* trailing comment */";

            var list = CssAnalyzer.Analyze(css);
            Assert.NotNull(list);
            Assert.NotEmpty(list);

            Assert.Contains(list, r => string.Equals(r.Property, "margin", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("10px"));
            Assert.Contains(list, r => string.Equals(r.Property, "padding", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("5px"));
            Assert.Contains(list, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("100px"));
        }

        [Fact]
        public void HTMLProcessor_InlineStyle_MissingSemicolon()
        {
            string html = "<div style=\"width:100px; padding:5px\">Content</div>";

            var results = HTMLProcessor.ProcessText(html);
            Assert.NotNull(results);

            Assert.Contains(results, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(results, r => string.Equals(r.Property, "padding", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void CssAnalyzer_DoesNotCrash_OnMalformedCommentSyntax()
        {
            // Unclosed comment should not crash
            string css = ".a { width: 10px; /* unclosed comment } .b { height: 20px; }";

            var list = CssAnalyzer.Analyze(css);
            // Analyzer should not crash; result depends on regex robustness
            Assert.NotNull(list);
        }
    }
}