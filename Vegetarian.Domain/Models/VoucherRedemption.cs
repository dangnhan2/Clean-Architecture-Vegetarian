using Vegetarian.Domain.Enum;


namespace Vegetarian.Domain.Models
{
    public class VoucherRedemption
    {
        public Guid Id{ get; set; }
        public Guid VoucherID { get; set; }
        public Voucher Voucher { get; set; } = null!;
        public Guid UserID { get; set; }
        public User User { get; set; } = null!;
        public Guid OrderID { get; set; }
        public Order Order { get; set; } = null!;
        public DateTimeOffset RedeemedAt { get; set; }     
        public VoucherRedemptionStatus VoucherRedemptionStatus { get; set; }
    }
}
