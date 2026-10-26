using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PermitManagmentSystem.Models
{
    // The work the department opens for a permit (one task per permit)
    public class PermitTask
    {
        [Key]
        public int Id { get; set; }

        [Index("IX_PermitTask_PermitId", IsUnique = true)]
        public int PermitId { get; set; }

        [ForeignKey("PermitId")]
        public virtual Permit Permit { get; set; }

        [Required, StringLength(150)]
        public string SupervisorName { get; set; }

        [Column(TypeName = "date")]
        public DateTime WorkStartDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime ExpectedEndDate { get; set; }

        [StringLength(100)]
        public string AssignedBy { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        // ---- filled by the Safety department (null until they save) ----
        [StringLength(150)]
        public string SafetyStewardName { get; set; }

        [StringLength(100)]
        public string SafetyUpdatedBy { get; set; }

        public DateTime? SafetyUpdatedOn { get; set; }

        public virtual ICollection<PermitWorker> Workers { get; set; } = new List<PermitWorker>();
    }

    // One worker pass no on a task (a task has many)
    public class PermitWorker
    {
        [Key]
        public int Id { get; set; }

        [Index("IX_PermitWorker_Task_Pass", 1, IsUnique = true)]
        public int PermitTaskId { get; set; }

        [ForeignKey("PermitTaskId")]
        public virtual PermitTask PermitTask { get; set; }

        [Required, StringLength(50)]
        [Index("IX_PermitWorker_Task_Pass", 2, IsUnique = true)]
        public string PassNo { get; set; }
    }
}