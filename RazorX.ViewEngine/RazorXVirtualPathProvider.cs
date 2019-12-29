using System.IO;
using System.Web.Hosting;

namespace RazorX.ViewEngine
{
    public class RazorXVirtualPathProvider : VirtualPathProvider
    {
        public override VirtualFile GetFile(string virtualPath)
        {
            if (!virtualPath.EndsWith(".cshtml"))
            {
                return base.GetFile(virtualPath);
            }

            var file = base.GetFile(virtualPath);

            using (var stream = file.Open())
            using (var reader = new StreamReader(stream))
            {
                var contents = RazorXParser.ProcessFile(reader.ReadToEnd());
                return new RazorXVirtualFile(virtualPath, contents);
            }
        }
    }
}
