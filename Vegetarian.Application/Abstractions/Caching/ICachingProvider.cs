using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Abstractions.Caching
{
    public interface ICachingProvider
    {
        public Task<T?> GetAsync<T>(string cacheKey);
        public Task SetAsync<T>(string cacheKey, T value, TimeSpan? expiry = null);
        public Task RemoveAsync(string cacheKey);
    }
}
