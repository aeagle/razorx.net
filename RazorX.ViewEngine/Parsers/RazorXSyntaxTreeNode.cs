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
        Code,
        Expression,
        SplitExpression
    }

    public class RazorXSyntaxTree : RazorXSyntaxTreeNode
    {
        public static RazorXSyntaxTree Create(Block razorSyntaxTree)
        {
            var context = new WalkTreeContext();
            Stack<RazorXSyntaxTree> target = new Stack<RazorXSyntaxTree>();

            var currentTarget = context.Tree;

            void pushText()
            {
                if (context.Text.Length > 0)
                {
                    currentTarget.Children.Add(
                        new RazorXSyntaxTree()
                        {
                            SourceBlock = null,
                            Type = RazorXSyntaxTreeNodeType.Text,
                            Content = context.Text.ToString()
                        }
                    );

                    context.Text.Clear();
                }
            }

            void walkTree(Block node)
            {
                if (IsSplitExpression(node))
                {
                    currentTarget.Children.Add(
                        new RazorXSyntaxTree()
                        {
                            SourceBlock = node,
                            Type = RazorXSyntaxTreeNodeType.SplitExpression,
                            Content =
                                string.Join(
                                    "",
                                    node.Children
                                        .Select(c => c as Span)
                                        .Where(c => c != null)
                                        .Select(c => c.Content)
                                )
                        }
                    );
                }
                else
                {
                    foreach (var item in node.Children)
                    {
                        if (item.IsBlock)
                        {
                            var block = (Block)item;

                            if (block.Type == BlockType.Statement)
                            {
                                pushText();
                                target.Push(currentTarget);

                                var newTarget =
                                    new RazorXSyntaxTree()
                                    {
                                        SourceBlock = block,
                                        Type = RazorXSyntaxTreeNodeType.CodeBlock,
                                        Content = ""
                                    };

                                currentTarget = newTarget;

                                walkTree(block);

                                pushText();

                                currentTarget = target.Pop();

                                if (newTarget.Children.Count > 0)
                                {
                                    currentTarget.Children.Add(newTarget);
                                }
                            }
                            else
                            {
                                walkTree(block);
                            }
                        }
                        else
                        {
                            var span = item as Span;
                            foreach (var symbol in span.Symbols)
                            {
                                // Starting HTML tag?
                                if (span.Kind == SpanKind.Markup && symbol.Content == "<" && !context.InHtmlTag)
                                {
                                    pushText();

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
                                    context.Text.Append(symbol.Content);
                                }

                                // Ending HTML tag?
                                if (span.Kind == SpanKind.Markup && symbol.Content == ">" && context.InHtmlTag)
                                {
                                    context.InHtmlTag = false;

                                    var tagContents = $"{context.HtmlTagName}{context.HtmlTagAttributes}";

                                    // Closing HTML tag?
                                    if (tagContents.StartsWith("</"))
                                    {
                                        currentTarget = target.Pop();
                                    }
                                    else if (!tagContents.EndsWith("/>"))
                                    {
                                        target.Push(currentTarget);

                                        var newTarget =
                                            new RazorXSyntaxTree()
                                            {
                                                SourceBlock = span.Parent,
                                                Type = RazorXSyntaxTreeNodeType.Tag,
                                                Content = context.HtmlTagName.ToString().Substring(1)
                                            };

                                        newTarget.Meta.Add(
                                            new RazorXSyntaxTree()
                                            {
                                                SourceBlock = span.Parent,
                                                Type = RazorXSyntaxTreeNodeType.TagAttribute,
                                                Content = context.HtmlTagAttributes.ToString().Substring(1)
                                            }
                                        );

                                        currentTarget.Children.Add(newTarget);
                                        currentTarget = newTarget;
                                    }
                                    else
                                    {
                                        var newTarget =
                                            new RazorXSyntaxTree()
                                            {
                                                SourceBlock = span.Parent,
                                                Type = RazorXSyntaxTreeNodeType.Tag,
                                                Content = context.HtmlTagName.ToString().Substring(1)
                                            };

                                        newTarget.Meta.Add(
                                            new RazorXSyntaxTree()
                                            {
                                                SourceBlock = span.Parent,
                                                Type = RazorXSyntaxTreeNodeType.TagAttribute,
                                                Content = context.HtmlTagAttributes.ToString().Substring(1)
                                            }
                                        );

                                        currentTarget.Children.Add(newTarget);
                                    }

                                    context.HtmlTagName.Clear();
                                    context.HtmlTagAttributes.Clear();
                                }
                            }
                        }
                    }
                }
            }

            walkTree(razorSyntaxTree);

            pushText();

            return context.Tree;
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

        internal class WalkTreeContext
        {
            internal RazorXSyntaxTree Tree { get; private set; } = new RazorXSyntaxTree { Type = RazorXSyntaxTreeNodeType.Root };
            internal bool InHtmlTag { get; set; }
            internal bool InHtmlTagName { get; set; }
            internal bool InCode { get; set; }
            internal StringBuilder Text { get; private set; } = new StringBuilder();
            internal StringBuilder HtmlTagName { get; private set; } = new StringBuilder();
            internal StringBuilder HtmlTagAttributes { get; private set; } = new StringBuilder();
        }
    }

    public class RazorXSyntaxTreeNode
    {
        protected internal RazorXSyntaxTreeNode()
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
    }
}
