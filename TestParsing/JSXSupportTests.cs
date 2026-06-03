using System;
using System.IO;
using System.Linq;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class JSXSupportTests
    {
        [Fact]
        public void HTMLProcessor_ProcessFile_RecognizesJsxExtension()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string jsxPath = Path.Combine(tempDir, "component.jsx");
                string jsx = "<div style={{width:'100px', height: '50px'}}>x</div>";
                File.WriteAllText(jsxPath, jsx);

                var results = HTMLProcessor.ProcessFile(jsxPath);
                Assert.NotNull(results);
                Assert.Contains(results, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("100px"));
                Assert.Contains(results, r => string.Equals(r.Property, "height", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("50px"));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void HTMLProcessor_ProcessFile_RecognizesTsxExtension()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string tsxPath = Path.Combine(tempDir, "component.tsx");
                string tsx = "<div style={{padding:5, width:100}}>x</div>";
                File.WriteAllText(tsxPath, tsx);

                var results = HTMLProcessor.ProcessFile(tsxPath);
                Assert.NotNull(results);
                Assert.Contains(results, r => string.Equals(r.Property, "padding", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("5"));
                Assert.Contains(results, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("100"));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void HTMLProcessor_ParsesReactFunctionComponentInlineStyles()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string jsxPath = Path.Combine(tempDir, "FuncComponent.jsx");
                string jsx = @"
                    import React from 'react';
                    export default function Box() {
                        return (
                            <div className=""box"" style={{ width: '120px', height: 60, paddingLeft: '8px', marginTop: -2.5 }}>
                                Hello
                            </div>
                        );
                    }";
                File.WriteAllText(jsxPath, jsx);

                var results = HTMLProcessor.ProcessFile(jsxPath);
                Assert.NotNull(results);

                Assert.Contains(results, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("120px"));
                Assert.Contains(results, r => string.Equals(r.Property, "height", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("60"));
                Assert.Contains(results, r => string.Equals(r.Property, "padding-left", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("8px"));
                Assert.Contains(results, r => string.Equals(r.Property, "margin-top", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("-2.5"));
                Assert.All(results, r => Assert.True(r.AffectsLayout));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void CssLikeStringsInScript_AreIgnored_InJsxFiles()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string jsxPath = Path.Combine(tempDir, "WithScript.jsx");
                string jsx = @"
                    const css = ` .a{ width: 999px; } `;
                    export default function C() {
                        return <div style={{ width: '20px' }}></div>;
                    }";
                File.WriteAllText(jsxPath, jsx);

                var results = HTMLProcessor.ProcessFile(jsxPath);
                Assert.NotNull(results);

                // Should contain only the real inline style (20px) and ignore the template literal 999px
                Assert.Contains(results, r => string.Equals(r.Property, "width", StringComparison.OrdinalIgnoreCase) && r.Value.Contains("20px"));
                Assert.DoesNotContain(results, r => r.Value.Contains("999px"));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}