using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

    public static void RegisterComponent<T>(
        TempDataDictionary components,
        string name, 
        Func<T, MvcHtmlString> start,
        Func<T, MvcHtmlString> end)
    {
        if (!components.ContainsKey(name))
        {
            components.Add(
                name,
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
        if (components.TryGetValue(name, out var component))
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
        if (components.TryGetValue(name, out var component))
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
                razorView.Render(viewContext, viewWriter);
            }

            var doc = Encoding.UTF8.GetString(memStream.ToArray());



            writer.Write(doc);
        }
    }
}