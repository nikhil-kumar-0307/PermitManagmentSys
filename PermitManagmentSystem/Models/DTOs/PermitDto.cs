using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace PermitManagmentSystem.Models.DTOs
{
    // Used by the Add and Edit forms
    public class PermitDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Permit no is required")]
        [StringLength(50, ErrorMessage = "Permit no can be up to 50 characters")]
        [Display(Name = "Permit no")]
        public string PermitNo { get; set; }

        [Required(ErrorMessage = "Work location is required")]
        [StringLength(200, ErrorMessage = "Work location can be up to 200 characters")]
        [Display(Name = "Work location")]
        public string WorkLocation { get; set; }

        [Required(ErrorMessage = "Select a department")]
        [Display(Name = "Department")]
        public int? DepartmentId { get; set; }

        // Drop-down items loaded from the QMS database
        public List<SelectListItem> Departments { get; set; } = new List<SelectListItem>();

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description can be up to 1000 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Enter who issued the permit")]
        [StringLength(150)]
        [Display(Name = "Permit issued by")]
        public string PermitIssueBy { get; set; }

        [Required(ErrorMessage = "Enter who the permit is issued to")]
        [StringLength(150)]
        [Display(Name = "Permit issued to")]
        public string PermitIssueTo { get; set; }

        [Required(ErrorMessage = "Issue date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Issue date")]
        public DateTime? IssueDate { get; set; }

        [Display(Name = "Attachment")]
        public HttpPostedFileBase Attachment { get; set; }

        // Edit only
        public string ExistingFileName { get; set; }
        public bool RemoveAttachment { get; set; }
    }

    // One row in the permit table / dashboard
    public class PermitListDto
    {
        public int Id { get; set; }
        public string PermitNo { get; set; }
        public string WorkLocation { get; set; }
        public int DepartmentId { get; set; }
        public string Department { get; set; }      // name, filled from the QMS database
        public string Description { get; set; }
        public string PermitIssueBy { get; set; }
        public string PermitIssueTo { get; set; }
        public DateTime IssueDate { get; set; }
        public bool HasAttachment { get; set; }
        public string AttachmentFileName { get; set; }
    }
}