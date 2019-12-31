using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Razor.Parser.SyntaxTree;

namespace RazorX.ViewEngine.Parsers
{
    public enum RazorXSyntaxTreeNodeType
    {
        Root,
        Text,
        Tag,
        TagAttribute,
        CodeBlock,
        Expression,
        SplitExpression
    }

    public class RazorXSyntaxTreeNode
    {
        private RazorXSyntaxTreeNode()
        {
        }

        public RazorXSyntaxTreeNodeType Type { get; set; }
        public string Content { get; set; }
        public Block SourceBlock { get; set; }
        public List<RazorXSyntaxTreeNode> Meta { get; set; } = new List<RazorXSyntaxTreeNode>();
        public List<RazorXSyntaxTreeNode> Children { get; set; } = new List<RazorXSyntaxTreeNode>();

        public override string ToString()
        {
            return $"{Type}:{Content}";
        }

        public static RazorXSyntaxTreeNode Create(Block razorSyntaxTree)
        {
            var context = new WalkTreeContext();
            Stack<Block> source = new Stack<Block>(new[] { razorSyntaxTree });
            Stack<RazorXSyntaxTreeNode> target = new Stack<RazorXSyntaxTreeNode>();

            var currentTarget = context.Tree;

            while (source.Count > 0)
            {
                var currentSource = source.Pop();

                foreach (var item in currentSource.Children)
                {
                    if (!item.IsBlock)
                    {
                        var span = (Span)item;
                        foreach (var symbol in span.Symbols)
                        {
                            // Starting HTML tag?
                            if (span.Kind == SpanKind.Markup && symbol.Content == "<" && !context.InHtmlTag)
                            {
                                context.InHtmlTag = true;
                                context.InHtmlTagName = true;
                            }

                            if (context.InHtmlTag)
                            {
                                if (symbol.Content == " ")
                                {
                                    context.InHtmlTagName = false;
                                }

                                if (context.InHtmlTagName)
                                {
                                    context.HtmlTagName.Append(symbol.Content);
                                }
                                else
                                {
                                    context.HtmlTagAttributes.Append(symbol.Content);
                                }
                            }

                            if (!context.InHtmlTag)
                            {
                                currentTarget.Children.Add(
                                    new RazorXSyntaxTreeNode()
                                    {
                                        SourceBlock = currentSource,
                                        Type = RazorXSyntaxTreeNodeType.Text,
                                        Content = symbol.Content
                                    }
                                );
                            }

                            // Ending HTML tag?
                            if (span.Kind == SpanKind.Markup && symbol.Content == ">" && context.InHtmlTag)
                            {
                                context.InHtmlTag = false;

                                var tagContents = $"{context.HtmlTagName}{context.HtmlTagAttributes}";

                                // Closing HTML tag?
                                if (tagContents.EndsWith("/>"))
                                {
                                    currentTarget = target.Pop();
                                }
                                else if (!tagContents.StartsWith("</"))
                                {
                                    target.Push(currentTarget);

                                    var newTarget =
                                        new RazorXSyntaxTreeNode()
                                        {
                                            SourceBlock = currentSource,
                                            Type = RazorXSyntaxTreeNodeType.Tag,
                                            Content = context.HtmlTagName.ToString().Substring(1)
                                        };

                                    newTarget.Meta.Add(
                                        new RazorXSyntaxTreeNode()
                                        {
                                            SourceBlock = currentSource,
                                            Type = RazorXSyntaxTreeNodeType.TagAttribute,
                                            Content = context.HtmlTagAttributes.ToString().Substring(1)
                                        }
                                    );

                                    currentTarget.Children.Add(newTarget);
                                    currentTarget = newTarget;
                                }

                                context.HtmlTagName.Clear();
                                context.HtmlTagAttributes.Clear();
                            }
                        }
                    }
                }

                foreach (var item in currentSource.Children.Where(x => x.IsBlock))
                {
                    source.Push((Block)item);
                }
            }

            return context.Tree;
        }

        internal class WalkTreeContext
        {
            internal RazorXSyntaxTreeNode Tree { get; private set; } = new RazorXSyntaxTreeNode { Type = RazorXSyntaxTreeNodeType.Root };
            internal bool InHtmlTag { get; set; }
            internal bool InHtmlTagName { get; set; }
            internal bool InCode { get; set; }
            internal StringBuilder HtmlTagName { get; private set; } = new StringBuilder();
            internal StringBuilder HtmlTagAttributes { get; private set; } = new StringBuilder();
        }
    }
}
