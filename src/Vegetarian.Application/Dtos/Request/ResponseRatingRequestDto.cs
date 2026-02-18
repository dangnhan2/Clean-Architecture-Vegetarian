using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Request
{
    public class ResponseRatingRequestDto
    {
        public Guid UserId { get; set; }
        public Guid RatingId { get; set; }
        public string Comment { get; set; }
    }
}
