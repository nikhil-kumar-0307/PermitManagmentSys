using System;
using System.Collections.Generic;

namespace PermitManagmentSystem.Models.DTOs
{
    public class OperationDashboardDto
    {
        public int TotalPermits { get; set; }
        public int IssuedToday { get; set; }
        public int IssuedThisMonth { get; set; }
        public int WithoutAttachment { get; set; }

        public int AwaitingFinalClose { get; set; }   // department closed, Operation must final close
        public int FinalClosed { get; set; }

        public List<PermitListDto> Recent { get; set; } = new List<PermitListDto>();
        public List<DepartmentCountDto> ByDepartment { get; set; } = new List<DepartmentCountDto>();
        public List<ClosureRowDto> FinalCloseList { get; set; } = new List<ClosureRowDto>();

        // Permits issued per day for the last 30 days (oldest first, today last)
        public List<DayCountDto> LastDays { get; set; } = new List<DayCountDto>();
    }

    // A job closed by a department, shown to Operation for the final close
    public class ClosureRowDto
    {
        public int TaskId { get; set; }
        public int PermitId { get; set; }
        public string PermitNo { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string WorkLocation { get; set; }
        public string SupervisorName { get; set; }
        public string SafetyStewardName { get; set; }
        public string SafetyStewardMobile { get; set; }
        public DateTime? ClosedOn { get; set; }          // closed by the department
        public DateTime? FinalClosedOn { get; set; }     // final closed by Operation

        public bool IsFinalClosed { get { return FinalClosedOn.HasValue; } }
        public bool CanFinalClose { get { return ClosedOn.HasValue && !FinalClosedOn.HasValue; } }
    }

    public class ClosureListDto
    {
        public List<ClosureRowDto> Items { get; set; } = new List<ClosureRowDto>();
        public string Query { get; set; }
        public string Status { get; set; }   // "", "closing", "closed"
    }

    public class DepartmentCountDto
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class DayCountDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}