namespace PermitManagmentSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedPermitTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Permits",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PermitNo = c.String(nullable: false, maxLength: 50),
                        WorkLocation = c.String(nullable: false, maxLength: 200),
                        Department = c.String(nullable: false, maxLength: 100),
                        Description = c.String(nullable: false, maxLength: 1000),
                        PermitIssueBy = c.String(nullable: false, maxLength: 150),
                        PermitIssueTo = c.String(nullable: false, maxLength: 150),
                        IssueDate = c.DateTime(nullable: false, storeType: "date"),
                        AttachmentFileName = c.String(maxLength: 255),
                        AttachmentStoredName = c.String(maxLength: 100),
                        CreatedBy = c.String(maxLength: 100),
                        CreatedOn = c.DateTime(nullable: false),
                        UpdatedOn = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.PermitNo, unique: true, name: "IX_Permit_PermitNo");
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.Permits", "IX_Permit_PermitNo");
            DropTable("dbo.Permits");
        }
    }
}
