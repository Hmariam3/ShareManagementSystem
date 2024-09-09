using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Shareholder_Management_System.commons;
using Shareholder_Management_System.Models;

namespace Shareholder_Management_System.Controllers
{
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
                user.IsFirstLogin = true;
                user.Locked = 0;
                user.CreatedDate = DateTime.UtcNow;
                user.Status = true;

                // Add the new user to the database
                db.Users.Add(user);
                db.SaveChanges();

                // Success message
                TempData["Message"] = "User is registered successfully.";
                return RedirectToAction("Index");
            }

            // If model is not valid, return the form with validation errors
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchName", user.Branch);
            return View(user);
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
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", user.Branch);
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
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", user.Branch);
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
