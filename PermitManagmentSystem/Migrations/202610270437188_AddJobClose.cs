namespace PermitManagmentSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddJobClose : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PermitTasks", "ClosedOn", c => c.DateTime());
            AddColumn("dbo.PermitTasks", "ClosedBy", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PermitTasks", "ClosedBy");
            DropColumn("dbo.PermitTasks", "ClosedOn");
        }
    }
}
