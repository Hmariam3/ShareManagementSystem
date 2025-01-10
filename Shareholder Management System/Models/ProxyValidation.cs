using System;
using System.ComponentModel.DataAnnotations;

namespace Shareholder_Management_System.Models
{
    [MetadataType(typeof(ProxyValidation))]
    public partial class Proxy
    {
        private class ProxyValidation
        {
            [Display(Name = "Proxy ID")]
            public int ProxyID { get; set; }

            [Required(ErrorMessage = "Shareholder ID is required.")]
            [Display(Name = "Shareholder ID")]
            public int ShID { get; set; }

            [Required(ErrorMessage = "Proxy Name (English) is required.")]
            [StringLength(100, ErrorMessage = "Proxy Name (English) cannot exceed 100 characters.")]
            [Display(Name = "Proxy Name (English)")]
            public string FullName { get; set; }

            [Required(ErrorMessage = "Proxy Start Date is required.")]
            [DataType(DataType.Date, ErrorMessage = "Invalid date format.")]
            [Display(Name = "Proxy Start Date")]
            public DateTime StartDate { get; set; }

            [DataType(DataType.Date, ErrorMessage = "Invalid date format.")]
            [Display(Name = "Proxy End Date")]
            public DateTime? EndDate { get; set; }

            [Required(ErrorMessage = "Nationality is required.")]
            [Display(Name = "Nationality")]
            public string Nationality { get; set; }

            [Required(ErrorMessage = "Region is required.")]
            [Display(Name = "Region")]
            public string Region { get; set; }

            [Required(ErrorMessage = "Zone is required.")]
            [Display(Name = "Zone")]
            public string Zone { get; set; }

            [Required(ErrorMessage = "City is required.")]
            [Display(Name = "City")]
            public string City { get; set; }

            [Display(Name = "Subcity")]
            public string Subcity { get; set; }

            [Display(Name = "Woreda")]
            public string Woreda { get; set; }

            [Display(Name = "Kebele")]
            public string Kebele { get; set; }

            [Required(ErrorMessage = "Primary phone number is required.")]
            [Phone(ErrorMessage = "Invalid phone number format.")]
            [Display(Name = "Phone Number")]
            public string PhoneNo { get; set; }

            [Phone(ErrorMessage = "Invalid phone number format.")]
            [Display(Name = "Alternate Phone Number")]
            public string PhoneNo2 { get; set; }

            //[EmailAddress(ErrorMessage = "Invalid email format.")]
            //[Display(Name = "Email")]
            //public string Email { get; set; }

            //[EmailAddress(ErrorMessage = "Invalid email format.")]
            //[Display(Name = "Alternate Email")]
            //public string Email2 { get; set; }

            [Display(Name = "Remark")]
            public string Remark { get; set; }
        }

    }


}
