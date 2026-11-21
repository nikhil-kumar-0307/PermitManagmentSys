namespace PermitManagmentSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddWorkerName : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PermitWorkers", "WorkerName", c => c.String(maxLength: 150));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PermitWorkers", "WorkerName");
        }
    }
}
