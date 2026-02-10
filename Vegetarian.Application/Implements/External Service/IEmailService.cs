using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Implements.Hangfire
{
    public interface IEmailService
    {
        public Task EmailSender(string toEmail, string subject, string body);
    }
}
