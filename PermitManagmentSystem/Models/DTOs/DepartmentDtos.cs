using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PermitManagmentSystem.Models.DTOs
{
    public class DepartmentDashboardDto
    {
        public string DepartmentName { get; set; }
        public int TotalPermits { get; set; }
        public int AwaitingAssignment { get; set; }
        public int Assigned { get; set; }
        public int EndingThisWeek { get; set; }
        public int WorkersDeployed { get; set; }

        public List<DepartmentPermitRowDto> LatestAwaiting { get; set; } = new List<DepartmentPermitRowDto>();
        public List<UpcomingWorkDto> Upcoming { get; set; } = new List<UpcomingWorkDto>();
        public List<DayCountDto> LastDays { get; set; } = new List<DayCountDto>();   // reuses DayCountDto
    }

    public class DepartmentPermitRowDto
    {
        public int Id { get; set; }
        public string PermitNo { get; set; }
        public string WorkLocation { get; set; }
        public string PermitIssueBy { get; set; }
        public string PermitIssueTo { get; set; }
        public DateTime IssueDate { get; set; }
        public bool HasAttachment { get; set; }

        public bool HasTask { get; set; }
        public string SupervisorName { get; set; }
        public DateTime? ExpectedEndDate { get; set; }
        public int WorkerCount { get; set; }
    }

    public class UpcomingWorkDto
    {
        public int PermitId { get; set; }
        public string PermitNo { get; set; }
        public string WorkLocation { get; set; }
        public string SupervisorName { get; set; }
        public DateTime ExpectedEndDate { get; set; }
        public int WorkerCount { get; set; }
    }

    public class DepartmentPermitListDto
    {
        public List<DepartmentPermitRowDto> Items { get; set; } = new List<DepartmentPermitRowDto>();
        public string Query { get; set; }
        public string Status { get; set; }      // "", "pending", "assigned"
    }

    // Used by the Assign / Edit-task form
    public class AssignTaskDto
    {
        public int PermitId { get; set; }

        // ---- read-only permit info (filled by the controller, not posted) ----
        public string PermitNo { get; set; }
        public string WorkLocation { get; set; }
        public string DepartmentName { get; set; }
        public string Description { get; set; }
        public string PermitIssueBy { get; set; }
        public string PermitIssueTo { get; set; }
        public DateTime IssueDate { get; set; }
        public string AttachmentFileName { get; set; }
        public bool IsEdit { get; set; }

        // ---- fields the department fills ----
        [Required(ErrorMessage = "Work start date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Work start date")]
        public DateTime? WorkStartDate { get; set; }

        [Required(ErrorMessage = "Expected work end date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Expected work end date")]
        public DateTime? ExpectedEndDate { get; set; }

        [Required(ErrorMessage = "Supervisor name is required")]
        [StringLength(150, ErrorMessage = "Supervisor name can be up to 150 characters")]
        [Display(Name = "Supervisor name")]
        public string SupervisorName { get; set; }

        // One text box per worker, all named "WorkerPassNos"
        public List<string> WorkerPassNos { get; set; } = new List<string>();
    }
}