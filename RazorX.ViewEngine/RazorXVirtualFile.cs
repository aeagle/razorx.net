using System.IO;
using System.Text;
using System.Web.Hosting;

namespace RazorX.ViewEngine
{
    public class RazorXVirtualFile : VirtualFile
    {
        private string processedFile;

        public RazorXVirtualFile(string virtualPath, string processedFile)
            : base(virtualPath)
        {
            this.processedFile = processedFile;
        }

        public override Stream Open()
        {
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(processedFile));
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }
}
