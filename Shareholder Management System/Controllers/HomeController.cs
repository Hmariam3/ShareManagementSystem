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
            //User table
            ViewBag.countUser = db.Users.Count();
            ViewBag.countActive = db.Users.Where(c => c.Status == true).Count();
            ViewBag.countInactive = db.Users.Where(c => c.Status == false).Count();
            ViewBag.countLocked = db.Users.Where(c => c.Locked == 3).Count();
            ViewBag.countAdmin = db.Users.Where(c => c.Role == "Administrator").Count();
            ViewBag.countIntiator = db.Users.Where(c => c.Role == "Initiator").Count();
            ViewBag.countAuthorizer = db.Users.Where(c => c.Role == "Authorizer").Count();

            //Share Holders Table
            ViewBag.countShare = db.Shareholders.Count();

            var totalShareholder = ViewBag.countShare;

            // Get the current year
            var currentYear = DateTime.Now.Year;

            // Get the count of shareholders registered in the current year
            var currentYearShareholders = db.Shareholders
                .Where(st => st.CreatedDate.Value.Year == currentYear)
                .Count();

            double percentageCurrentYearShareholders = (double)currentYearShareholders / totalShareholder * 100;

            ViewBag.PercentageCurrentYearShareholders = percentageCurrentYearShareholders;

            //Subscribtions Table
            ViewBag.countSub = db.Subscribtions.Count();

            var totalSub = ViewBag.countSub;

            var currentYearSub = DateTime.Now.Year;

            var currentYearSubs = db.Subscribtions
                .Where(su => su.SubDate.Value.Year == currentYearSub)
                .Count();

            double percentageCurrentYearSubs = (double)currentYearSubs / totalSub * 100;

            ViewBag.PercentageSubs = percentageCurrentYearSubs;

            //Payment Table
            ViewBag.countPay = db.Payments.Count();

            var totalPay = ViewBag.countPay;

            var currentYearPay = DateTime.Now.Year;

            var currentYearPays = db.Payments
                .Where(pa => pa.PaymentDate.Value.Year == currentYearPay)
                .Count();

            double percentageCurrentYearPay = (double)currentYearPays / totalPay * 100;

            ViewBag.PercentagePay = percentageCurrentYearPay;

            //Transfer Table
            ViewBag.countTran = db.ShareTransfers.Count();

            var totalTranfer = ViewBag.CountTran;

            var currentYearTran = DateTime.Now.Year;

            var currentYearTrans = db.ShareTransfers
                .Where(tr => tr.CreationDate.Value.Year == currentYearTran)
                .Count();

            double percentageCurrentYearTran = (double)currentYearTrans / totalTranfer * 100;

            ViewBag.percentageTrans = percentageCurrentYearTran;

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