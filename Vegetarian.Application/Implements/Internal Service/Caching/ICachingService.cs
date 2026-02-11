using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Implements.Caching
{
    public interface ICachingService
    {
        public Task<T?> GetAsync<T>(string cacheKey);
        public Task SetAsync<T>(string cacheKey, T value, TimeSpan? expiry = null);
        public Task RemoveAsync(string cacheKey);
    }
}
