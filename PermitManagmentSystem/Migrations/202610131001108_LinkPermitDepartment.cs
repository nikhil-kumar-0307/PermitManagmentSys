namespace PermitManagmentSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LinkPermitDepartment : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Permits", "DepartmentId", c => c.Int(nullable: false));
            DropColumn("dbo.Permits", "Department");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Permits", "Department", c => c.String(nullable: false, maxLength: 100));
            DropColumn("dbo.Permits", "DepartmentId");
        }
    }
}
