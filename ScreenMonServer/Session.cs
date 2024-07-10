using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScreenMonServer
{
    [Index(nameof(Ip), IsUnique = false)]
    [Index(nameof(Mac), IsUnique = false)]
    public class Session
    {
        [Key] public Guid Id { get; set; }
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;
        [Required, StringLength(15, ErrorMessage = "Ipv4 address should be no more than 15 characters")] public required string Ip { get; set; }
        [Required, StringLength(12, ErrorMessage = "Mac address should be no more than 12 characters")] public required string Mac { get; set; }
        [Required] public required DateTime LoginTime { get; set; }
    }
}
