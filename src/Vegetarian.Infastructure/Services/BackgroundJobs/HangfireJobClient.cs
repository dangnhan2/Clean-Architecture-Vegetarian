using Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.BackgroundJobs;

namespace Vegetarian.Infrastructure.Services.BackgroundJobs
{
    public class HangfireJobClient : IHangfireJobClient
    {
        public string Enqueue<T>(Expression<Action<T>> methodCall)
        {
            return BackgroundJob.Enqueue(methodCall);
        }

        public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
        {
            return BackgroundJob.Enqueue(methodCall);
        }

        public void Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
        {
            BackgroundJob.Schedule(methodCall, delay);
        }
    }
}
