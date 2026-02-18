using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Request
{
    public class RatingRequestDto
    {
        public Guid MenuId { get; set; }
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public int Stars { get; set; }
        public string? Comment { get; set; }
        public List<IFormFile> Images { get; set; } = new List<IFormFile>();
    }
}
