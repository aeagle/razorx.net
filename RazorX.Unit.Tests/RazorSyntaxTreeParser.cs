using NUnit.Framework;
using RazorX.ViewEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Razor;

namespace RazorX.Unit.Tests
{
    public class RazorSyntaxTreeParserTests
    {
        [Test]
        public void RazorSyntaxTreeParser_SimpleSplitComponent_ExpectedIsValid()
        {
            // Arrange
            var expected = File.ReadAllText(TestFile("ComponentSimple-Expected.cshtml"));

            // Act
            var actual = IsValidRazor(expected);

            // Assert
            Assert.IsTrue(actual);
        }

        [Test]
        public void RazorSyntaxTreeParser_SimpleSplitComponent_IsValid()
        {
            // Arrange
            var original = File.ReadAllText(TestFile("ComponentSimple.cshtml"));
            var expected = File.ReadAllText(TestFile("ComponentSimple-Expected.cshtml"));
            var sut = new RazorXSyntaxTreeParser();

            // Act
            var actual = sut.Process(original);

            // Assert
            Assert.AreEqual(expected, actual);
            Assert.IsTrue(IsValidRazor(actual));
        }

        [Test]
        public void RazorSyntaxTreeParser_ComponentWithinComponent_ExpectedIsValid()
        {
            // Arrange
            var expected = File.ReadAllText(TestFile("ComponentWithinComponent-Expected.cshtml"));

            // Act
            var actual = IsValidRazor(expected);

            // Assert
            Assert.IsTrue(actual);
        }

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
    }
}