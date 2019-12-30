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
                WalkTree(walkContext, parserResults.Document);

                StringBuilder result = new StringBuilder();
                foreach (var span in walkContext.FlattenedTree)
                {
                    result.Append(span.Content);
                }

                return result.ToString();
            }
        }

        internal class WalkContext
        {
            internal List<Span> FlattenedTree { get; set; } = new List<Span>();
            internal bool NeedsSplit { get; set; } = false;
        }

        private static void WalkTree(WalkContext context, Block block, int level = 0)
        {
            if (IsSplitExpression(block))
            {
                // TODO: Look at the block types surrounding these blocks to determine if we need the @ or not
                context.FlattenedTree.Insert(0, CreateCodeSpan("@if (Model.renderTop) {\r\n"));
                context.FlattenedTree.Add(CreateCodeSpan("\r\n}\r\n@if (!Model.renderTop) {\r\n"));
                context.NeedsSplit = true;
            }
            else
            {
                foreach (var item in block.Children)
                {
                    var span = item as Span;
                    if (span != null)
                    {
                        // TODO: Add @: in front of tags now missing end tag and vice versa because of added @if statements
                        context.FlattenedTree.Add(span);

                        // TODO: Process <component-xxx> tags
                    }

                    if (item.IsBlock)
                    {
                        WalkTree(context, (Block)item, level + 1);
                    }
                }
            }

            if (level == 0 && context.NeedsSplit)
            {
                context.FlattenedTree.Add(CreateCodeSpan("\r\n}\r\n"));
            }
        }

        private static Span CreateCodeSpan(string code)
        {
            // TODO: Correctly reconstruct the syntax tree with correct symbols
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

        private static bool IsSplitExpression(Block block) =>
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
