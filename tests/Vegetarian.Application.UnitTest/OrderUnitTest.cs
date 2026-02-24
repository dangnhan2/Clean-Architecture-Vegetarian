using FluentAssertions;
using Hangfire;
using MockQueryable.Moq;
using Moq;
using RedLockNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.BackgroundJobs;
using Vegetarian.Application.Abstractions.Caching;
using Vegetarian.Application.Abstractions.Notifications;
using Vegetarian.Application.Abstractions.Payment;
using Vegetarian.Application.Abstractions.Persistence;
using Vegetarian.Application.Dtos.QueryParams;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Implements.Services;
using Vegetarian.Domain.Enum;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.UnitTest
{
    public class OrderUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IPaymentGateway> _mockPaymentGateway;
        private readonly Mock<IDistributedLockFactory> _mockRedLockFactory;
        private readonly Mock<ICachingProvider> _mockCachingService;
        private readonly Mock<INotificationSender> _mockNotificationSender;
        private readonly OrderService _orderService;
        private readonly Mock<IVoucherRedemptionRepo> _mockVoucherRedemptionRepo;
        private readonly Mock<IHangfireJobClient> _mockHangfireJobClient;

        public OrderUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockPaymentGateway = new Mock<IPaymentGateway>();
            _mockRedLockFactory = new Mock<IDistributedLockFactory>(MockBehavior.Loose);
            _mockCachingService = new Mock<ICachingProvider>();
            _mockNotificationSender = new Mock<INotificationSender>();
            _mockVoucherRedemptionRepo = new Mock<IVoucherRedemptionRepo>();
            _mockHangfireJobClient = new Mock<IHangfireJobClient>();

            _orderService = new OrderService(
                _mockUnitOfWork.Object,
                _mockPaymentGateway.Object,
                _mockRedLockFactory.Object,
                _mockCachingService.Object,
                _mockNotificationSender.Object,
                _mockHangfireJobClient.Object
            );
        }


        [Fact]
        public async Task GetAllAsync_ShouldReturnPaginatedOrders_WhenPageAndPageSizeProvided()
        {
            // Arrange
            var orders = CreateSampleOrders().BuildMockDbSet();
            var orderParams = new OrderParams { Page = 1, PageSize = 10 };

            _mockUnitOfWork.Setup(x => x.Order.GetAll())
                .Returns(orders.Object);

            // Act
            var result = await _orderService.GetAllAsync(orderParams);

            result.Data.Should().HaveCount(2);
            result.Total.Should().Be(2);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllOrders_WhenPageIsZero()
        {
            // Arrange
            var orders = CreateSampleOrders().BuildMockDbSet();
            var orderParams = new OrderParams { Page = 0, PageSize = 0 };

            _mockUnitOfWork.Setup(x => x.Order.GetAll())
                .Returns(orders.Object);

            // Act
            var result = await _orderService.GetAllAsync(orderParams);

            result.Data.Should().HaveCount(2);
            result.Total.Should().Be(2);
        }

        [Fact]
        public async Task GetAllAsync_ShouldOrderByOrderDateDescending()
        {
            // Arrange
            var orders = CreateSampleOrders().BuildMockDbSet();
            var orderParams = new OrderParams { Page = 1, PageSize = 10 };

            _mockUnitOfWork.Setup(x => x.Order.GetAll())
                .Returns(orders.Object);

            // Act
            var result = await _orderService.GetAllAsync(orderParams);

            result.Data.Should().HaveCount(2);
            result.Total.Should().Be(2);
        }

        [Fact]
        public async Task CreateOrderByQRAsync_ShouldThrowException_WhenCartIsEmpty()
        {
            // Arrange
            var request = new OrderRequestDto { UserId = Guid.NewGuid() };

            _mockUnitOfWork.Setup(x => x.Cart.GetCartByCustomerAsync(request.UserId))
                .ReturnsAsync((Cart?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async
                () => await _orderService.CreateOrderByQRAsync(request)
            );
        }

        [Fact]
        public async Task CreateOrderByQRAsync_ShouldCreateOrder_WhenCartExists()
        {
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();

            // Arrange
            var request = new OrderRequestDto
            {
                UserId = Guid.NewGuid(),
                AddressId = Guid.NewGuid(),
                Note = "",
                PaymentMethod = PaymentMethod.QR,
                TotalAmount = 1000
            };

            var cart = CreateSampleCart();
            var paymentResponse = new PaymentOrderInfoDto();

            _mockUnitOfWork.Setup(x => x.Cart.GetCartByCustomerAsync(request.UserId))
                .ReturnsAsync(cart);

            _mockUnitOfWork.Setup(x => x.VoucherRedemption.AddAsync(It.IsAny<VoucherRedemption>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.Order.AddAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.SaveChangeAsync())
                .Returns(Task.CompletedTask);

            _mockPaymentGateway.Setup(x => x.CreatePaymentLink(
                It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(paymentResponse);

            // Act
            var result = await _orderService.CreateOrderByQRAsync(request);

            // Assert
            Assert.NotNull(result);
            _mockUnitOfWork.Verify(x => x.Order.AddAsync(It.IsAny<Order>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangeAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateOrderByQRAsync_WithVoucher_ShouldApplyDiscount()
        {
            // Arrange
            var voucherId = Guid.NewGuid();
            var request = new OrderRequestDto
            {
                UserId = Guid.NewGuid(),
                VoucherId = voucherId,
                AddressId = Guid.NewGuid(),
                Note = "",
                PaymentMethod = PaymentMethod.QR,
                TotalAmount = 1000
            };

            var cart = CreateSampleCart();
            var voucher = new Voucher
            {
                Id = voucherId,
                IsActive = true,
                UsageLimit = 10,
                UsedCount = 0,
                ReservedCount = 0,
                DiscountType = "percent",
                DiscountValue = 10,
                MaxDiscount = 100
            };

            var mockRedLock = new Mock<IRedLock>();
            mockRedLock.Setup(x => x.IsAcquired).Returns(true);

            _mockUnitOfWork.Setup(x => x.Cart.GetCartByCustomerAsync(request.UserId))
                .ReturnsAsync(cart);

            _mockUnitOfWork.Setup(x => x.Voucher.GetByIdAsync(voucherId))
                .ReturnsAsync(voucher);

            _mockUnitOfWork.Setup(x => x.VoucherRedemption.AddAsync(It.IsAny<VoucherRedemption>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.Order.AddAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            _mockRedLockFactory
              .Setup(x => x.CreateLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
              .ReturnsAsync(mockRedLock.Object);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync())
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            _mockPaymentGateway.Setup(x => x.CreatePaymentLink(
                It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PaymentOrderInfoDto());

            // Act
            var result = await _orderService.CreateOrderByQRAsync(request);

            // Assert
            Assert.NotNull(result);
            _mockUnitOfWork.Verify(x => x.Voucher.Update(It.IsAny<Voucher>()), Times.Once);
            Assert.Equal(1, voucher.ReservedCount);
        }

        [Fact]
        public async Task CreateOrderByQRAsync_WithVoucher_ShouldThrowException_WhenVoucherInvalid()
        {
            // Arrange
            var voucherId = Guid.NewGuid();
            var request = new OrderRequestDto
            {
                UserId = Guid.NewGuid(),
                VoucherId = Guid.NewGuid(),
                AddressId = Guid.NewGuid(),
                PaymentMethod = PaymentMethod.QR,
                TotalAmount = 10000
            };

            var cart = CreateSampleCart();

            var mockRedLock = new Mock<IRedLock>();
            mockRedLock.Setup(x => x.IsAcquired).Returns(true);

            _mockUnitOfWork.Setup(x => x.Cart.GetCartByCustomerAsync(request.UserId))
                .ReturnsAsync(cart);

            _mockUnitOfWork.Setup(x => x.Voucher.GetByIdAsync(voucherId))
                .ReturnsAsync((Voucher?)null);

            _mockRedLockFactory
              .Setup(x => x.CreateLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
              .ReturnsAsync(mockRedLock.Object);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _orderService.CreateOrderByQRAsync(request)
            );
        }

        [Fact]
        public async Task CreateOrderByQRAsync_WithVoucher_ShouldThrowException_WhenVoucherRunsOut()
        {
            // Arrange
            var voucherId = Guid.NewGuid();
            var request = new OrderRequestDto
            {
                UserId = Guid.NewGuid(),
                VoucherId = voucherId,
                AddressId = Guid.NewGuid(),
                PaymentMethod = PaymentMethod.QR,
                TotalAmount = 10000
            };

            var cart = CreateSampleCart();
            var voucher = new Voucher
            {
                Id = voucherId,
                IsActive = true,
                Code = "Hello1",
                DiscountType = "percent",
                DiscountValue = 10,
                MaxDiscount = 10000,
                MinOrderAmount = 50000,
                PerUserLimit = 1,
                UsageLimit = 20,
                UsedCount = 20,
                ReservedCount = 0,
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddMonths(2),
            };

            var mockRedLock = new Mock<IRedLock>();
            mockRedLock.Setup(x => x.IsAcquired).Returns(true);

            _mockUnitOfWork.Setup(x => x.Cart.GetCartByCustomerAsync(request.UserId))
                .ReturnsAsync(cart);

            _mockUnitOfWork.Setup(x => x.Voucher.GetByIdAsync(voucherId))
                .ReturnsAsync(voucher);

            _mockRedLockFactory
               .Setup(x => x.CreateLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
               .ReturnsAsync(mockRedLock.Object);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidDataException>(
                () => _orderService.CreateOrderByQRAsync(request)
            );
        }

        [Fact]
        public async Task CreateOrderByQRAsync_WithVoucher_ShouldThrowException_WhenLockNotAcquired()
        {
            // Arrange
            var voucherId = Guid.NewGuid();
            var request = new OrderRequestDto
            {
                UserId = Guid.NewGuid(),
                VoucherId = voucherId,
                AddressId = Guid.NewGuid(),
                Note = "",
                PaymentMethod = PaymentMethod.QR,
                TotalAmount = 1000
            };

            var cart = CreateSampleCart();

            var mockRedLock = new Mock<IRedLock>();
            mockRedLock.Setup(x => x.IsAcquired).Returns(false);

            _mockUnitOfWork.Setup(x => x.Cart.GetCartByCustomerAsync(request.UserId))
                .ReturnsAsync(cart);

            _mockRedLockFactory
              .Setup(x => x.CreateLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
              .ReturnsAsync(mockRedLock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidDataException>(
                () => _orderService.CreateOrderByQRAsync(request)
            );
        }

        [Fact]
        public async Task CreateOrderByQRAsync_WithVoucher_ShouldRollback_WhenExceptionOccurs()
        {
            // Arrange
            var voucherId = Guid.NewGuid();
            var request = new OrderRequestDto
            {
                UserId = Guid.NewGuid(),
                VoucherId = voucherId,
                AddressId = Guid.NewGuid(),
                Note = "",
                PaymentMethod = PaymentMethod.QR,
                TotalAmount = 1000
            };

            var cart = CreateSampleCart();

            var mockRedLock = new Mock<IRedLock>();
            mockRedLock.Setup(x => x.IsAcquired).Returns(true);

            _mockUnitOfWork.Setup(x => x.Cart.GetCartByCustomerAsync(request.UserId))
                .ReturnsAsync(cart);

            _mockUnitOfWork.Setup(x => x.Voucher.GetByIdAsync(voucherId))
                .ThrowsAsync(new Exception("Database error"));

            _mockRedLockFactory
               .Setup(x => x.CreateLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
               .ReturnsAsync(mockRedLock.Object);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync())
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                () => _orderService.CreateOrderByQRAsync(request)
            );

            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateOrderByCODAsync_ShouldThrowException_WhenCartIsEmpty()
        {
            // Arrange
            var request = new OrderRequestDto { UserId = Guid.NewGuid() };

            _mockUnitOfWork.Setup(x => x.Cart.GetCartByCustomerAsync(request.UserId))
                .ReturnsAsync((Cart?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _orderService.CreateOrderByCODAsync(request)
            );
        }

        [Fact]
        public async Task CreateOrderByCODAsync_ShouldCreateOrderAndSendNotification_WhenValid()
        {
            // Arrange
            var request = new OrderRequestDto
            {
                UserId = Guid.NewGuid(),
                AddressId = Guid.NewGuid(),
                PaymentMethod = PaymentMethod.COD
            };

            var cart = CreateSampleCart();

            _mockUnitOfWork.Setup(x => x.Cart.GetCartByCustomerAsync(request.UserId))
                .ReturnsAsync(cart);

            _mockUnitOfWork.Setup(x => x.Menu.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new Menu { Id = Guid.NewGuid(), SoldQuantity = 0 });

            _mockUnitOfWork.Setup(x => x.Order.AddAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.SaveChangeAsync())
                .Returns(Task.CompletedTask);

            _mockNotificationSender.Setup(x => x.NotifyAdminWhenNewOrderCreatedAsync(
                It.IsAny<int>(), It.IsAny<decimal>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _orderService.CreateOrderByCODAsync(request);

            // Assert
            Assert.True(result != 0);
            _mockUnitOfWork.Verify(x => x.Cart.Remove(It.IsAny<Cart>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.Order.AddAsync(It.IsAny<Order>()), Times.Once);
            _mockNotificationSender.Verify(x => x.NotifyAdminWhenNewOrderCreatedAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Once);
        }


        [Fact]
        public async Task CreateOrderByCODAsync_ShouldUpdateSoldQuantity()
        {
            var menuId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            // Arrange
            var request = new OrderRequestDto
            {
                UserId = Guid.NewGuid(),
                AddressId = Guid.NewGuid(),
                PaymentMethod = PaymentMethod.COD,
                TotalAmount = 10000,
                Note = ""
            };

            var cart = CreateSampleCart();
            var cartMenu = cart.CartItems.First().Menu;
            var menu = new Menu
            {
                Id = menuId,
                SoldQuantity = 5,
                CategoryId = categoryId,
                Category = new Category
                {
                    Id = categoryId,
                    Name = "VN Food",
                }
            };

            _mockUnitOfWork.Setup(x => x.Cart.GetCartByCustomerAsync(request.UserId))
                .ReturnsAsync(cart);

            _mockUnitOfWork.Setup(x => x.Menu.GetByIdAsync(It.IsAny<Guid>()))
               .ReturnsAsync(cartMenu);

            _mockUnitOfWork.Setup(x => x.Order.AddAsync(It.IsAny<Order>()))
              .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.SaveChangeAsync())
                .Returns(Task.CompletedTask);

            _mockNotificationSender.Setup(x => x.NotifyAdminWhenNewOrderCreatedAsync(
                It.IsAny<int>(), It.IsAny<decimal>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _orderService.CreateOrderByCODAsync(request);

            // Assert
            Assert.True(cartMenu.SoldQuantity > 10);
            _mockCachingService.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task CreateOrderByCODAsync_WithVoucher_ShouldRollback_WhenExceptionOccurs()
        {
            // Arrange
            var voucherId = Guid.NewGuid();
            var request = new OrderRequestDto
            {
                UserId = Guid.NewGuid(),
                VoucherId = voucherId,
                AddressId = Guid.NewGuid(),
                PaymentMethod = PaymentMethod.QR,
                TotalAmount = 10000
            };

            var cart = CreateSampleCart();

            var mockRedLock = new Mock<IRedLock>();
            mockRedLock.Setup(x => x.IsAcquired).Returns(true);

            _mockUnitOfWork.Setup(x => x.Cart.GetCartByCustomerAsync(request.UserId))
                .ReturnsAsync(cart);

            _mockUnitOfWork.Setup(x => x.Voucher.GetByIdAsync(voucherId))
                .ThrowsAsync(new Exception("Database error"));

            _mockRedLockFactory
               .Setup(x => x.CreateLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
               .ReturnsAsync(mockRedLock.Object);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync())
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                () => _orderService.CreateOrderByCODAsync(request)
            );

            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsyncByCustomer_ShouldReturnUserOrders_WhenUserIdProvided()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orders = CreateSampleOrders().BuildMockDbSet();
            var orderParams = new OrderParams { Page = 1, PageSize = 10 };

            _mockUnitOfWork.Setup(x => x.Order.GetAll())
                .Returns(orders.Object);

            // Act
            var result = await _orderService.GetAllAsyncByCustomer(userId, orderParams);

            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllAsyncByCustomer_ShouldReturnOrdersWithRatingInfo()
        {
            var userId = Guid.NewGuid();
            var orderParams = new OrderParams { Page = 1, PageSize = 10 };
            var orders = CreateSampleOrdersWithRatings(userId).BuildMockDbSet();

            _mockUnitOfWork.Setup(x => x.Order.GetAll())
                .Returns(orders.Object);

            var result = await _orderService.GetAllAsyncByCustomer(userId, orderParams);

            result.Data.Should().NotBeNull();
        }

        private List<Order> CreateSampleOrders()
        {
            var userId = Guid.NewGuid();
            var addressId = Guid.NewGuid();

            return new List<Order>
            {
                new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    AddressId = addressId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Status = OrderStatus.Pending,
                    TotalAmount = 100,
                    OrderCode = 123456,
                    PaymentMethod = PaymentMethod.QR,
                    Address = new Address
                    {
                        Id = addressId,
                        FullName = "John Doe",
                        District = "",
                        Province = "",
                        PhoneNumber = "1234567890",
                        AddressName = "123 Main St"
                    },
                    OrderMenus = new List<OrderMenus>
                    {
                        new OrderMenus
                        {
                            Id = Guid.NewGuid(),
                            MenuId = Guid.NewGuid(),
                            Quantity = 2,
                            UnitPrice = 50,
                            Menus = new Menu
                            {
                                Id = Guid.NewGuid(),
                                Name = "Test Menu",
                                ImageUrl = "test.jpg",
                                Ratings = new List<Rating>()
                            }
                        }
                    }
                },
                new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    AddressId = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                    Status = OrderStatus.Paid,
                    TotalAmount = 200,
                    OrderCode = 654321,
                    PaymentMethod = PaymentMethod.COD,
                    Address = new Address
                    {
                        Id = Guid.NewGuid(),
                        FullName = "Jane Smith",
                        District = "",
                        Province = "",
                        PhoneNumber = "0987654321",
                        AddressName = "456 Oak Ave"
                    },
                    OrderMenus = new List<OrderMenus>()
                }
            };
        }

        private List<Order> CreateSampleOrdersWithRatings(Guid userId)
        {
            var menuId = Guid.NewGuid();

            return new List<Order>
            {
                new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    AddressId = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.UtcNow,
                    Status = OrderStatus.Paid,
                    TotalAmount = 100,
                    OrderCode = 123456,
                    Address = new Address
                    {
                        Id = Guid.NewGuid(),
                        FullName = "John Doe",
                        District = "",
                        Province = "",
                        PhoneNumber = "1234567890",
                        AddressName = "123 Main St"
                    },
                    OrderMenus = new List<OrderMenus>
                    {
                        new OrderMenus
                        {
                            Id = Guid.NewGuid(),
                            MenuId = menuId,
                            Quantity = 1,
                            UnitPrice = 100,
                            Menus = new Menu
                            {
                                Id = menuId,
                                Name = "Rated Menu",
                                ImageUrl = "rated.jpg",
                                Ratings = new List<Rating>
                                {
                                    new Rating { UserId = userId, MenuId = menuId }
                                }
                            }
                        }
                    }
                }
            };
        }

        private Cart CreateSampleCart()
        {
            var menuId = Guid.NewGuid();

            return new Cart
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                CartItems = new List<CartItem>
                {
                    new CartItem
                    {
                        Id = Guid.NewGuid(),
                        MenuId = menuId,
                        Quantity = 2,
                        UnitPrice = 50,
                        Menu = new Menu
                        {
                            Id = menuId,
                            Name = "Test Menu Item",
                            ImageUrl = "test.jpg",
                            SoldQuantity = 10,
                            CategoryId = Guid.NewGuid(),
                            Category = new Category
                            {
                                Id= Guid.NewGuid(),
                                Name = "VN Food"
                            }
                        }
                    }
                }
            };
        }
    }
}
