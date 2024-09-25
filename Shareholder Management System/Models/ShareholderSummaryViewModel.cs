using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shareholder_Management_System.Models
{
    public class ShareholderSummaryViewModel
    {
        public string ShareholderId { get; set; }
        public string ShareholderIdd { get; set; }
        public string ShareholderName { get; set; }
        public int TotalNumberOfShares { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalUnpaidSubscription { get; set; }
    }
}