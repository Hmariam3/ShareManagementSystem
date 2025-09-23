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
using Shareholder_Management_System.ViewModel;
using System.DirectoryServices;

namespace Shareholder_Management_System.Controllers
{
    public class UsersAuthorizationController : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();
        private PasswordHash _passwordHasher;

        private readonly string LdapUrl = "LDAP://10.1.72.10";
        //private readonly string LdapServiceUsername = "danielgd";
        //private readonly string LdapServicePassword = "Dan@59112116#Gela";

        public UsersAuthorizationController()
        {
            _passwordHasher = new PasswordHash();
        }

        // GET: UsersAuthorization/Login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        // POST: UsersAuthorization/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(User userData)
        {
            if (userData == null || string.IsNullOrWhiteSpace(userData.UserName) || string.IsNullOrWhiteSpace(userData.Password))
            {
                TempData["errormessage"] = "Username and password are required.";
                return View(userData);
            }

            // Check if user exists in the database
            var checkUser = db.Users
                             .Include(u => u.Branch1)
                             .FirstOrDefault(x => x.UserName.Equals(userData.UserName, StringComparison.OrdinalIgnoreCase));

            if (checkUser == null)
            {
                TempData["errormessage"] = "Account does not exist for the user.";
                return View(userData);
            }

            if (checkUser.Status == false)
            {
                TempData["errormessage"] = "Account is inactive. Please contact the administrator.";
                return View(userData);
            }

            try
            {
                // Authenticate against LDAP
                using (var entry = new DirectoryEntry(LdapUrl, userData.UserName, userData.Password))
                {
                    // Trigger LDAP bind to verify credentials
                    object nativeObject = entry.NativeObject;

                    // Set session variables
                    Session["ID"] = checkUser.UID.ToString();
                    Session["FullName"] = checkUser.FullName;
                    Session["Username"] = checkUser.UserName;
                    Session["Branch"] = checkUser.Branch;
                    Session["Roles"] = checkUser.Role;
                    Session["BranchName"] = checkUser.Branch1?.BranchName ?? "Unknown";

                    // Update active status
                    checkUser.activeStatus = true;
                    db.SaveChanges();

                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                TempData["errormessage"] = "LDAP authentication failed: " + ex.Message;
                return View(userData);
            }
        }

        [HttpGet]
        public ActionResult PasswordReset()
        {
            if (TempData["UserId"] == null)
            {
                return RedirectToAction("Login");
            }
            int userId = (int)TempData["UserId"];

            return View(new PasswordResetViewModel { UserId = userId });
        }

        [HttpPost]
        public ActionResult PasswordReset(PasswordResetViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Find the user in the database
                var user = db.Users.Find(model.UserId);
                if (user == null)
                {
                    TempData["errormessage"] = "User not found.";
                    return View(model);
                }

                var passwordHash = new PasswordHasher();
                var passwordHasher = new PasswordHash();

                // Password policy regex: Minimum 10 characters, at least one letter, one number, and one special character
                var passwordPolicyRegex = new System.Text.RegularExpressions.Regex(@"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[\W_]).{10,}$");

                if (!passwordPolicyRegex.IsMatch(model.NewPassword))
                {
                    TempData["errormessage"] = "Password must be at least 10 characters long and contain at least one letter, one number, and one special character.";
                    return View(model);
                }


                // Verify the entered password against the hashed password stored in the database
                string userHashed = passwordHasher.HashPassword(model.CurrentPassword);

                // Verify the current password
                if (!userHashed.Equals(user.Password))
                {
                    TempData["errormessage"] = "Current password is incorrect.";
                    return View(model);
                }

                // Verify new password and confirm password match
                if (model.NewPassword != model.ConfirmPassword)
                {
                    TempData["errormessage"] = "New password and confirmation do not match.";
                    return View(model);
                }

                string newPassword = _passwordHasher.HashPassword(model.NewPassword);

                // Verify new password and current passwords are not the same
                if (user.Password == newPassword)
                {
                    TempData["errormessage"] = "The new password must be different from the current password.";
                    return View(model);
                }

                // Update the user's password
                user.Password = newPassword;
                user.IsFirstLogin = false; // Mark first login as completed
                db.SaveChanges();

                TempData["SuccessMessage"] = "Password changed successfully.";
                return RedirectToAction("Login");
            }

            return View(model);
        }


        public ActionResult SignOut()
        {
            if (Session["ID"] != null && int.TryParse(Session["ID"].ToString(), out int id))
            {
                User user = db.Users.Find(id);
                if (user != null)
                {
                    user.activeStatus = false;
                    db.SaveChanges();
                }
            }
            else
            {
                // Handle the case where the session does not contain the ID or the ID is not valid
            }

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
            Session["BranchName"] = null;


            // Redirect to the Login view
            return RedirectToAction("Login", "UsersAuthorization");
        }

        [HttpPost]
        public ActionResult Heartbeat()
        {
            // Check if the session is still valid
            if (Session["ID"] != null)
            {
                // Reset the session timeout to 10 seconds
                Session.Timeout = 1; // This sets the session to expire in 1 minute, but we are using it to reset every 10 seconds

                // Optionally, return a success response
                return Json(new { success = true });
            }
            else
            {
                // Return a status indicating the session has expired
                return Json(new { success = false, message = "Session expired" });
            }
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