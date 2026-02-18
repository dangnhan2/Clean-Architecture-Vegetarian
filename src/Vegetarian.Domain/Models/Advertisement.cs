using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Domain.Enum;

namespace Vegetarian.Domain.Models
{
    public class Advertisement
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string? BannerUrl { get; set; }
        public AdTargetType AdTargetType { get; set; }
        public string? TargetKey { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset? EndAt { get; set; }
        public bool IsActive { get; set; }
    }
}
