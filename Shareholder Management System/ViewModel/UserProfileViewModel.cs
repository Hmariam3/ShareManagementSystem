using Shareholder_Management_System.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shareholder_Management_System.ViewModel
{
    public class UserProfileViewModel
    {
        public User User { get; set; }
        public List<AuditLog> AuditLogs { get; set; } // List of AuditLogs
    }

}