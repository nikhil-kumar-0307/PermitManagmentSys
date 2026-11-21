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
        public int AwaitingSafety { get; set; }     // work saved, Safety has not added the steward yet
        public int ReadyToClose { get; set; }       // steward assigned, department can close
        public int Closed { get; set; }             // fully closed (Safety final close done)
        public List<DepartmentPermitRowDto> ReadyList { get; set; } = new List<DepartmentPermitRowDto>();

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

        public string SafetyStewardName { get; set; }
        public string SafetyStewardMobile { get; set; }
        public DateTime? ClosedOn { get; set; }          // closed by the department
        public DateTime? FinalClosedOn { get; set; }     // final closed by Safety

        // "pending" -> department must assign work
        // "safety"  -> waiting for Safety steward
        // "ready"   -> steward assigned, department can close
        // "closing" -> department closed, waiting for Safety final close
        // "closed"  -> fully finished
        public string Stage
        {
            get
            {
                if (!HasTask) return "pending";
                if (FinalClosedOn.HasValue) return "closed";
                if (ClosedOn.HasValue) return "closing";
                if (string.IsNullOrEmpty(SafetyStewardName)) return "safety";
                return "ready";
            }
        }
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
        public string Status { get; set; }      // "", "pending", "safety", "ready", "closing", "closed"
    }

    public class WorkerInputDto
    {
        [StringLength(50)]
        public string PassNo { get; set; }

        [StringLength(150)]
        public string WorkerName { get; set; }
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

        // ---- set when Safety has assigned the steward: the work is locked (view only) ----
        public bool IsLocked { get; set; }
        public string SafetyStewardName { get; set; }
        public string SafetyStewardMobile { get; set; }

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

        // One row per worker: Workers[0].PassNo, Workers[0].WorkerName, ...
        public List<WorkerInputDto> Workers { get; set; } = new List<WorkerInputDto>();
    }
}