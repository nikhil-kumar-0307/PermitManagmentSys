using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PermitManagmentSystem.Models
{
    public class Permit
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        [Index("IX_Permit_PermitNo", IsUnique = true)]
        public string PermitNo { get; set; }

        [Required, StringLength(200)]
        public string WorkLocation { get; set; }

        [Required, StringLength(100)]
        public string Department { get; set; }

        [Required, StringLength(1000)]
        public string Description { get; set; }

        [Required, StringLength(150)]
        public string PermitIssueBy { get; set; }

        [Required, StringLength(150)]
        public string PermitIssueTo { get; set; }

        [Column(TypeName = "date")]
        public DateTime IssueDate { get; set; }

        // Original file name shown to the user (e.g. "hot-work-form.pdf")
        [StringLength(255)]
        public string AttachmentFileName { get; set; }

        // Random name used on disk (Guid + extension) - null when there is no attachment
        [StringLength(100)]
        public string AttachmentStoredName { get; set; }

        [StringLength(100)]
        public string CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }
    }
}