using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class AdditionalCssTests
    {
        [Fact]
        public void HTMLProcessor_InlineAndStyle_BothParsed_IncludingUnitless()
        {
            string html = @"
                <html>
                  <head>
                    <style>
                      .cls { margin: 10px 0 5px 2px; }
                    </style>
                  </head>
                  <body>
                    <div class=""cls"" style=""padding:5px; width:100;"">Content</div>
                  </body>
                </html>";

            var results = HTMLProcessor.ProcessText(html);
            Assert.NotNull(results);
            // margin from <style> shorthand should be present
            Assert.Contains(results, r => string.Equals(r.Property, "margin", StringComparison.OrdinalIgnoreCase)
                                          && r.Value.Contains("10px"));
            // padding from inline style should be present
            Assert.Contains(results, r => string.Equals(r.Property, "padding", StringComparison.OrdinalIgnoreCase)
                                          && r.Value.Contains("5px"));
            // unitless width should be reported (treated as px-equivalent for reporting)
            Assert.Contains(results, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase)
                                          && r.Value.Contains("100"));
            // All returned should be flagged as affecting layout
            Assert.All(results, r => Assert.True(r.AffectsLayout));
        }

        [Fact]
        public void CssAnalyzer_ShorthandMarginAndUnitless_AreDetected()
        {
            string css = ".a { margin: 10px 0 5px 2px; width: 100; height:0; font-weight: 700; }";

            var list = CssAnalyzer.Analyze(css);
            Assert.NotNull(list);

            // Shorthand margin with multiple tokens should be captured
            var margin = list.FirstOrDefault(r => string.Equals(r.Property, "margin", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(margin);
            Assert.Contains("10px", margin.Value);
            Assert.True(margin.AffectsLayout);

            // Unitless width and height should be detected and flagged as layout affecting
            var width = list.FirstOrDefault(r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(width);
            Assert.Contains("100", width.Value);
            Assert.True(width.AffectsLayout);

            var height = list.FirstOrDefault(r => string.Equals(r.Property, "height", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(height);
            Assert.Contains("0", height.Value);
            Assert.True(height.AffectsLayout);

            // Non-layout numeric property (font-weight) may appear but should not be marked as layout-affecting
            var fw = list.FirstOrDefault(r => string.Equals(r.Property, "font-weight", StringComparison.OrdinalIgnoreCase));
            if (fw is not null)
            {
                Assert.False(fw.AffectsLayout);
            }
        }
    }
}