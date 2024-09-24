using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Shareholder_Management_System.Models;

namespace Shareholder_Management_System.Controllers
{
    public class HomeController : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        public ActionResult Index()
        {
            ViewBag.countUser = db.Users.Count();
            ViewBag.countActive = db.Users.Where(c => c.Status == true).Count();
            ViewBag.countInactive = db.Users.Where(c => c.Status == false).Count();
            ViewBag.countLocked = db.Users.Where(c => c.Locked == 3).Count();
            ViewBag.countAdmin = db.Users.Where(c => c.Role == "Administrator").Count();
            ViewBag.countIntiator = db.Users.Where(c => c.Role == "Initiator").Count();
            ViewBag.countAuthorizer = db.Users.Where(c => c.Role == "Authorizer").Count();
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Unauthorized()
        {
            return View();
        }
    }
}