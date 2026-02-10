using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Helper
{
    public static class Extensions
    {
        public static IQueryable<T> Paging<T>(this IQueryable<T> values, int page, int pageSize) where T : class
        {
            return values.Skip((page - 1) * pageSize).Take(pageSize);
        }

        public static string HashToken(this string token)
        {
            var sha256 = SHA256.Create();
            var tokenBytes = Encoding.UTF8.GetBytes(token);
            var hashToken = Convert.ToBase64String(sha256.ComputeHash(tokenBytes));
            return hashToken;
        }

        public static string FormatDateTimeOffset(this DateTimeOffset dateTime)
        {
            return dateTime.ToString("MM/dd/yyyy H:mm:ss");
        }

        public static string FormatDateTime(this DateTime dateTime)
        {
            return dateTime.ToString("MM/dd/yyyy H:mm:ss");
        }
    }
}
