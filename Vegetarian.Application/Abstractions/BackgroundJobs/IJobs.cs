using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Abstractions.BackgroundJobs
{
    public interface IJobs
    {
        public Task RecurringDeleteExpiredOtpJob_5mins();
        public Task RecurringDeleteExpiredRefreshTokensJob_3months();
        public Task RecurringDeleteExpiredCartsJob_3hours();
        public Task RecurringPublicVouchersJob_1hour();
        public Task RecurringRetrieveVouchersJob_1hour();
        public Task RecurringResetVoucherRedemptionsJob_24hours();
        public Task RecurringDeleteNotificationsJob_1month();
        public Task ScheduleUpdateOrderExpiredJob_10mins(Guid orderId);
        public Task SchedulePublicVoucher(Guid voucherId);
        public Task ScheduleRetrieveVoucher(Guid voucherId);
    }
}
