using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Response;

namespace Vegetarian.Application.Implements.Interface
{
    public interface IDashboardViewService
    {
        public Task<DashboardViewDto> GetInfoAsync();
    }
}
