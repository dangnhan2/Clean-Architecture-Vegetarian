using Hangfire;
using Microsoft.EntityFrameworkCore;
using RedLockNet;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.BackgroundJobs;
using Vegetarian.Application.Abstractions.Caching;
using Vegetarian.Application.Abstractions.Notifications;
using Vegetarian.Application.Abstractions.Payment;
using Vegetarian.Application.Contants;
using Vegetarian.Application.Dtos.QueryParams;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Helper;
using Vegetarian.Application.Implements.Interface;
using Vegetarian.Application.Validator;
using Vegetarian.Domain.Enum;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.Implements.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IDistributedLockFactory _redLockFactory;
        private readonly int orderCode = int.Parse(DateTimeOffset.Now.ToString("ffffff"));
        private readonly ICachingProvider _cachingService;
        private readonly INotificationSender _notificationSenderServer;
        private readonly IHangfireJobClient _hangfireService;

        public OrderService(
            IUnitOfWork unitOfWork,
            IPaymentGateway paymentGateway,
            IDistributedLockFactory redLockFactory,
            ICachingProvider cachingService,
            INotificationSender notificationSenderServer,
            IHangfireJobClient hangfireService)
        {
            _unitOfWork = unitOfWork;
            _paymentGateway = paymentGateway;
            _redLockFactory = redLockFactory;
            _cachingService = cachingService;
            _notificationSenderServer = notificationSenderServer;
            _hangfireService = hangfireService;
        }

        public async Task<int> CreateOrderByCODAsync(OrderRequestDto request)
        {
            var cart = await _unitOfWork.Cart.GetCartByCustomerAsync(request.UserId) ?? throw new KeyNotFoundException("Giỏ hàng trống / không tồn tại");

            decimal totalAmount = GetSubAmount(cart.CartItems);
            var newOrder = MappingOrder(request, totalAmount);

            // add menu to order
            MappingMenuToOrder(cart.CartItems, newOrder);

            if (request.VoucherId.HasValue)
            {
                var resource = $"lock:voucher:{request.VoucherId.Value}";
                var expiry = TimeSpan.FromSeconds(30);

                Log.Information("Checking voucher running out of slot");

                await using (var redLock = await _redLockFactory.CreateLockAsync(resource, expiry))
                {
                    if (!redLock.IsAcquired)
                        throw new InvalidDataException("Hệ thống đang xử lý voucher này, vui lòng thử lại sau.");

                    await _unitOfWork.BeginTransactionAsync();

                    try
                    {
                        var voucher = await _unitOfWork.Voucher.GetByIdAsync(request.VoucherId.Value);

                        if (voucher == null || !voucher.IsActive)
                            throw new KeyNotFoundException("Voucher không hợp lệ");

                        if (voucher.UsedCount >= voucher.UsageLimit)
                            throw new InvalidDataException("Voucher đã sử dụng hết");

                        decimal discountValue = voucher.DiscountType == "percent"
                            ? totalAmount * voucher.DiscountValue / 100
                            : voucher.DiscountValue;

                        discountValue = Math.Min(discountValue, voucher.MaxDiscount);

                        totalAmount -= discountValue;

                        voucher.UsedCount++;

                        // update voucher used count if it reached limit
                        if (voucher.UsedCount >= voucher.UsageLimit)
                            voucher.IsActive = false;

                        //create voucher redemption
                        await CreateVouherRedemption(request.VoucherId.Value, request.UserId, newOrder.Id, VoucherRedemptionStatus.Used);

                        newOrder.TotalAmount = totalAmount;

                        // update voucher after increase voucher used count
                        _unitOfWork.Voucher.Update(voucher);

                        // update sold quantity 
                        await UpdateSoldQuantity(cart.CartItems);

                        _unitOfWork.Cart.Remove(cart);

                        await _unitOfWork.Order.AddAsync(newOrder);

                        await _unitOfWork.SaveChangeAsync();
                        await _unitOfWork.CommitTransactionAsync();
                    }
                    catch
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        throw;
                    }
                }
            }
            else
            {
                await UpdateSoldQuantity(cart.CartItems);
                _unitOfWork.Cart.Remove(cart);
                await _unitOfWork.Order.AddAsync(newOrder);
                await _unitOfWork.SaveChangeAsync();
            }

            Log.Information("Send notification to admin");
            await _notificationSenderServer.NotifyAdminWhenNewOrderCreatedAsync(newOrder.OrderCode, newOrder.TotalAmount);

            Log.Information("Order created");
            return newOrder.OrderCode;
        }

        public async Task<PaymentOrderInfoDto> CreateOrderByQRAsync(OrderRequestDto request)
        {
            var cart = await _unitOfWork.Cart.GetCartByCustomerAsync(request.UserId) ?? throw new KeyNotFoundException("Giỏ hàng trống / không tồn tại");

            decimal totalAmount = GetSubAmount(cart.CartItems);

            var newOrder = MappingOrder(request, totalAmount);

            // add menu to order
            MappingMenuToOrder(cart.CartItems, newOrder);

            if (request.VoucherId.HasValue)
            {
                var resource = $"lock:voucher:{request.VoucherId.Value}";
                var expiry = TimeSpan.FromSeconds(30);

                await using (var redLock = await _redLockFactory.CreateLockAsync(resource, expiry))
                {
                    if (!redLock.IsAcquired)
                        throw new InvalidDataException("Hệ thống đang xử lý voucher này, vui lòng thử lại sau.");

                    await _unitOfWork.BeginTransactionAsync();

                    try
                    {
                        var voucher = await _unitOfWork.Voucher.GetByIdAsync(request.VoucherId.Value);

                        if (voucher == null || !voucher.IsActive)
                            throw new KeyNotFoundException("Voucher không hợp lệ");

                        if (voucher.UsedCount + voucher.ReservedCount >= voucher.UsageLimit)
                            throw new InvalidDataException("Voucher đã sử dụng hết");

                        voucher.ReservedCount += 1;

                        decimal discountValue = voucher.DiscountType == "percent"
                            ? totalAmount * voucher.DiscountValue / 100
                            : voucher.DiscountValue;

                        discountValue = Math.Min(discountValue, voucher.MaxDiscount);

                        totalAmount -= discountValue;

                        _unitOfWork.Voucher.Update(voucher);

                        newOrder.TotalAmount = totalAmount;

                        //create voucher redemption
                        await CreateVouherRedemption(request.VoucherId.Value, request.UserId, newOrder.Id, VoucherRedemptionStatus.Pending);
                        await _unitOfWork.Order.AddAsync(newOrder);
                        await _unitOfWork.SaveChangeAsync();
                        await _unitOfWork.CommitTransactionAsync();
                    }
                    catch
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        throw;
                    }
                }
            }
            else
            {
                await _unitOfWork.Order.AddAsync(newOrder);
                await _unitOfWork.SaveChangeAsync();
            }

            Log.Information("Creating payment link");

            var response = await _paymentGateway.CreatePaymentLink((int)totalAmount, orderCode);

            Log.Information("Payment link created");

            _hangfireService.Schedule<IJobs>(x => x.ScheduleUpdateOrderExpiredJob_10mins(newOrder.Id),
                TimeSpan.FromMinutes(10));

            return response;
        }

        public async Task<PagingResponse<OrderDto>> GetAllAsync(OrderParams orderParams)
        {
            var orders = _unitOfWork.Order.GetAll();

            var ordersToDTO = orders
               .OrderByDescending(o => o.CreatedAt)
               .Select(o => new OrderDto
               {
                   Id = o.Id,
                   UserId = o.UserId,
                   OrderDate = o.CreatedAt.FormatDateTimeOffset(),
                   FullName = o.Address.FullName,
                   PhoneNumber = o.Address.PhoneNumber,
                   Address = o.Address.AddressName,
                   City = o.Address.Province + "," + o.Address.District,
                   Note = o.Note,
                   OrderStatus = o.Status,
                   TotalAmount = o.TotalAmount,
                   OrderCode = o.OrderCode,
                   PaymentMethod = o.PaymentMethod,
                   Menus = o.OrderMenus.Select(m => new OrderMenuDto
                   {
                       Id = m.Id,
                       MenuId = m.MenuId,
                       MenuName = m.Menus.Name,
                       MenuImage = m.Menus.ImageUrl,
                       Quantity = m.Quantity,
                       UnitPrice = m.UnitPrice,
                       SubPrice = m.UnitPrice * m.Quantity
                   }).ToList()
               })
               .AsNoTracking();

            if (orderParams.Page != 0 && orderParams.PageSize != 0)
                ordersToDTO = ordersToDTO.Paging(orderParams.Page, orderParams.PageSize);

            var response = new PagingResponse<OrderDto>(orderParams.Page, orderParams.PageSize, orders.Count(), await ordersToDTO.ToListAsync());
            return response;
        }

        public async Task<PagingResponse<OrderDto>> GetAllAsyncByCustomer(Guid userId, OrderParams orderParams)
        {
            var orders = _unitOfWork.Order.GetAll().Where(o => o.UserId == userId);

            var ordersToDTO = orders
               .OrderByDescending(o => o.CreatedAt)
               .AsNoTracking()
               .Select(o => new OrderDto
               {
                   Id = o.Id,
                   UserId = o.UserId,
                   OrderDate = o.CreatedAt.FormatDateTimeOffset(),
                   FullName = o.Address.FullName,
                   PhoneNumber = o.Address.PhoneNumber,
                   Address = o.Address.AddressName,
                   OrderStatus = o.Status,
                   TotalAmount = o.TotalAmount,
                   OrderCode = o.OrderCode,
                   PaymentMethod = o.PaymentMethod,
                   Menus = o.OrderMenus.Select(m => new OrderMenuDto
                   {
                       Id = m.Id,
                       MenuId = m.MenuId,
                       MenuName = m.Menus.Name,
                       MenuImage = m.Menus.ImageUrl,
                       Quantity = m.Quantity,
                       SubPrice = m.UnitPrice * m.Quantity,
                       IsRated = m.Menus.Ratings.Any(r => r.UserId == o.UserId && r.MenuId == m.MenuId)
                   }).ToList()
               });

            if (orderParams.Page != 0 && orderParams.PageSize != 0)
            {
                ordersToDTO = ordersToDTO.Paging(orderParams.Page, orderParams.PageSize);
            }

            return new PagingResponse<OrderDto>(orderParams.Page, orderParams.PageSize, orders.Count(), await ordersToDTO.ToListAsync());
        }

        public async Task ConfirmPaidOrderAsync(Guid orderId)
        {
            var order = await _unitOfWork.Order.GetByIdAsync(orderId) ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng");

            if (order.Status == OrderStatus.Pending) throw new InvalidDataException("Đơn hàng đang chờ khách hàng thanh toán");

            order.Status = OrderStatus.Confirmed;

            _unitOfWork.Order.Update(order);
            await _unitOfWork.SaveChangeAsync();

            await _notificationSenderServer.NotifyCustomerWhenOrderConfirmedAsync(order.UserId, order.Id, order.OrderCode);
        }

        public async Task CancelPaidOrderAsync(Guid orderId, CancelOrderRequestDto cancelOrderRequest)
        {   
            var result = await new CancelOrderRequestValidator().ValidateAsync(cancelOrderRequest);

            if (!result.IsValid)
                throw new ValidationDictionaryException(result.ToDictionary());

            var order = await _unitOfWork.Order.GetByIdAsync(orderId) ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng");

            if (order.UserId != cancelOrderRequest.UserId)
                throw new UnauthorizedAccessException("Bạn không có quyền hủy đơn hàng này");

            if (order.Status != OrderStatus.Paid)
                throw new InvalidDataException("Chỉ có thể hủy đơn hàng ở trạng thái đã thanh toán");

            var cancelDeadline = order.CreatedAt.AddMinutes(5);

            if (cancelDeadline < DateTimeOffset.UtcNow)
                throw new InvalidDataException("Đơn hàng đã hết hạn hủy");

            if (order.Status == OrderStatus.Cancelled)
                throw new InvalidDataException("Đơn hàng này đã được hủy");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                order.Status = OrderStatus.Cancelled;
                order.CancelReason = cancelOrderRequest.Reason;

                _unitOfWork.Order.Update(order);
                await _unitOfWork.SaveChangeAsync();

                if (order.PaymentMethod == PaymentMethod.QR)              
                   _hangfireService.Enqueue<IPaymentGateway>(x => x.Payout((int)order.TotalAmount, cancelOrderRequest.BankAccountNumber, cancelOrderRequest.BankBin));                          

                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }


        #region helper method
        private async Task CreateVouherRedemption(Guid voucherId, Guid userId, Guid orderId, VoucherRedemptionStatus status)
        {
            var voucherRedemption = new VoucherRedemption
            {
                Id = Guid.NewGuid(),
                VoucherID = voucherId,
                UserID = userId,
                OrderID = orderId,
                RedeemedAt = DateTimeOffset.UtcNow,
                VoucherRedemptionStatus = status
            };

            await _unitOfWork.VoucherRedemption.AddAsync(voucherRedemption);
        }

        private Order MappingOrder(OrderRequestDto request, decimal total)
        {
            var newOrder = new Order
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                AddressId = request.AddressId,
                Note = request.Note,
                TotalAmount = total,
                OrderCode = orderCode,
            };

            if (request.PaymentMethod == PaymentMethod.QR)
            {
                newOrder.PaymentMethod = PaymentMethod.QR;
                newOrder.ExpiredAt = DateTimeOffset.UtcNow.AddMinutes(10);
                newOrder.Status = OrderStatus.Pending;
            }
            else if (request.PaymentMethod == PaymentMethod.COD)
            {
                newOrder.PaymentMethod = PaymentMethod.COD;
                newOrder.ExpiredAt = null;
                newOrder.Status = OrderStatus.Paid;
            }
            return newOrder;
        }

        private async Task UpdateSoldQuantity(ICollection<CartItem> cartItems)
        {
            foreach (var item in cartItems)
            {
                var menu = await _unitOfWork.Menu.GetByIdAsync(item.MenuId);

                if (menu == null) continue;
                menu.SoldQuantity = menu.SoldQuantity + item.Quantity;
                await _cachingService.RemoveAsync(CacheKeys.MenuDetail(menu.Id));
            }
        }

        private decimal GetSubAmount(ICollection<CartItem> items)
        {
            int TAX_RATE = 8;
            decimal subTotal = 0;
            foreach (var item in items)
            {
                subTotal += item.Quantity * item.UnitPrice;
            }

            subTotal = subTotal + (subTotal * TAX_RATE) / 100;
            return subTotal;
        }

        private void MappingMenuToOrder(ICollection<CartItem> cartItems, Order order)
        {
            foreach (var item in cartItems)
            {
                var orderItem = new OrderMenus
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    MenuId = item.MenuId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    SubTotal = item.Quantity * item.UnitPrice
                };

                order.OrderMenus.Add(orderItem);
            }
        }      
        #endregion
    }
}
