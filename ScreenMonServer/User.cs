using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScreenMonServer
{
    [Index(nameof(Name), IsUnique = true)]
    public class User
    {
        [Required] public int Id { get; set; }

        [Required, Length(4, 32, ErrorMessage = "Username must be between 4-32 characters")]
        public required string Name { get; set; }

        [Required, Length(8, 64, ErrorMessage = "Password must be between 8-64 characters")]
        public required string Password { get; set; }
        public DateTime LastLoginTime {get; set; }
        public virtual ICollection<Session> Sessions { get; set; } = null!;
        [NotMapped] public bool IsOnline { get; set; }
        [NotMapped] public Session? CurrentSession { get; set; }
    }
}
