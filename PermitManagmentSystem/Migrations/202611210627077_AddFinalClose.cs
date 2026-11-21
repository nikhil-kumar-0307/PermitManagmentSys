namespace PermitManagmentSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddFinalClose : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PermitTasks", "FinalClosedOn", c => c.DateTime());
            AddColumn("dbo.PermitTasks", "FinalClosedBy", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PermitTasks", "FinalClosedBy");
            DropColumn("dbo.PermitTasks", "FinalClosedOn");
        }
    }
}
