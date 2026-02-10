using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Domain.Models
{
    public class ResponseRating
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public Guid RatingId { get; set; }
        public Rating Rating { get; set; } = null!;
        public string Comment { get; set; }
        public DateTimeOffset ResponseAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
