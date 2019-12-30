using NUnit.Framework;
using RazorX.ViewEngine;
using System.IO;

namespace RazorX.Unit.Tests
{
    public class RazorXSyntaxTreeParserTests
    {
        [TestCase("ComponentSimple")]
        [TestCase("ComponentWithinComponent")]
        [TestCase("ComponentUseSimple")]
        [TestCase("ComponentUseNested")]
        public void RazorXSyntaxTreeParser_IsValid(string testFolder)
        {
            // Arrange
            var original = File.ReadAllText(Utils.TestFile($"{testFolder}/Original.cshtml"));
            var expected = File.ReadAllText(Utils.TestFile($"{testFolder}/Expected.cshtml"));
            Assert.IsTrue(Utils.IsValidRazor(original));
            Assert.IsTrue(Utils.IsValidRazor(expected));

            var sut = new RazorXSyntaxTreeParser();

            // Act
            var actual = sut.Process(original);

            // Assert
            Assert.AreEqual(expected, actual);
            Assert.IsTrue(Utils.IsValidRazor(actual));
        }
    }
}