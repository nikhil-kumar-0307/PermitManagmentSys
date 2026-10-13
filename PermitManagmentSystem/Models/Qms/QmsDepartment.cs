using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PermitManagmentSystem.Models.Qms
{
    /// <summary>
    /// Read-only copy of the QMS "Departments" table.
    /// Only the columns this project needs are mapped. The QMS project owns the table.
    /// </summary>
    [Table("Departments", Schema = "dbo")]
    public class QmsDepartment
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        public bool IsActive { get; set; }
    }
}