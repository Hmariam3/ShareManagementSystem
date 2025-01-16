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
using System.IO;
using Shareholder_Management_System.ViewModel;

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
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int decryptedId;

            try
            {
                decryptedId = int.Parse(TripleDESEncryptionHelper.Decrypt(id));
            }
            catch
            {
                return HttpNotFound();
            }

            User user = db.Users.Find(decryptedId);
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
                user.IsFirstLogin = true;
                user.Locked = 0;
                user.CreatedDate = DateTime.UtcNow;
                user.Status = true;

                int id = Convert.ToInt32(Session["ID"]);

                // Add the new user to the database
                db.Users.Add(user);
                db.SaveChanges();

                // Call RecordLog method
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("register", user.UID, "User", id, Session["BranchName"].ToString());


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
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int decryptedId;

            try
            {
                decryptedId = int.Parse(TripleDESEncryptionHelper.Decrypt(id));
            }
            catch
            {
                return HttpNotFound();
            }

            User user = db.Users.Find(decryptedId);
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
                auditLogsController.RecordLog("edit", user.UID, "User", id, Session["BranchName"].ToString());

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
        public ActionResult Profile(string id)
        {
            if (id == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int decryptedId;

            try
            {
                decryptedId = int.Parse(TripleDESEncryptionHelper.Decrypt(id));
            }
            catch
            {
                return HttpNotFound();
            }

            // User information
            User user = db.Users.Find(decryptedId);
            if (user == null)
            {
                // Store a temporary message
                TempData["Message"] = "User not found.";

                // Redirect to another action or return a view
                return RedirectToAction("Index");  // or any other action/view
            }


            // User Activity: Retrieve a list of audit logs for the user
            List<AuditLog> auditLogs = db.AuditLogs
                                        .Where(a => a.PerformedBy == decryptedId)
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
            //Capturing Session User ID
            int userId2 = Convert.ToInt32(Session["ID"]);

            string encryptedId = TripleDESEncryptionHelper.Encrypt(userId2.ToString());
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid data.";
                return RedirectToAction("Profile", "Users", new { id = encryptedId });
            }

            int userId = Convert.ToInt32(Session["ID"]);

            var user = db.Users.SingleOrDefault(u => u.UID == userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Profile", "Users", new { id = encryptedId }); // Redirect to Profile page with error message
            }

            // Hash the current password to compare with stored hash
            var hashedCurrentPassword = _passwordHasher.HashPassword(resetPasswordModel.CurrentPassword);
            if (user.Password != hashedCurrentPassword)
            {
                TempData["ErrorMessage"] = "Current password is incorrect.";
                return RedirectToAction("Profile", "Users", new { id = encryptedId }); // Redirect to Profile page with error message
            }

            // Check if new password and confirm password match
            if (resetPasswordModel.NewPassword != resetPasswordModel.ConfirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirm password do not match.";
                return RedirectToAction("Profile", "Users", new { id = encryptedId }); // Redirect to Profile page with error message
            }

            string newPassword = _passwordHasher.HashPassword(resetPasswordModel.NewPassword);

            if (user.Password == newPassword)
            {
                TempData["ErrorMessage"] = "Current password and new password match.";
                return RedirectToAction("Profile", "Users", new { id = encryptedId }); // Redirect to Profile page with error message
            }

            // Update the user's password (hash the new password)
            user.Password = newPassword;
            db.SaveChanges();

            TempData["SuccessMessage"] = "Password reset successfully.";

            return RedirectToAction("Profile", "Users", new { id = encryptedId }); // Redirect to Profile page after successful reset
        }

        public ActionResult View(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int decryptedId;

            try
            {
                decryptedId = int.Parse(TripleDESEncryptionHelper.Decrypt(id));
            }
            catch
            {
                return HttpNotFound();
            }
            // Fetch the user based on the provided ID

            int userID = Convert.ToInt32(Session["ID"]);
            string encryptedId = TripleDESEncryptionHelper.Encrypt(userID.ToString());

            string encodedId = HttpUtility.UrlEncode(encryptedId);

            var user = db.Users.Find(decryptedId);
            if (user == null)
            {
                TempData["Message"] = "User not found.";
                return RedirectToAction("Profile", "Users", new { id = encryptedId });
            }

            // Fetch the latest audit log for this user based on TransactionDate
            var latestAuditLog = db.AuditLogs
                                  .Where(a => a.TransactionID == decryptedId)
                                  .OrderByDescending(a => a.TransactionDate)
                                  .FirstOrDefault();

            // Fetch the user who performed the action based on PerformedBy ID
            var performedByUser = latestAuditLog != null
                                  ? db.Users.Find(latestAuditLog.PerformedBy)
                                  : null;

            // Prepare the ViewModel
            var viewModel = new UserAuditLogViewModel
            {
                User = user,
                Message = latestAuditLog?.Message,
                ActionType = latestAuditLog?.ActionType,
                TransactionID = latestAuditLog?.TransactionID ?? 0,
                TableName = latestAuditLog?.TableName,
                PerformedBy = performedByUser?.UserName ?? "Unknown", // Display the UserName of the one who performed
                PerformerBranch = latestAuditLog?.PerformerBranch,
                TransactionDate = latestAuditLog?.TransactionDate ?? DateTime.MinValue
            };

            return View(viewModel);
        }

        // GET: Users/Reset
        public ActionResult Reset(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int decryptedId;

            try
            {
                decryptedId = int.Parse(TripleDESEncryptionHelper.Decrypt(id));
            }
            catch
            {
                return HttpNotFound();
            }
            // Find the user by ID
            var user = db.Users.SingleOrDefault(u => u.UID == decryptedId); // Assuming UID is the user's ID in the table

            if (user != null)
            {
                // Create the new password in the required format
                string currentYear = DateTime.Now.Year.ToString();
                string newPassword = $"Coopbank@{currentYear}";
                user.Password = newPassword;

                // Hash the password and set initial user values
                user.Password = _passwordHasher.HashPassword(user.Password);
                user.IsFirstLogin = true;
                user.Locked = 0;

                // Save changes to the database
                db.SaveChanges();

                TempData["ResetMessage"] = "Password has been reset successfully!";
            }
            else
            {
                TempData["ResetMessage"] = "User not found!";
            }

            int uid = Convert.ToInt32(Session["ID"]);

            // Call RecordLog method
            AuditLogsController auditLogsController = new AuditLogsController();
            auditLogsController.RecordLog("Password Reset", user.UID, "User", uid, Session["BranchName"].ToString());

            // Redirect to some view, e.g., the user list page
            return RedirectToAction("Index");
        }

        // GET: Users/Activate
        public ActionResult Activate(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int decryptedId;

            try
            {
                decryptedId = int.Parse(TripleDESEncryptionHelper.Decrypt(id));
            }
            catch
            {
                return HttpNotFound();
            }
            // Find the user by ID
            var user = db.Users.SingleOrDefault(u => u.UID == decryptedId); // Assuming UID is the user's ID in the table

            if (user != null)
            {

                //Activate an Account that has been Inactivated Before
                user.Status = true;


                // Save changes to the database
                db.SaveChanges();

                TempData["ResetMessage"] = "Account has been activated successfully!";
            }
            else
            {
                TempData["ResetMessage"] = "User not found!";
            }

            int uid = Convert.ToInt32(Session["ID"]);

            // Call RecordLog method
            AuditLogsController auditLogsController = new AuditLogsController();
            auditLogsController.RecordLog("Account Activated", user.UID, "User", uid, Session["BranchName"].ToString());

            // Redirect to some view, e.g., the user list page
            return RedirectToAction("Index");
        }

        // GET: Users/Inactivate
        public ActionResult Inactivate(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int decryptedId;

            try
            {
                decryptedId = int.Parse(TripleDESEncryptionHelper.Decrypt(id));
            }
            catch
            {
                return HttpNotFound();
            }
            // Find the user by ID
            var user = db.Users.SingleOrDefault(u => u.UID == decryptedId); // Assuming UID is the user's ID in the table

            if (user != null)
            {

                //Activate an Account that has been Inactivated Before
                user.Status = false;


                // Save changes to the database
                db.SaveChanges();

                TempData["ResetMessage"] = "Account has been deactivated successfully!";
            }
            else
            {
                TempData["ResetMessage"] = "User not found!";
            }

            int uid = Convert.ToInt32(Session["ID"]);

            // Call RecordLog method
            AuditLogsController auditLogsController = new AuditLogsController();
            auditLogsController.RecordLog("Account Inactivated", user.UID, "User", uid, Session["BranchName"].ToString());

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