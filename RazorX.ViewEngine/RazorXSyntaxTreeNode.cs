using System.Collections.Generic;
using System.Web.Razor.Parser.SyntaxTree;

namespace RazorX.ViewEngine
{
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
        public byte[] Bytes { get; set; } = new byte[] { 0x10, 0x12, 0x13, 0x14 };
        public IDictionary<int, string> Dictionary { get; set; } =
            new Dictionary<int, string>()
            {
                { 10, "Apples" },
                { 20, "Bananas" },
                { 30, "Oranges" }
            };

        public override string ToString()
        {
            return $"{Type}:{Content}";
        }
    }
}
