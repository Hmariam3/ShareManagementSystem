using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Shareholder_Management_System.Models;
using Shareholder_Management_System.commons;
using System.Web.Security;

namespace Shareholder_Management_System.Controllers
{
    public class UsersAuthorizationController : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();
        private PasswordHash _passwordHasher;

        public UsersAuthorizationController()
        {
            _passwordHasher = new PasswordHash();
        }

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(User userData)
        {
            var checkUser = db.Users
                            .FirstOrDefault(x => x.UserName.Equals(userData.UserName));

            if (checkUser != null && checkUser.Status != false)
            {
                var passwordHash = new PasswordHasher();
                var passwordHasher = new PasswordHash();

                // Verify the entered password against the hashed password stored in the database
                string userHashed = passwordHasher.HashPassword(userData.Password);
                //var passwordVerificationResult = passwordHash.VerifyHashedPassword(checkUser.Password, userHashed);
                if (userHashed.Equals(checkUser.Password))
                {
                    if (string.IsNullOrEmpty(checkUser.UID.ToString()))
                    {
                        // Handle this case if necessary
                    }

                    //if (checkUser.IsFirstLogin != false)
                    //{
                    //    TempData["Username"] = checkUser.UserName; // Pass the username to the password change view
                    //    TempData["UserID"] = checkUser.UID; // Pass the user ID to the password change view
                    //    return RedirectToAction("ChangePassword");
                    //}

                    Session["ID"] = checkUser.UID.ToString();
                    Session["FullName"] = checkUser.FullName;
                    Session["Username"] = checkUser.UserName;
                    Session["Branch"] = checkUser.Branch;
                    Session["Roles"] = checkUser.Role;
        
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["errormessage"] = "Wrong Password. Please try again.";
                }
            }
            else
            {
                TempData["errormessage"] = "Wrong Username or Password. Please try again.";
            }
            return View();
        }

        public ActionResult SignOut()
        {
            Session.Clear();
            Session.Abandon();
                        
            // Sign out the user
            FormsAuthentication.SignOut();

            // Invalidate the authentication cookie
            HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, "");
            authCookie.Expires = DateTime.Now.AddYears(-1); // Set expiration in the past to remove the cookie
            Response.Cookies.Add(authCookie);

            Session["ID"] = null;
            Session["FullName"] = null;
            Session["Username"] = null;
            Session["Branch"] = null;
            Session["Roles"] = null;

            // Redirect to the Login view
            return RedirectToAction("Login", "UsersAuthorization");
        }

        // GET: UsersAuthorization
        public ActionResult Index()
        {
            var users = db.Users.Include(u => u.Branch1);
            return View(users.ToList());
        }

        // GET: UsersAuthorization/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // GET: UsersAuthorization/Create
        public ActionResult Create()
        {
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode");
            return View();
        }

        // POST: UsersAuthorization/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "UID,FullName,UserName,Password,Status,Role,Branch,IsFirstLogin,Locked,activeStatus,CreatedDate")] User user)
        {
            if (ModelState.IsValid)
            {
                db.Users.Add(user);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", user.Branch);
            return View(user);
        }

        // GET: UsersAuthorization/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", user.Branch);
            return View(user);
        }

        // POST: UsersAuthorization/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "UID,FullName,UserName,Password,Status,Role,Branch,IsFirstLogin,Locked,activeStatus,CreatedDate")] User user)
        {
            if (ModelState.IsValid)
            {
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", user.Branch);
            return View(user);
        }

        // GET: UsersAuthorization/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: UsersAuthorization/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            User user = db.Users.Find(id);
            db.Users.Remove(user);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
