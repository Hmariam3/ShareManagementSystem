using Shareholder_Management_System.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shareholder_Management_System.ViewModel
{
    public class UserAuditLogViewModel
    {
        // User data
        public User User { get; set; }

        // Latest AuditLog data
        public string Message { get; set; }
        public string ActionType { get; set; }
        public int TransactionID { get; set; }
        public string TableName { get; set; }
        public string PerformedBy { get; set; }
        public string PerformerBranch { get; set; }
        public DateTime TransactionDate { get; set; }
    }
}