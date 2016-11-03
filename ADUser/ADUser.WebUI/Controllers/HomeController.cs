using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ADUser.WebUI.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult PartialHeader()
        {
            return View();
        }

        public ActionResult PartialFooter()
        {
            return View();
        }
    }
}