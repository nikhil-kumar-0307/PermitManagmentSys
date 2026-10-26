namespace PermitManagmentSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UseDepartmentName : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "DepartmentName", c => c.String(maxLength: 150));
            DropColumn("dbo.Users", "DepartmentId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Users", "DepartmentId", c => c.Int());
            DropColumn("dbo.Users", "DepartmentName");
        }
    }
}
