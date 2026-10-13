namespace PermitManagmentSystem.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using PermitManagmentSystem.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<PermitManagmentSystem.Data.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(PermitManagmentSystem.Data.ApplicationDbContext context)
        {
            // One login for each role. AddOrUpdate matches on Username,
            // so running update-database again will not create duplicates.
            context.Users.AddOrUpdate(
                u => u.Username,

                new User
                {
                    Username = "superadmin",
                    FullName = "Super Administrator",
                    Password = "Admin@123",
                    Role = AppRoles.SuperAdmin,
                    IsActive = true,
                    CreatedOn = DateTime.Now
                },
                new User
                {
                    Username = "operation",
                    FullName = "Operation User",
                    Password = "Ops@123",
                    Role = AppRoles.Operation,
                    IsActive = true,
                    CreatedOn = DateTime.Now
                },
                new User
                {
                    Username = "DEPT001",
                    FullName = "Department User",
                    Password = "Dept@123",
                    Role = AppRoles.Department,
                    IsActive = true,
                    CreatedOn = DateTime.Now
                },
                new User
                {
                    Username = "safty",
                    FullName = "Safety User",
                    Password = "Safe@123",
                    Role = AppRoles.Safety,
                    IsActive = true,
                    CreatedOn = DateTime.Now
                }
            );

            context.SaveChanges();
        }
    }
}