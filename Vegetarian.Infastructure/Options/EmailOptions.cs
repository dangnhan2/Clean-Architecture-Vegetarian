using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Infrastructure.Options
{
    public sealed class EmailOptions
    {
        public string SMTP_HOST { get; set; }
        public int SMTP_PORT { get; set; }
        public bool SMTP_ENABLE_SSL { get; set; }
        public string SMTP_USERNAME { get; set; }
        public string SMTP_PASSWORD { get; set; }
        public string SMTP_FROM_EMAIL { get; set; }
        public string SMTP_FROM_NAME { get; set; }
    }
}
