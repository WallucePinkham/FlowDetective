
using System;
using System.IO;
using Xunit;
using FlowDetective;

namespace TestParsing
{
    public class JsxResilienceTests
    {
        [Fact]
        public void HTMLProcessor_ProcessFile_DoesNotThrow_OnMalformedJsx()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string path = Path.Combine(tempDir, "BrokenComponent.jsx");
                // malformed JSX (unbalanced braces / unterminated comment)
                string jsx = @"
                    import React from 'react';
                    export default function X() {
                        return <div style={{ width: '100px', height: 50 }}>Hello{/* unclosed comment
                    }";
                File.WriteAllText(path, jsx);

                var ex = Record.Exception(() => HTMLProcessor.ProcessFile(path));
                Assert.Null(ex);

                var results = HTMLProcessor.ProcessFile(path);
                Assert.NotNull(results); // may be empty, but must not be null / crash
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void HTMLProcessor_ProcessFile_DoesNotThrow_OnMalformedTsx()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string path = Path.Combine(tempDir, "BrokenComponent.tsx");
                // malformed TSX (broken expression)
                string tsx = @"
                    const A: React.FC = () => {
                        return <div style={{ padding: 5, width: 100 }}>X</div
                    }";
                File.WriteAllText(path, tsx);

                var ex = Record.Exception(() => HTMLProcessor.ProcessFile(path));
                Assert.Null(ex);

                var results = HTMLProcessor.ProcessFile(path);
                Assert.NotNull(results);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}