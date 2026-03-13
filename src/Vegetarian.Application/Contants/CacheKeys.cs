using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Contants
{
    public class CacheKeys
    {
        public static string UserAddresses(Guid userId)
            => $"user:address:{userId}:";

        public const string USER_ADDRESS_PREFIX = "user:address:";

        public const string CATEGORIES_PREFIX = "category:all";
        public static string MenuDetail(Guid menuId)
            => $"menu:detail:{menuId}";

        public const string VOUCHER_ACTIVE = "voucher:active";
        public static string VoucherDetail(Guid voucherId)
            => $"voucher:detail:{voucherId}";

        public const string ORDERS_ADMIN_PREFIX = "order:admin:";

        public static string UserDetail(Guid userId)
            => $"user:detail:{userId}";

        public static string ADVERTISEMENT_ACTIVE = "advertisement:active";

        public static string ADVERTISEMENT_PREFIX = "advertisement";

        public static string RelatedMenus(Guid menuId)
            => $"menu:related:{menuId}";

    }
}
