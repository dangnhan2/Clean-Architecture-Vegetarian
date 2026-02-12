using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Response
{
    public class RatingDto 
    {
        public Guid Id { get; set; }
        public Guid MenuId { get; set; }

        public string CustomerUserName { get; set; }

        public int Stars { get; set; }
        public string? Comment { get; set; }
        public string RatingAt { get; set; }
        public ICollection<string> Images { get; set; } = new List<string>();

        public string? ResponseComment { get; set; }
        public string? ResponseAt { get; set; }
        public string? AdminUserName { get; set; }
    }
}
