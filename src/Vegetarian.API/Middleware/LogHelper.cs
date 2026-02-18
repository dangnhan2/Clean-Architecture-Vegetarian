using Serilog;
using System.Runtime.CompilerServices;

namespace Vegetarian.API.Middleware
{
    public static class LogHelper
    {
        public static void LogError(Exception ex, object? data = null,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int line = 0,
            [CallerFilePath] string file = ""
            )
        {
            Log.Error(ex, "Lỗi trong {File}:{Line} ({Method}) {Message}", Path.GetFileName(file), line, method, ex?.InnerException?.Message ?? ex.Message);
        }

        public static void LogWarning(Exception ex, object? data = null,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int line = 0,
            [CallerFilePath] string file = ""
            )
        {
            Log.Warning(ex, "Lỗi trong {File}:{Line} ({Method}) {Message}", Path.GetFileName(file), line, method, ex.Message);
        }
    }
}
