using Shareholder_Management_System.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shareholder_Management_System.Controllers
{
    public class AuditLogsController : Controller
    {
        private readonly Shareholder_Management_SystemEntities1 _context;

        public AuditLogsController()
        {
            _context = new Shareholder_Management_SystemEntities1();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RecordLog(string actionType, int transactionId, string tableName, int performedBy, string performerBranch)
        {
            // Generate the Message based on the ActionType and TableName
            string message = GenerateMessage(actionType, tableName);

            // Create a new instance of AuditLog
            var auditLog = new AuditLog
            {
                Message = message,
                ActionType = actionType,
                TransactionID = transactionId,
                TableName = tableName,
                PerformedBy = performedBy,
                PerformerBranch = performerBranch,
                TransactionDate = DateTime.Now // Set the current date and time
            };

            // Add the new audit log entry to the context
            _context.AuditLogs.Add(auditLog);

            // Save changes to the database
            _context.SaveChanges();

            // Redirect to Index or return success message
            return RedirectToAction("Index");
        }

        private string GenerateMessage(string actionType, string tableName)
        {
            // Generate a message based on the action type
            switch (actionType.ToLower())
            {
                case "register":
                    return $"{tableName} successfully registered.";
                case "update":
                    return $"{tableName} successfully updated.";
                case "delete":
                    return $"{tableName} successfully deleted.";
                case "approve":
                    return $"{tableName} successfully approved.";
                // Add other cases as needed
                default:
                    return $"{tableName} action performed.";
            }
        }

        //// GET: AuditLogs
        //public ActionResult Index()
        //{
        //    return View();
        //}
    }
}
