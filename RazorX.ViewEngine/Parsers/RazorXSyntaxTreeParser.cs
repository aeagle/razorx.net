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

                var walkContext = new WalkContext();
                walkTree(walkContext, parserResults.Document);

                StringBuilder result = new StringBuilder();
                foreach (var span in walkContext.FlattenedTree)
                {
                    result.Append(span.Content);
                }

                return result.ToString();
            }
        }

        private static void walkTree(WalkContext context, Block block, int level = 0)
        {
            if (isSplitExpression(block))
            {
                context.FlattenedTree.Insert(0, createCodeSpan("@if (Model.renderTop) {\r\n"));
                context.FlattenedTree.Add(createCodeSpan("\r\n}\r\n@if (!Model.renderTop) {\r\n"));
                context.NeedsSplit = true;
            }
            else
            {
                foreach (var item in block.Children)
                {
                    var span = item as Span;
                    if (span != null)
                    {
                        context.FlattenedTree.Add(span);
                    }

                    if (item.IsBlock)
                    {
                        walkTree(context, (Block)item, level + 1);
                    }
                }
            }

            if (level == 0 && context.NeedsSplit)
            {
                context.FlattenedTree.Add(createCodeSpan("\r\n}\r\n"));
            }
        }

        internal class WalkContext
        {
            internal List<Span> FlattenedTree { get; set; } = new List<Span>();
            internal bool NeedsSplit { get; set; } = false;
        }

        private static Span createCodeSpan(string code)
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

        private static bool isSplitExpression(Block block) =>
            block.Type == BlockType.Expression &&
            string.Join(
                "",
                block.Children
                    .Select(c => c as Span)
                    .Where(c => c != null)
                    .Select(c => c.Content)
            ).IndexOf(RazorXViewEngine.PARTIAL_SPLIT_TOKEN) >= 0;
    }
}
