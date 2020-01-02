using NUnit.Framework;
using RazorX.ViewEngine.Parsers;
using System.IO;

namespace RazorX.Unit.Tests
{
    public class RazorXSyntaxTreeNodeTests
    {
        [TestCase("ComponentComplex")]
        public void RazorXRegExParser_IsValid(string testFolder)
        {
            // Arrange
            var razorFilename = File.ReadAllText(Utils.TestFile($"{testFolder}/Original.cshtml"));
            Assert.IsTrue(Utils.IsValidRazor(razorFilename), "Original file is not valid");
            var razorSyntaxTree = Utils.RazorSyntaxTree(razorFilename);

            // Act 
            var actual = RazorXSyntaxTree.Create(razorSyntaxTree);

            // Assert
        }
    }
}