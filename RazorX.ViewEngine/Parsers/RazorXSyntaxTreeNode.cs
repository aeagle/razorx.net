using System.Collections.Generic;
using System.Web.Razor.Parser.SyntaxTree;

namespace RazorX.ViewEngine.Parsers
{
    public enum RazorXSyntaxTreeNodeType
    {
        Root,
        Text,
        Tag,
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
        public List<RazorXSyntaxTreeNode> Children { get; set; } = new List<RazorXSyntaxTreeNode>();

        public override string ToString()
        {
            return $"{Type}:{Content}";
        }

        public static RazorXSyntaxTreeNode Create(Block razorSyntaxTree)
        {
            return new RazorXSyntaxTreeNode { Type = RazorXSyntaxTreeNodeType.Root };
        }
    }
}
