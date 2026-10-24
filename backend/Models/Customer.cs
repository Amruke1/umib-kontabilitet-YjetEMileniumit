#nullable enable

using System.ComponentModel.DataAnnotations;

namespace MiniERP.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(254)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(30)]
        public string? Phone { get; set; }
    }
}
