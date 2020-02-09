using NUnit.Framework;
using RazorX.ViewEngine.Parsers;
using System.IO;
using System.Threading.Tasks;

namespace RazorX.Unit.Tests
{
    public class RazorXSyntaxTreeTests
    {
        [TestCase("ComponentComplex")]
        public async Task RazorXSyntaxTree_Create_IsValid(string testFolder)
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
                logger.Log(new { A = 1, B = "2", C = 3 });

                await logger.Flush();
            }
        }
    }
}