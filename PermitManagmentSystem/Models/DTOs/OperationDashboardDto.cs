using System.Collections.Generic;

namespace PermitManagmentSystem.Models.DTOs
{
    public class OperationDashboardDto
    {
        public int TotalPermits { get; set; }
        public int IssuedToday { get; set; }
        public int IssuedThisMonth { get; set; }
        public int WithoutAttachment { get; set; }

        public List<PermitListDto> Recent { get; set; } = new List<PermitListDto>();
        public List<DepartmentCountDto> ByDepartment { get; set; } = new List<DepartmentCountDto>();
    }

    public class DepartmentCountDto
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }
}