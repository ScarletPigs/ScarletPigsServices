using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ScarletPigsServices.Data.Events
{
    public class Event
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(33)]
        public required string Name { get; set; }

        [Required]
        public required string CreatorDiscordUsername { get; set; }

        [MaxLength(12)]
        public string? Author { get; set; }

        [MaxLength(300)]
        public string? Description { get; set; }

        [Required]
        public required DateTime CreatedAt { get; set; }

        [Required]
        public required DateTime LastModified { get; set; }

        [Required]
        public required DateTime StartTime { get; set; }

        [Required]
        public required DateTime EndTime { get; set; }
        


    }
}
