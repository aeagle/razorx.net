using HtmlAgilityPack;
using System;
using System.IO;
using System.Text;
using System.Web.Hosting;
using System.Web.Mvc;

public class RSXViewEngine : IViewEngine
{
    private readonly IViewEngine baseViewEngine = new RazorViewEngine();

    public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
    {
        var result =
            baseViewEngine.FindPartialView(controllerContext, partialViewName, useCache);

        if (result.View != null)
        {
            return new ViewEngineResult(new RSXView(result.View), this);
        }

        return result;
    }

    public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
    {
        var result =
            baseViewEngine.FindView(controllerContext, viewName, masterName, useCache);

        if (result.View != null)
        {
            return new ViewEngineResult(new RSXView(result.View), this);
        }

        return result;
    }

    public class ComponentRegistration<T>
    {
        public Func<T, MvcHtmlString> Start { get; set; }
        public Func<T, MvcHtmlString> End { get; set; }
    }

    public static readonly string PREFIX = "RSXViewEngineComponent";

    public static void RegisterComponent<T>(
        TempDataDictionary components,
        string name, 
        Func<T, MvcHtmlString> start,
        Func<T, MvcHtmlString> end)
    {
        if (!components.ContainsKey($"{PREFIX}-{name}"))
        {
            components.Add(
                $"{PREFIX}-{name}",
                new ComponentRegistration<T>
                {
                    Start = start,
                    End = end
                }
            );
        }
    }

    public static MvcHtmlString WriteStart<T>(
        TempDataDictionary components, 
        string name, 
        T props)
    {
        if (components.TryGetValue($"{PREFIX}-{name}", out var component))
        {
            return ((ComponentRegistration<T>)component).Start(props);
        }
        return new MvcHtmlString("");
    }

    public static MvcHtmlString WriteEnd<T>(
        TempDataDictionary components, 
        string name, 
        T props)
    {
        if (components.TryGetValue($"{PREFIX}-{name}", out var component))
        {
            return ((ComponentRegistration<T>)component).End(props);
        }
        return new MvcHtmlString("");
    }

    public void ReleaseView(ControllerContext controllerContext, IView view)
    {
        baseViewEngine.ReleaseView(controllerContext, view);
    }
}

public class RSXView : IView
{
    private readonly IView razorView;

    public RSXView(IView razorView)
    {
        this.razorView = razorView ?? throw new ArgumentNullException(nameof(razorView));
    }

    public void Render(ViewContext viewContext, TextWriter writer)
    {
        // First render register components
        using (var memStream = new MemoryStream())
        { 
            using (var viewWriter = new StreamWriter(memStream))
            {
                razorView.Render(viewContext, viewWriter);
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(Encoding.UTF8.GetString(memStream.ToArray()));
        }

        // Second render
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

public class RSXVirtualPathProvider : VirtualPathProvider
{
    public static void AppInitialize()
    {
        RSXVirtualPathProvider provider = new RSXVirtualPathProvider();
        HostingEnvironment.RegisterVirtualPathProvider(provider);
    }

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
            var contents = RSXProcessor.ProcessFile(reader.ReadToEnd());
            return new RSXVirtualFile(virtualPath, contents);
        }
    }
}

public class RSXVirtualFile : VirtualFile
{
    private string processedFile;

    public RSXVirtualFile(string virtualPath, string processedFile)
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

public class RSXProcessor
{
    public static string ProcessFile(string text)
    {
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(text);

        var components = doc.DocumentNode.SelectNodes("//component");
        if (components != null)
        {
            foreach (var node in components)
            {
                var name = node.GetAttributeValue("component-name", null);
                if (name != null)
                {
                    var definition = node.InnerHtml;
                    definition = definition.Replace("{children}", "¬");
                    var parts = definition.Split("¬".ToCharArray());
                    var start = parts[0];
                    var end = parts.Length > 1 ? parts[1] : "";
                }
                node.Remove();
            }
        }

        return doc.DocumentNode.OuterHtml;
    }
}

public enum BoxoutType
{
    Standard,
    Bordered
}