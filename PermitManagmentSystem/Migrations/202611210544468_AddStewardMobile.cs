namespace PermitManagmentSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddStewardMobile : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PermitTasks", "SafetyStewardMobile", c => c.String(maxLength: 15));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PermitTasks", "SafetyStewardMobile");
        }
    }
}
