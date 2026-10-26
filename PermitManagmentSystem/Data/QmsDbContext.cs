using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using PermitManagmentSystem.Models.Qms;

namespace PermitManagmentSystem.Data
{
    /// <summary>
    /// READ-ONLY access to the QMS database (departments).
    /// This context never creates, migrates or writes to the QMS database.
    /// </summary>
    public class QmsDbContext : DbContext
    {
        public QmsDbContext() : base("name=QMSDbContext")
        {
            // Never let EF create or change the QMS database from this project
            Database.SetInitializer<QmsDbContext>(null);
        }

        public DbSet<QmsDepartment> Departments { get; set; }

        public override int SaveChanges()
        {
            throw new InvalidOperationException("The QMS database is read-only from the permit system.");
        }

        // Id -> Name for every department (small table, one query)
        public Dictionary<int, string> GetDepartmentNames()
        {
            return Departments
                .Select(d => new { d.Id, d.Name })
                .ToList()
                .ToDictionary(d => d.Id, d => d.Name);
        }

        // Department name -> Id (ignores case and extra spaces). Null when there is no match.
        public int? FindDepartmentId(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var wanted = name.Trim();

            var hit = Departments
                .Select(d => new { d.Id, d.Name })
                .ToList()
                .FirstOrDefault(d => d.Name != null && string.Equals(d.Name.Trim(), wanted, StringComparison.OrdinalIgnoreCase));

            return hit == null ? (int?)null : hit.Id;
        }

        public static string NameOf(Dictionary<int, string> names, int departmentId)
        {
            string name;
            return names.TryGetValue(departmentId, out name) ? name : "Not set";
        }
    }
}