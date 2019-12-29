using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Razor.Parser;

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

    private static Dictionary<string, object> components =
        new Dictionary<string, object>();

    public static void RegisterComponent<T>(
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
    public static Regex componentTagRegex = 
        new Regex(@"<component-(.+?)(| (.+?))>(.+?)</component-\1>", RegexOptions.Singleline | RegexOptions.Compiled);

    public static string ProcessFile(string text)
    {
        MatchEvaluator processComponent = null;
        processComponent =
            (match) =>
            {
                var parsedDoc = new HtmlDocument();
                parsedDoc.LoadHtml($"<p {match.Groups[2].Value}></p>");
                var node = parsedDoc.DocumentNode.ChildNodes.FindFirst("p");

                var propsGuid = Guid.NewGuid().ToString().Replace("-", "");
                StringBuilder dynamicObject = new StringBuilder("@{ ");
                dynamicObject.Append($"dynamic props{propsGuid}start = new System.Dynamic.ExpandoObject();");
                dynamicObject.Append($"props{propsGuid}start.renderTop = true;");
                foreach (var attribute in node.Attributes)
                {
                    if (attribute.Value.StartsWith("@"))
                    {
                        dynamicObject.Append($"props{propsGuid}start.{attribute.Name} = {attribute.Value.Substring(1)};");
                    }
                    else
                    {
                        dynamicObject.Append($"props{propsGuid}start.{attribute.Name} = \"{attribute.Value}\";");
                    }
                }
                dynamicObject.Append($"dynamic props{propsGuid}end = new System.Dynamic.ExpandoObject();");
                dynamicObject.Append($"props{propsGuid}end.renderTop = false;");
                foreach (var attribute in node.Attributes)
                {
                    if (attribute.Value.StartsWith("@"))
                    {
                        dynamicObject.Append($"props{propsGuid}end.{attribute.Name} = {attribute.Value.Substring(1)};");
                    }
                    else
                    {
                        dynamicObject.Append($"props{propsGuid}end.{attribute.Name} = \"{attribute.Value}\";");
                    }
                }
                dynamicObject.Append("}\r\n");
                dynamicObject.AppendLine($"@Html.Partial(\"{match.Groups[1]}\", (object)props{propsGuid}start)");
                dynamicObject.AppendLine(
                    componentTagRegex.Replace(
                        match.Groups[4].Value, 
                        processComponent
                    )
                );
                dynamicObject.AppendLine($"@Html.Partial(\"{match.Groups[1]}\", (object)props{propsGuid}end)");

                var replacement = dynamicObject.ToString();
                return replacement;
            };

        text = componentTagRegex.Replace(text, processComponent);

        if (text.IndexOf("@Model.children") >= 0)
        {
            var renderParts = text.Replace("@Model.children", "¬").Split("¬".ToCharArray());
            StringBuilder newText = new StringBuilder();
            newText.AppendLine("@if (Model.renderTop) {");
            newText.AppendLine(string.Join("\r\n", renderParts[0].Split("\r\n".ToCharArray()).Select(x => x.Trim().StartsWith("@") ? x : $"@:{x}")));
            newText.AppendLine("}");
            if (renderParts.Length > 1)
            {
                newText.AppendLine("@if (!Model.renderTop) {");
                newText.AppendLine(string.Join("\r\n", renderParts[1].Split("\r\n".ToCharArray()).Select(x => x.Trim().StartsWith("@") ? x : $"@:{x}")));
                newText.AppendLine("}");
            }
            text = newText.ToString();
        }

        return text;
    }
}

public enum BoxoutType
{
    Standard,
    Bordered
}

public enum CardSize
{
    Quarter,
    Third,
    Half,
    Full
}