namespace PermitManagmentSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPermitTask : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PermitTasks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PermitId = c.Int(nullable: false),
                        SupervisorName = c.String(nullable: false, maxLength: 150),
                        WorkStartDate = c.DateTime(nullable: false, storeType: "date"),
                        ExpectedEndDate = c.DateTime(nullable: false, storeType: "date"),
                        AssignedBy = c.String(maxLength: 100),
                        CreatedOn = c.DateTime(nullable: false),
                        UpdatedOn = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Permits", t => t.PermitId, cascadeDelete: true)
                .Index(t => t.PermitId, unique: true, name: "IX_PermitTask_PermitId");
            
            CreateTable(
                "dbo.PermitWorkers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PermitTaskId = c.Int(nullable: false),
                        PassNo = c.String(nullable: false, maxLength: 50),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PermitTasks", t => t.PermitTaskId, cascadeDelete: true)
                .Index(t => new { t.PermitTaskId, t.PassNo }, unique: true, name: "IX_PermitWorker_Task_Pass");
            
            AddColumn("dbo.Users", "DepartmentId", c => c.Int());
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PermitWorkers", "PermitTaskId", "dbo.PermitTasks");
            DropForeignKey("dbo.PermitTasks", "PermitId", "dbo.Permits");
            DropIndex("dbo.PermitWorkers", "IX_PermitWorker_Task_Pass");
            DropIndex("dbo.PermitTasks", "IX_PermitTask_PermitId");
            DropColumn("dbo.Users", "DepartmentId");
            DropTable("dbo.PermitWorkers");
            DropTable("dbo.PermitTasks");
        }
    }
}
