using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Abstractions.Email
{
    public interface IEmailSender
    {
        public Task Sender(string toEmail, string subject, string body);
    }
}
