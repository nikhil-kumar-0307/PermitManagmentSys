using PermitManagmentSystem.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace PermitManagmentSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext() : base("ApplicationDbContext") { }

        public DbSet<User> Users { get; set; }
        public DbSet<Permit> Permits { get; set; }
        public DbSet<PermitTask> PermitTasks { get; set; }
        public DbSet<PermitWorker> PermitWorkers { get; set; }
    }
}