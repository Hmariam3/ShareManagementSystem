using System;

namespace Shareholder_Management_System.ViewModel
{
    public class ShareholderSubscriptionReportViewModel
    {
        public int ShID { get; set; }
        public string ShareID { get; set; }
        public string ShareholderName { get; set; }
        public int SubID { get; set; }
        public int SubscribedShare { get; set; }
        public decimal SubscriptionAmount { get; set; }
        public decimal PaidSubscription { get; set; }
        public decimal UnpaidSubscription { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BlockedAmount { get; set; }
    }
}
