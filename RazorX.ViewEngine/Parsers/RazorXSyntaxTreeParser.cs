using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Razor;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.Razor.Text;
using System.Web.Razor.Tokenizer.Symbols;

namespace RazorX.ViewEngine
{
    public class RazorXSyntaxTreeParser : IRazorXParser
    {
        private readonly RazorTemplateEngine engine;

        public RazorXSyntaxTreeParser()
        {
            var host = new RazorEngineHost(new CSharpRazorCodeLanguage());
            engine = new RazorTemplateEngine(host);
        }

        public string Process(string razorContent)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(razorContent)))
            {
                return Process(stream);
            }
        }

        public string Process(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var parserResults = engine.ParseTemplate(reader);
                var flattenedTree = new List<Span>();
                var needsSplit = false;

                bool isSplitExpression(Block block)
                {
                    return
                        block.Type == BlockType.Expression &&
                        string.Join(
                            "",
                            block.Children
                                .Select(c => c as Span)
                                .Where(c => c != null)
                                .Select(c => c.Content)
                        ).IndexOf($"@{RazorXViewEngine.PARTIAL_SPLIT_TOKEN}") >= 0;
                }

                Span createCodeSpan(string code)
                {
                    var builder = new SpanBuilder();
                    builder.Kind = SpanKind.Code;

                    builder.Accept(
                        new HtmlSymbol(
                            new SourceLocation(0, 0, 0),
                            code,
                            HtmlSymbolType.Text
                        )
                    );

                    return builder.Build();
                }

                void walkTree(Block block)
                {
                    if (isSplitExpression(block))
                    {
                        flattenedTree.Insert(0, createCodeSpan("@if (Model.renderTop) {\r\n"));
                        flattenedTree.Add(createCodeSpan("\r\n}\r\n@if (!Model.renderTop) {\r\n"));
                        needsSplit = true;
                    }
                    else
                    {
                        foreach (var item in block.Children)
                        {
                            var span = item as Span;
                            if (span != null)
                            {
                                flattenedTree.Add(span);
                            }

                            if (item.IsBlock)
                            {
                                walkTree((Block)item);
                            }
                        }
                    }
                }

                walkTree(parserResults.Document);
                if (needsSplit)
                {
                    flattenedTree.Add(createCodeSpan("\r\n}\r\n"));
                }

                StringBuilder result = new StringBuilder();
                foreach (var span in flattenedTree)
                {
                    result.Append(span.Content);
                }

                return result.ToString();
            }
        }
    }
}
