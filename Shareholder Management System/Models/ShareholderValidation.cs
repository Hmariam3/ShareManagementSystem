using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Shareholder_Management_System.Models
{
    [MetadataType(typeof(ShareholderValidation))]
    public partial class Shareholder
    {
        private class ShareholderValidation
        {
            [Required(ErrorMessage = "Share ID is required.")]
            [Display(Name = "Shareholder ID")]
            public int ShareID { get; set; }

            //[StringLength(100, ErrorMessage = "Full name in English cannot exceed 100 characters.")]
            [Required(ErrorMessage = "Full name in English is required.")]
            [Display(Name = "Full Name (English)")]
            public string FullNameEng { get; set; }

            [Required(ErrorMessage = "Maqaa Guutuu galchaa")]
            [Display(Name = "Maqaa Guutuu")]
            public string FullNameAfanOromo { get; set; }

            [Required(ErrorMessage = "ሙሉ ስም ያስገቡ")]
            [Display(Name = "ሙሉ ስም")]
            public string FullNameAmharic { get; set; }

            [Required(ErrorMessage = "Nationality is required.")]
            [Display(Name = "Nationality")]
            public string Nationality { get; set; } = "Ethiopian"; 

            [Required(ErrorMessage = "Shareholder category is required.")]
            [Display(Name = "Shareholder Category")]
            public string SHCategory { get; set; }

            //[Required(ErrorMessage = "TIN Number is required.")]
            [RegularExpression(@"^[0-9]*$", ErrorMessage = "TIN number must be a numeric value.")]
            [Display(Name = "TIN Number")]
            public string TINNum { get; set; }

            [Required(ErrorMessage = "Account number is required.")]
            [RegularExpression(@"^[0-9]*$", ErrorMessage = "Account number must be numeric.")]
            [StringLength(50, ErrorMessage = "Account number cannot exceed 50 characters.")]
            [Display(Name = "Account Number")]
            public string AccountNumber { get; set; }

            [Required(ErrorMessage = "Region is required.")]
            [Display(Name = "Region")]
            public string Region { get; set; }

            //[Required(ErrorMessage = "Zone is required.")]
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

            [Display(Name = "House Number")]
            public string HouseNo { get; set; }

            [Required(ErrorMessage = "Primary phone number is required.")]
            [Phone(ErrorMessage = "Invalid phone number format.")]
            [Display(Name = "Phone Number")]
            public string PhoneNo { get; set; }

            [Phone(ErrorMessage = "Invalid phone number format.")]
            [Display(Name = "Phone Number 2")]
            public string PhoneNo2 { get; set; }

            [Phone(ErrorMessage = "Invalid phone number format.")]
            [Display(Name = "Phone Number 3")]
            public string PhoneNo3 { get; set; }

            [EmailAddress(ErrorMessage = "Invalid email format.")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [EmailAddress(ErrorMessage = "Invalid email format.")]
            [Display(Name = "Alternate Email")]
            public string Email2 { get; set; }

            [EmailAddress(ErrorMessage = "Invalid email format.")]
            [Display(Name = "Alternate Email 2")]
            public string Email3 { get; set; }

            [StringLength(250, ErrorMessage = "Remark cannot exceed 250 characters.")]
            [Display(Name = "Remark")]
            public string Remark { get; set; }

        }
    }
}
