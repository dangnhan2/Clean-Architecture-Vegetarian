using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Request
{
    public class MarkNotificationRequestDto
    {
        public ICollection<Guid> NotificationIds { get; set; } = new List<Guid>();
    }
}
