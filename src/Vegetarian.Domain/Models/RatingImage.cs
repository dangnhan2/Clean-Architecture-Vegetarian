using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Domain.Models
{
    public class RatingImage
    {
        public Guid Id { get; set; }
        public Guid RatingId { get; set; }
        public Rating Rating { get; set; } = null!;

        public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
    }
}
