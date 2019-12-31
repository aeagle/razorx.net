using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Razor;
using System.Web.Razor.Parser.SyntaxTree;

namespace RazorX.Unit.Tests
{
    public static class Utils
    {
        public static string TestFile(string name) =>
            Path.Combine(AssemblyDirectory, $"../../../RazorFiles/{name}");

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static bool IsValidRazor(string razor)
        {
            var host = new RazorEngineHost(new CSharpRazorCodeLanguage());
            var engine = new RazorTemplateEngine(host);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(razor)))
            {
                using (var reader = new StreamReader(stream))
                {
                    var parserResults = engine.ParseTemplate(reader);
                    return !parserResults.ParserErrors.Any();
                }
            }
        }

        public static Block RazorSyntaxTree(string razor)
        {
            var host = new RazorEngineHost(new CSharpRazorCodeLanguage());
            var engine = new RazorTemplateEngine(host);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(razor)))
            {
                using (var reader = new StreamReader(stream))
                {
                    return engine.ParseTemplate(reader).Document;
                }
            }
        }
    }
}
