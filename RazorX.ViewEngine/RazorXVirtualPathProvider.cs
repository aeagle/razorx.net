using System;
using System.Web.Hosting;

namespace RazorX.ViewEngine
{
    public class RazorXVirtualPathProvider : VirtualPathProvider
    {
        private readonly IRazorXParser parser;

        public RazorXVirtualPathProvider(IRazorXParser parser)
        {
            this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        public override VirtualFile GetFile(string virtualPath)
        {
            if (!virtualPath.EndsWith(".cshtml"))
            {
                return base.GetFile(virtualPath);
            }

            var file = base.GetFile(virtualPath);

            using (var stream = file.Open())
            {
                var contents = parser.Process(stream);
                return new RazorXVirtualFile(virtualPath, contents);
            }
        }
    }
}
