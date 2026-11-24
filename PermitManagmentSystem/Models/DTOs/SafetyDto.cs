using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PermitManagmentSystem.Models.DTOs
{
    public class SafetyDashboardDto
    {
        public int TotalTasks { get; set; }
        public int AwaitingSteward { get; set; }
        public int StewardAssigned { get; set; }
        public int WorkersOnPermits { get; set; }

        public List<SafetyTaskRowDto> LatestAwaiting { get; set; } = new List<SafetyTaskRowDto>();
        public List<DepartmentCountDto> ByDepartment { get; set; } = new List<DepartmentCountDto>();
        public List<DayCountDto> LastDays { get; set; } = new List<DayCountDto>();
    }

    public class SafetyTaskRowDto
    {
        public int TaskId { get; set; }
        public int PermitId { get; set; }
        public string PermitNo { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string WorkLocation { get; set; }
        public string SupervisorName { get; set; }
        public DateTime WorkStartDate { get; set; }
        public DateTime ExpectedEndDate { get; set; }
        public int WorkerCount { get; set; }
        public string SafetyStewardName { get; set; }
        public string SafetyStewardMobile { get; set; }
        public DateTime? ClosedOn { get; set; }          // closed by the department

        public bool HasSteward { get { return !string.IsNullOrEmpty(SafetyStewardName); } }
        public bool IsClosed { get { return ClosedOn.HasValue; } }
    }

    public class SafetyTaskListDto
    {
        public List<SafetyTaskRowDto> Items { get; set; } = new List<SafetyTaskRowDto>();
        public string Query { get; set; }
        public string Status { get; set; }   // "", "pending", "done"
    }

    // read-only worker shown to Safety
    public class SafetyWorkerDto
    {
        public string PassNo { get; set; }
        public string WorkerName { get; set; }
    }

    public class SafetyStewardDto
    {
        public int TaskId { get; set; }

        // ---- read-only (filled by the controller) ----
        public int PermitId { get; set; }
        public string PermitNo { get; set; }
        public string DepartmentName { get; set; }
        public string WorkLocation { get; set; }
        public string Description { get; set; }
        public string PermitIssueBy { get; set; }
        public string PermitIssueTo { get; set; }
        public DateTime IssueDate { get; set; }
        public string AttachmentFileName { get; set; }
        public string SupervisorName { get; set; }
        public DateTime WorkStartDate { get; set; }
        public DateTime ExpectedEndDate { get; set; }
        public List<SafetyWorkerDto> Workers { get; set; } = new List<SafetyWorkerDto>();
        public bool IsEdit { get; set; }

        // department has closed the job - Safety can only view it
        public bool IsClosed { get; set; }
        public DateTime? ClosedOn { get; set; }

        // ---- the fields Safety fills ----
        [Required(ErrorMessage = "Safety steward is required")]
        [StringLength(150, ErrorMessage = "Safety steward can be up to 150 characters")]
        [Display(Name = "Safety Steward")]
        public string SafetyStewardName { get; set; }

        [Required(ErrorMessage = "Mobile no is required")]
        [StringLength(15, ErrorMessage = "Mobile no can be up to 15 characters")]
        [RegularExpression(@"^\+?[0-9\s\-]{10,15}$", ErrorMessage = "Enter a valid mobile no (10 to 15 digits)")]
        [Display(Name = "Mobile no")]
        public string SafetyStewardMobile { get; set; }
    }
}