
using System;
using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class HtmlCommentsTests
    {
        [Fact]
        public void CssAnalyzer_FindsDeclarations_WhenCssWrappedWithHtmlCommentMarkers_InsideStyle()
        {
            string html = "<style><!-- .a{width:10px;} --></style>";

            var list = CssAnalyzer.Analyze(html);
            Assert.NotNull(list);
            Assert.Contains(list, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("10px"));

            // Processor should also surface the layout-affecting prop (and filter visual-only)
            var processed = HTMLProcessor.ProcessText(html);
            Assert.Contains(processed, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("10px"));
        }

        [Fact]
        public void CssAnalyzer_FindsDeclarations_WhenStyleTagIsInsideHtmlCommentBlock()
        {
            string html = "<!-- <style>.a{width:20px;}</style> -->";

            var list = CssAnalyzer.Analyze(html);
            Assert.NotNull(list);
            // Current behavior: style tag inside an HTML comment is still matched by the extractor
            Assert.Contains(list, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("20px"));
        }

        [Fact]
        public void CssAnalyzer_FindsDeclarations_InConditionalComments()
        {
            string html = "<!--[if IE]><style>.ie{width:30px;}</style><![endif]-->";

            var list = CssAnalyzer.Analyze(html);
            Assert.NotNull(list);
            Assert.Contains(list, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("30px"));
        }
    }
}