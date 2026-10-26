using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace PermitManagmentSystem.Models
{
    public static class AppRoles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Operation = "Operation";
        public const string Department = "Department";
        public const string Safety = "Safety";
    }
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Username { get; set; }

        [Required, StringLength(150)]
        public string FullName { get; set; }

        [Required, StringLength(300)]
        public string Password { get; set; }

        [Required, StringLength(50)]
        public string Role { get; set; }

        // Only for Role = "Department": the department NAME exactly as it appears in QMS
        // (Departments.Name / EmployeeMaster.Department). The id is looked up at login. Null for other roles.
        [StringLength(150)]
        public string DepartmentName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? LastLoginOn { get; set; }
    }
}