using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class StyleBlockTerminationTests
    {
        [Fact]
        public void CssAnalyzer_ParsesDeclarations_ThatEndBeforeClosingBrace()
        {
            string html = "<style>.a{width:100px}</style><script>var s = '.a{width:10px}';</script>";

            var list = CssAnalyzer.Analyze(html);
            Assert.NotNull(list);

            // Ensure we detect the real style block declaration and ignore the script string
            Assert.Contains(list, r => string.Equals(r.Property, "width", System.StringComparison.OrdinalIgnoreCase) && r.Value.Contains("100px"));
            Assert.DoesNotContain(list, r => r.Value.Contains("10px"));
        }
    }
}