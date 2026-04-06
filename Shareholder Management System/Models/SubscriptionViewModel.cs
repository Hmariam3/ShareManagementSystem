using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shareholder_Management_System.Models
{
    public class SubscriptionViewModel
    {
        public IQueryable<Subscribtion> Subscriptions { get; set; }
        public SelectList ShareholderList { get; set; }
        public string SelectedShareholder { get; set; }
    }
}