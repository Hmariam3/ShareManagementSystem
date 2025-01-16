using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Shareholder_Management_System.Models;

namespace Shareholder_Management_System.Controllers
{
    public class Branches1Controller : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        // GET: Branches
        public ActionResult Index()
        {
            return View(db.Branches.ToList());
        }

        // GET: Branches/Details/5
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

            Branch branch = db.Branches.Find(decryptedId);
            if (branch == null)
            {
                return HttpNotFound();
            }
            return View(branch);
        }

        // GET: Branches/Create
        public ActionResult Create()
        {
            // Fetch distinct processes and subprocesses from the Branches table
            var processes = db.Branches.Select(b => b.Process).Distinct().ToList();
            var subProcesses = db.Branches.Select(b => b.SubProcess).Distinct().ToList();

            // Pass the dropdown data to the view
            ViewBag.Processes = new SelectList(processes);
            ViewBag.SubProcesses = new SelectList(subProcesses);

            return View();
        }

        // POST: Branches/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,BranchCode,BranchName,SubProcess,Process")] Branch branch)
        {
            var existingCode = db.Branches.FirstOrDefault(u => u.BranchCode == branch.BranchCode);
            var existingName = db.Branches.FirstOrDefault(u => u.BranchName == branch.BranchName);

            if (existingCode != null)
            {
                // If the username exists, show a message and return the view
                TempData["Message"] = "Branch Code already exists.";

                // Fetch distinct processes and subprocesses from the Branches table
                var processes = db.Branches.Select(b => b.Process).Distinct().ToList();
                var subProcesses = db.Branches.Select(b => b.SubProcess).Distinct().ToList();
                // Pass the dropdown data to the view
                ViewBag.Processes = new SelectList(processes);
                ViewBag.SubProcesses = new SelectList(subProcesses);
                return View(branch);
            }

            if (existingName != null)
            {
                // If the username exists, show a message and return the view
                TempData["Message"] = "Branch Name already exists.";

                // Fetch distinct processes and subprocesses from the Branches table
                var processes = db.Branches.Select(b => b.Process).Distinct().ToList();
                var subProcesses = db.Branches.Select(b => b.SubProcess).Distinct().ToList();
                // Pass the dropdown data to the view
                ViewBag.Processes = new SelectList(processes);
                ViewBag.SubProcesses = new SelectList(subProcesses);
                return View(branch);
            }

            if (ModelState.IsValid)
            {
                db.Branches.Add(branch);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(branch);
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

            Branch branch = db.Branches.Find(decryptedId);
            if (branch == null)
            {
                return HttpNotFound();
            }


            // Fetch distinct processes and subprocesses from the Branches table
            var processes = db.Branches.Select(b => b.Process).Distinct().ToList();
            var subProcesses = db.Branches.Select(b => b.SubProcess).Distinct().ToList();

            // Pass the dropdown data to the view
            ViewBag.Processes = new SelectList(processes, branch.Process);  // Preselect the current process
            ViewBag.SubProcesses = new SelectList(subProcesses, branch.SubProcess);  // Preselect the current subprocess

            return View(branch);
        }


        // POST: Branches/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,BranchCode,BranchName,SubProcess,Process")] Branch branch)
        {
            int ids = branch.ID;
            var existingCode = db.Branches.FirstOrDefault(u => u.BranchCode == branch.BranchCode);
            var existingName = db.Branches.FirstOrDefault(u => u.BranchName == branch.BranchName);

            // If the model state is invalid, repopulate the dropdowns before returning to the view
            var processes = db.Branches.Select(b => b.Process).Distinct().ToList();
            var subProcesses = db.Branches.Select(b => b.SubProcess).Distinct().ToList();

            if (existingCode != null)
            {
                if (existingCode.ID != branch.ID)
                {
                    // If the username exists, show a message and return the view
                    TempData["Message"] = "Branch Code already exists.";

                    int uid = branch.ID;
                    // Pass the dropdown data to the view
                    ViewBag.Processes = new SelectList(processes);
                    ViewBag.SubProcesses = new SelectList(subProcesses);
                    return View(branch);
                }
            }

            if (existingName != null)
            {
                if (existingName.ID != branch.ID)
                {
                    // If the username exists, show a message and return the view
                    TempData["Message"] = "Branch Name already exists.";


                    // Pass the dropdown data to the view
                    ViewBag.Processes = new SelectList(processes);
                    ViewBag.SubProcesses = new SelectList(subProcesses);
                    return View(branch);
                }
            }

            if (ModelState.IsValid)
            {
                // Load the existing branch from the database
                var existingBranch = db.Branches.Find(branch.ID);
                if (existingBranch != null)
                {
                    // Update the properties of the existing entity
                    existingBranch.BranchCode = branch.BranchCode;
                    existingBranch.BranchCode = branch.BranchName;
                    existingBranch.SubProcess = branch.SubProcess;
                    existingBranch.Process = branch.Process;

                    db.SaveChanges();
                    TempData["Message"] = "Branch Successfully Edited.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Message"] = "Branch not found.";
                }
                return View(branch);
            }


            ViewBag.Processes = new SelectList(processes, branch.Process);
            ViewBag.SubProcesses = new SelectList(subProcesses, branch.SubProcess);

            TempData["Message"] = "Branch not successfully Registered.";

            return View(branch);
        }




        // GET: Users/Activate
        public ActionResult Delete(string id)
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
            var branchs = db.Branches.SingleOrDefault(u => u.ID == decryptedId); // Assuming UID is the user's ID in the table

            if (branchs != null)
            {

                Branch branch = db.Branches.Find(decryptedId);
                db.Branches.Remove(branch);
                db.SaveChanges();
                TempData["Message"] = "Branch has been deleted successfully!";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["ResetMessage"] = "User not found!";
            }

            int uid = Convert.ToInt32(Session["ID"]);

            // Call RecordLog method
            AuditLogsController auditLogsController = new AuditLogsController();
            auditLogsController.RecordLog("Branch Deleted", branchs.ID, "Branch", uid, Session["BranchName"].ToString());

            // Redirect to some view, e.g., the user list page
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
