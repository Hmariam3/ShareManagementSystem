using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using Microsoft.AspNetCore.Identity;
using System.Web.Mvc;
using Shareholder_Management_System.commons;
using Shareholder_Management_System.Models;
using System.Net.Mail;

namespace Shareholder_Management_System.Controllers
{
    [AdminRoleFilter]
    public class UsersController : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();
        private PasswordHash _passwordHasher;

        public UsersController()
        {
            _passwordHasher = new PasswordHash();
        }

        // GET: Users
        public ActionResult Index()
        {
            var users = db.Users.Include(u => u.Branch1);
            return View(users.ToList());
        }

        // GET: Users/Details/5
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

        // GET: Users/Create
        public ActionResult Create()
        {
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchName");
            return View();
        }

        // GET: Users/SearchBranches
        public JsonResult SearchBranches(string searchTerm)
        {
            // Check if searchTerm is not null or empty
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                // If no search term, return an empty list
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            // Search for branches based on the searchTerm
            var branches = db.Branches
                             .Where(b => b.BranchName.Contains(searchTerm))
                             .Select(b => new
                             {
                                 Value = b.ID, // This will be the value in the dropdown
                         Text = b.BranchName // This will be the displayed name in the dropdown
                     })
                             .ToList();

            // Return the result as JSON
            return Json(branches, JsonRequestBehavior.AllowGet);
        }


        // POST: Users/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "UID,FullName,UserName,Password,Status,Role,Branch,CreatedDate")] User user)
        {
            // Check if the username already exists in the database
            var existingUser = db.Users.FirstOrDefault(u => u.UserName == user.UserName);

            if (existingUser != null)
            {
                // If the username exists, show a message and return the view
                TempData["Message"] = "Username already exists.";
                ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchName", user.Branch);
                return View(user);
            }

            if (ModelState.IsValid)
            {
                // Hash the password and set initial user values
                user.Password = _passwordHasher.HashPassword(user.Password);
                user.IsFristLogin = true;
                user.Locked = 0;
                user.CreatedDate = DateTime.UtcNow;
                user.Status = true;

                int id = Convert.ToInt32(Session["ID"]);
               
                // Add the new user to the database
                db.Users.Add(user);
                db.SaveChanges();

                // Call RecordLog method
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("register", user.UID, "User", id, Session["Branch"].ToString());


                //var emailAddress1 = "hailemariam.kebede@coopbankoromiasc.com";

                //// Send a confirmation email
                //SendConfirmationEmail(user.UserName, emailAddress1);  // Add a method to send the email

                // Success message
                TempData["Message"] = "User is registered successfully.";
                return RedirectToAction("Index");
            }

            // If model is not valid, return the form with validation errors
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchName", user.Branch);
            return View(user);
        }

        // Method to send email
        private void SendConfirmationEmail(string userName, string emailAddress)
        {
            try
            {
                // Set up the email
                var fromAddress = new MailAddress("Daniel.Gelan@coopbankoromiasc.com", "Cooperative Bank of Oromia");
                var toAddress = new MailAddress(emailAddress);
                const string subject = "User Registration Successful";
                string body = $"Dear {userName},\n\nYour account has been successfully created.\n\nRegards,\nCooperative Bank of Oromia";

                // Create SMTP client
                var smtpClient = new SmtpClient
                {
                    Host = "mail.coopbankoromiasc.com",  // SMTP host
                    Port = 587,                          // SMTP port
                    EnableSsl = true,                  // Disable SSL (port 25 usually doesn't use SSL)
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential("danielgd", "Dan@59112116#Ge") // SMTP credentials
                };

                // Prepare the mail message
                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    // Send the email
                    smtpClient.Send(message);
                }
            }
            catch (Exception ex)
            {
                // Log or handle exceptions here
                TempData["Message"] = "Registration succeeded, but there was an error sending the confirmation email.";
            }
        }


        // GET: Users/Edit/5
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
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchName", user.Branch);
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "UID,FullName,UserName,Password,Status,Role,Branch,CreatedDate")] User user)
        {
            if (ModelState.IsValid)
            {
                int id = Convert.ToInt32(Session["ID"]);

                // Call RecordLog method
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("edit", user.UID, "User", id, Session["Branch"].ToString());

                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchName", user.Branch);
            return View(user);
        }

        // GET: Users/Delete/5
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

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            User user = db.Users.Find(id);
            db.Users.Remove(user);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        // GET: Users/Profile/5
        public ActionResult Profile(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // User information
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }


            // User Activity: Retrieve a list of audit logs for the user
            List<AuditLog> auditLogs = db.AuditLogs
                                        .Where(a => a.PerformedBy == id)
                                        .ToList();

            // Pass both user and audit logs to the view model
            var viewModel = new ViewModel.UserProfileViewModel
            {
                User = user,
                AuditLogs = auditLogs // Use 'auditLogs', not 'auditlogs'
            };

            return View(viewModel);
        }

        // Unauthorized action
        public ActionResult Unauthorized()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPasswordOwn(ResetPasswordViewModel resetPasswordModel)
        {
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid data.";
                return RedirectToAction("Profile", "Users", new { id = Session["ID"] });
            }

            int userId = Convert.ToInt32(Session["ID"]);

            var user = db.Users.SingleOrDefault(u => u.UID == userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Profile", "Users", new { id = Session["ID"] }); // Redirect to Profile page with error message
            }

            // Hash the current password to compare with stored hash
            var hashedCurrentPassword = _passwordHasher.HashPassword(resetPasswordModel.CurrentPassword);
            if (user.Password != hashedCurrentPassword)
            {
                TempData["ErrorMessage"] = "Current password is incorrect.";
                return RedirectToAction("Profile", "Users", new { id = Session["ID"] }); // Redirect to Profile page with error message
            }

            // Check if new password and confirm password match
            if (resetPasswordModel.NewPassword != resetPasswordModel.ConfirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirm password do not match.";
                return RedirectToAction("Profile", "Users", new { id = Session["ID"] }); // Redirect to Profile page with error message
            }

            // Update the user's password (hash the new password)
            user.Password = _passwordHasher.HashPassword(resetPasswordModel.NewPassword);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Password reset successfully.";

            return RedirectToAction("Profile", "Users", new { id = Session["ID"] }); // Redirect to Profile page after successful reset
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
