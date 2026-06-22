using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shareholder_Management_System.Models
{
    public class ApprovalsViewModel
    {
        public IEnumerable<Shareholder> Shareholders { get; set; }
        public IEnumerable<Proxy> Proxies { get; set; }
        public IEnumerable<Subscribtion> Subscribtions { get; set; }
        public IEnumerable<Payment> Payments { get; set; }
        public IEnumerable<ShareTransfer> ShareTransfers { get; set; }
        public IEnumerable<AddOnSub> AddOnSub { get; set; }
        public IEnumerable<Blocked> Blockeds { get; set; }
        public IEnumerable<Document> Documents { get; set; }
        public IEnumerable<Certificate> Certificates { get; set; }

    }
}