using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Response
{
    public class AdvertisementDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string BannerUrl { get; set; }
        public string AdTargetType { get; set; }
        public string? TargetKey { get; set; }
        public string StartAt { get; set; }
        public string EndAt { get; set; }
        public bool IsActive { get; set; }
    }
}
