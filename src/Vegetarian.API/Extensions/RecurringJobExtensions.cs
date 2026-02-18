using Hangfire;
using Vegetarian.Application.Abstractions.BackgroundJobs;

namespace Vegetarian.API.Extensions
{
    public static class RecurringJobExtensions
    {
        public static void UseRecurringJobs(this IApplicationBuilder application)
        {
            using var scope = application.ApplicationServices.CreateScope();
            var recurring = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

            recurring.AddOrUpdate<IJobs>(
             "DeleteExpiredCarts_3hours",
             j => j.RecurringDeleteExpiredCartsJob_3hours(),
             Cron.Hourly);

            recurring.AddOrUpdate<IJobs>(
                "DeleteExpiredRefreshTokens_3months",
                j => j.RecurringDeleteExpiredRefreshTokensJob_3months(),
                Cron.Hourly()); // (tên job nói 3 months nhưng cron đang hourly)

            var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

            recurring.AddOrUpdate<IJobs>(
                "PublicVouchers_24hours",
                j => j.RecurringPublicVouchersJob_1hour(),
                Cron.Hourly(),
                tz);

            recurring.AddOrUpdate<IJobs>(
                "RetrieveVouchers_24hours",
                j => j.RecurringRetrieveVouchersJob_1hour(),
                Cron.Hourly(),
                tz);

            recurring.AddOrUpdate<IJobs>(
                "ResetVoucherRedemptions_24hours",
                j => j.RecurringResetVoucherRedemptionsJob_24hours(),
                Cron.Daily(),
                tz);

            recurring.AddOrUpdate<IJobs>(
                "DeleteNotifications_1month",
                j => j.RecurringDeleteNotificationsJob_1month(),
                Cron.Monthly(),
                tz);

            recurring.AddOrUpdate<IJobs>(
                "DeleteExpiredOtpJob",
                j => j.RecurringDeleteExpiredOtpJob_5mins(),
                Cron.MinuteInterval(5));
        }
    }
}
