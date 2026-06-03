
using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class ScriptStyleTextTests
    {
        [Fact]
        public void CssAnalyzer_IgnoresCssLikeStrings_InScript()
        {
            string html = "<html><body><script>var s = '.a{width:10px}';</script></body></html>";

            var list = CssAnalyzer.Analyze(html);
            Assert.NotNull(list);
            Assert.DoesNotContain(list, r => string.Equals(r.Property, "width", System.StringComparison.OrdinalIgnoreCase) && r.Value.Contains("10px"));

            var processed = HTMLProcessor.ProcessText(html);
            Assert.NotNull(processed);
            Assert.Empty(processed);
        }

        [Fact]
        public void CssAnalyzer_StillParsesRealStyleBlocks_WhenScriptContainsCssLikeStrings()
        {
            string html = "<style>.a{width:100px}</style><script>var s = '.a{width:10px}';</script>";

            var list = CssAnalyzer.Analyze(html);
            Assert.NotNull(list);
            Assert.Contains(list, r => string.Equals(r.Property, "width", System.StringComparison.OrdinalIgnoreCase) && r.Value.Contains("100px"));
            Assert.DoesNotContain(list, r => r.Value.Contains("10px"));

            var processed = HTMLProcessor.ProcessText(html);
            Assert.NotNull(processed);
            Assert.Contains(processed, r => string.Equals(r.Property, "width", System.StringComparison.OrdinalIgnoreCase) && r.Value.Contains("100px"));
            Assert.DoesNotContain(processed, r => r.Value.Contains("10px"));
        }
    }
}