using System.IO;
using System.Linq;
using System.Text;
using System.Web.Razor;
using System.Web.Razor.Parser.SyntaxTree;

namespace RazorX.ViewEngine
{
    public class RazorXSyntaxTreeParser
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

                void walkTree(StringBuilder output, Block block) {
                    if (block.Type == BlockType.Expression &&
                        string.Join(
                            "",
                            block.Children
                                .Select(c => c as Span)
                                .Where(c => c != null)
                                .Select(c => c.Content)
                        ).IndexOf("Model.children") >= 0)
                    {
                        output.Append("{CHILDREN HERE}");
                    }
                    else
                    {
                        foreach (var item in block.Children)
                        {
                            var span = item as Span;
                            if (span != null)
                            {
                                output.Append(span.Content);
                            }

                            if (item.IsBlock)
                            {
                                walkTree(output, (Block)item);
                            }
                        }
                    }
                }

                StringBuilder result = new StringBuilder();
                walkTree(result, parserResults.Document);

                return result.ToString();
            }
        }
    }
}
