using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Abstractions.BackgroundJobs
{
    public interface IHangfireJobClient
    {
        string Enqueue<T>(Expression<Action<T>> methodCall);
        string Enqueue<T>(Expression<Func<T, Task>> methodCall);
        void Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);
    }
}
