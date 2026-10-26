namespace PermitManagmentSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSafetySteward : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PermitTasks", "SafetyStewardName", c => c.String(maxLength: 150));
            AddColumn("dbo.PermitTasks", "SafetyUpdatedBy", c => c.String(maxLength: 100));
            AddColumn("dbo.PermitTasks", "SafetyUpdatedOn", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.PermitTasks", "SafetyUpdatedOn");
            DropColumn("dbo.PermitTasks", "SafetyUpdatedBy");
            DropColumn("dbo.PermitTasks", "SafetyStewardName");
        }
    }
}
