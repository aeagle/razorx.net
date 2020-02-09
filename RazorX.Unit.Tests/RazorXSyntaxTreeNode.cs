using NUnit.Framework;
using RazorX.ViewEngine.Parsers;
using System.IO;
using System.Threading.Tasks;

namespace RazorX.Unit.Tests
{
    public class RazorXSyntaxTreeNodeTests
    {
        [TestCase("ComponentComplex")]
        public async Task RazorXSyntaxTreeParser_IsValid(string testFolder)
        {
            // Arrange
            var razorFilename = File.ReadAllText(Utils.TestFile($"{testFolder}/Original.cshtml"));
            Assert.IsTrue(Utils.IsValidRazor(razorFilename), "Original file is not valid");
            var razorSyntaxTree = Utils.RazorSyntaxTree(razorFilename);

            // Act 
            using (var logger = new VisualLog.Logger())
            {
                await logger.Clear();

                var actual = RazorXSyntaxTree.Create(razorSyntaxTree);
                logger.Log(actual);

                await logger.Flush();
            }
        }
    }
}