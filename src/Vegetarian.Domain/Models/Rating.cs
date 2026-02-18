
namespace Vegetarian.Domain.Models
{
    public class Rating
    {
        public Guid Id { get; set; }
        public Guid MenuId { get; set; }
        public Menu Menu { get; set; } = null!;
        
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid OrderId { get; set; }
        public Order Order { get; set; }

        public ResponseRating ResponseRating { get; set; } 

        public int Stars { get; set; }
        public string? Comment { get; set; }

        public DateTimeOffset RatingAt { get; set; } = DateTimeOffset.UtcNow;
        public ICollection<RatingImage> Images { get; set; } = new List<RatingImage>();
    }
}
