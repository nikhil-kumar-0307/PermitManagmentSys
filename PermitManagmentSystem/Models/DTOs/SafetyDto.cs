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
        public bool HasSteward { get { return !string.IsNullOrEmpty(SafetyStewardName); } }
    }

    public class SafetyTaskListDto
    {
        public List<SafetyTaskRowDto> Items { get; set; } = new List<SafetyTaskRowDto>();
        public string Query { get; set; }
        public string Status { get; set; }   // "", "pending", "done"
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
        public List<string> WorkerPassNos { get; set; } = new List<string>();
        public bool IsEdit { get; set; }

        // ---- the only field Safety fills ----
        [Required(ErrorMessage = "Safety steward is required")]
        [StringLength(150, ErrorMessage = "Safety steward can be up to 150 characters")]
        [Display(Name = "Safety Steward")]
        public string SafetyStewardName { get; set; }
    }
}