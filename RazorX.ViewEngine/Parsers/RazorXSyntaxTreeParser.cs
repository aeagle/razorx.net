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

                void walkTree(Block block)
                {
                    if (block.Type == BlockType.Expression &&
                        string.Join(
                            "",
                            block.Children
                                .Select(c => c as Span)
                                .Where(c => c != null)
                                .Select(c => c.Content)
                        ).IndexOf("Model.children") >= 0)
                    {
                        var builder = new SpanBuilder();
                        builder.Kind = SpanKind.Code;
                       
                        builder.Accept(
                            new HtmlSymbol(
                                new SourceLocation(0, 0, 0),
                                "\r\n}\r\n@if (!Model.renderTop) {\r\n",
                                HtmlSymbolType.Text
                            )
                        );
                        flattenedTree.Add(builder.Build());
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
                StringBuilder result = new StringBuilder();
                if (needsSplit)
                {
                    result.AppendLine("@if (Model.renderTop) {");
                }
                foreach (var span in flattenedTree)
                {
                    result.Append(span.Content);
                }
                if (needsSplit)
                {
                    result.AppendLine("}");
                }

                return result.ToString();
            }
        }
    }
}
