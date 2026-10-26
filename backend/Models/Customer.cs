#nullable enable

using System.ComponentModel.DataAnnotations;

namespace MiniERP.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Emri është i detyrueshëm.")]
        [StringLength(150, ErrorMessage = "Emri nuk mund të ketë më shumë se 150 karaktere.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(254, ErrorMessage = "Email-i nuk mund të ketë më shumë se 254 karaktere.")]
        [EmailAddress(ErrorMessage = "Email-i nuk është në format të vlefshëm.")]
        public string? Email { get; set; }

        [StringLength(30, ErrorMessage = "Telefoni nuk mund të ketë më shumë se 30 karaktere.")]
        public string? Phone { get; set; }
    }
}
