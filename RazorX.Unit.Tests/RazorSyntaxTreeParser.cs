using NUnit.Framework;
using RazorX.ViewEngine;
using System;
using System.IO;
using System.Reflection;
using System.Web.Razor;

namespace RazorX.Unit.Tests
{
    public class RazorSyntaxTreeParserTests
    {
        [Test]
        public void RazorSyntaxTreeParser_SimpleSplitComponent()
        {
            // Arrange
            var original = File.ReadAllText(TestFile("ComponentSimple.cshtml"));
            var expected = File.ReadAllText(TestFile("ComponentSimple-Expected.cshtml"));
            var sut = new RazorXSyntaxTreeParser();

            // Act
            var actual = sut.Process(original);

            // Assert
            Assert.AreEqual(expected, actual);
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
    }
}