using System.Web.Hosting;
using System.Web.Mvc;

namespace RazorX.ViewEngine
{
    public class RazorXViewEngine : IViewEngine
    {
        public const string COMPONENT_TAG_PREFIX = "component";
        public const string PARTIAL_SPLIT_TOKEN = "@Model.children";

        public static void Initialize()
        {
            RazorXVirtualPathProvider provider = 
                new RazorXVirtualPathProvider(
                    new RazorXRegExParser()
                );

            HostingEnvironment.RegisterVirtualPathProvider(provider);
        }

        private readonly IViewEngine baseViewEngine = new RazorViewEngine();

        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            var result =
                baseViewEngine.FindPartialView(controllerContext, partialViewName, useCache);

            if (result.View != null)
            {
                return new ViewEngineResult(new RazorXView(result.View), this);
            }

            return result;
        }

        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            var result =
                baseViewEngine.FindView(controllerContext, viewName, masterName, useCache);

            if (result.View != null)
            {
                return new ViewEngineResult(new RazorXView(result.View), this);
            }

            return result;
        }

        public void ReleaseView(ControllerContext controllerContext, IView view)
        {
            baseViewEngine.ReleaseView(controllerContext, view);
        }
    }
}