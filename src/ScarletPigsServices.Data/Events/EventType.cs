using System.ComponentModel.DataAnnotations;

namespace ScarletPigsServices.Data.Events
{
    public class EventType
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public required string Name { get; set; }

        [Required, MaxLength(150)]
        public required string Description { get; set; }
    }
}
