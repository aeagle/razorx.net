using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebTest.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
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