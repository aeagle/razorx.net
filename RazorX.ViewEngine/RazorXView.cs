using System;
using System.IO;
using System.Text;
using System.Web.Mvc;

namespace RazorX.ViewEngine
{
    public class RazorXView : IView
    {
        private readonly IView razorView;

        public RazorXView(IView razorView)
        {
            this.razorView = razorView ?? throw new ArgumentNullException(nameof(razorView));
        }

        public void Render(ViewContext viewContext, TextWriter writer)
        {
            using (var memStream = new MemoryStream())
            {
                using (var viewWriter = new StreamWriter(memStream))
                {
                    razorView.Render(viewContext, viewWriter);
                }

                var doc = Encoding.UTF8.GetString(memStream.ToArray());
                writer.Write(doc);
            }
        }
    }
}
