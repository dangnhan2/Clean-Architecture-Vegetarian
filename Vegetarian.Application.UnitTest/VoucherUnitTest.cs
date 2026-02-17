using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.BackgroundJobs;
using Vegetarian.Application.Abstractions.Caching;
using Vegetarian.Application.Abstractions.Persistence;
using Vegetarian.Application.Contants;
using Vegetarian.Application.Dtos.QueryParams;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Implements.Services;
using Vegetarian.Application.Validator;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.UnitTest
{
    public class VoucherUnitTest
    {
        private readonly Mock<IUnitOfWork> _uow;
        private readonly Mock<ICachingProvider> _cache;
        private readonly Mock<IVoucherRepo> _voucherRepoMock;
        private readonly Mock<IHangfireJobClient> _mockHangfireJobClient;
        private readonly VoucherService _sut;
        private const decimal TAX_RATE = 8;

        public VoucherUnitTest()
        {
            _uow = new Mock<IUnitOfWork>();
            _cache = new Mock<ICachingProvider>();
            _voucherRepoMock = new Mock<IVoucherRepo>();
            _mockHangfireJobClient = new Mock<IHangfireJobClient>();

            _uow.SetupGet(uow => uow.Voucher).Returns(_voucherRepoMock.Object);

            _sut = new VoucherService(_uow.Object, _cache.Object, _mockHangfireJobClient.Object);
        }


        [Fact]
        public async Task AddVoucherAsync_ShouldThrowValidationDictionaryException_WhenRequestIsInvalid()
        {
            var request = new VoucherRequestDto
            {
                Code = "",
                DiscountType = "percent",
                DiscountValue = 10,
                IsActive = true,
                MaxDiscount = 10000,
                MinOrderAmount = 50000,
                PerUserLimit = 1,
                UsageLimit = 20,
                StartDate = DateTimeOffset.UtcNow.AddMonths(1),
                EndDate = DateTimeOffset.UtcNow.AddMonths(2),
            };

            var exception = await Assert.ThrowsAsync<ValidationDictionaryException>(
                async () => await _sut.AddAsync(request));

            exception.Should().NotBeNull();
        }

        [Fact]
        public async Task AddVoucherAsync_ShouldThrowArgumentException_WhenVoucherIsActiveButTimeIsDifferent()
        {
            var request = new VoucherRequestDto
            {
                Code = "Hello",
                DiscountType = "percent",
                DiscountValue = 10,
                IsActive = true,
                MaxDiscount = 10000,
                MinOrderAmount = 50000,
                PerUserLimit = 1,
                UsageLimit = 20,
                StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
                EndDate = DateTimeOffset.UtcNow.AddMonths(2),
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _sut.AddAsync(request));

            exception.Message.Should().Be("Thời điểm bắt đầu voucher đã qua, hãy sửa lại giờ bắt đầu phù hợp");
        }

        [Fact]
        public async Task AddVoucherAsync_ShouldAddVoucherButNotDeleteCache_WhenVoucherDoesNotActive()
        {
            var request = new VoucherRequestDto
            {
                Code = "Hello",
                DiscountType = "percent",
                DiscountValue = 10,
                IsActive = false,
                MaxDiscount = 10000,
                MinOrderAmount = 50000,
                PerUserLimit = 1,
                UsageLimit = 20,
                StartDate = DateTimeOffset.UtcNow.AddMonths(1),
                EndDate = DateTimeOffset.UtcNow.AddMonths(2),
            };

            _uow.Setup(uow => uow.SaveChangeAsync());

            await _sut.AddAsync(request);

            _uow.Verify(c => c.SaveChangeAsync(), Times.Once);
            _cache.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AddVoucherAsync_ShouldAddVoucherAndDeleteCache_WhenVoucherDoesActive()
        {
            var request = new VoucherRequestDto
            {
                Code = "Hello",
                DiscountType = "percent",
                DiscountValue = 10,
                IsActive = true,
                MaxDiscount = 10000,
                MinOrderAmount = 50000,
                PerUserLimit = 1,
                UsageLimit = 20,
                StartDate = DateTimeOffset.UtcNow.AddMonths(1),
                EndDate = DateTimeOffset.UtcNow.AddMonths(2),
            };

            _uow.Setup(uow => uow.SaveChangeAsync());

            await _sut.AddAsync(request);

            _uow.Verify(c => c.SaveChangeAsync(), Times.Once);
            _cache.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DeleteVoucherAsync_ShouldThrowKeyNotFoundException_WhenVoucherDoesNotExist()
        {
            var voucherId = Guid.NewGuid();

            _voucherRepoMock.Setup(v => v.GetByIdAsync(voucherId)).ReturnsAsync((Voucher?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _sut.DeleteAsync(voucherId));

            exception.Message.Should().Be("Mã giảm giá không tồn tại");
        }

        [Fact]
        public async Task DeleteVoucherAsync_ShouldThrowInvalidOperationException_WhenVoucherDoesActive()
        {
            var voucherId = Guid.NewGuid();

            var voucher = new Voucher
            {
                Id = voucherId,
                IsActive = true,
                Code = "Hello"
            };

            _voucherRepoMock.Setup(v => v.GetByIdAsync(voucherId)).ReturnsAsync(voucher);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _sut.DeleteAsync(voucherId));

            exception.Message.Should().Be("Mã giảm giá đang được áp dụng. Hãy cập nhật lại trước khi xóa");
        }

        [Fact]
        public async Task DeleteVoucherAsync_ShouldDeleteVoucher_WhenRequestIsValid()
        {
            var voucherId = Guid.NewGuid();

            var voucher = new Voucher
            {
                Id = voucherId,
                IsActive = false,
                Code = "Hello"
            };

            _voucherRepoMock.Setup(v => v.GetByIdAsync(voucherId)).ReturnsAsync(voucher);
            _uow.Setup(uow => uow.SaveChangeAsync());

            await _sut.DeleteAsync(voucherId);

            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Once);
            _cache.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateVoucherAsync_ShouldThrowValidationDictionaryException_WhenRequestIsInvalid()
        {
            var voucherId = Guid.NewGuid();
            var request = new VoucherRequestDto
            {
                Code = "",
                DiscountType = "percent",
                DiscountValue = 10,
                IsActive = false,
                MaxDiscount = 10000,
                MinOrderAmount = 50000,
                PerUserLimit = 1,
                UsageLimit = 20,
                StartDate = DateTimeOffset.UtcNow.AddMonths(1),
                EndDate = DateTimeOffset.UtcNow.AddMonths(2),
            };

            _uow.Setup(uow => uow.SaveChangeAsync());

            var exception = await Assert.ThrowsAsync<ValidationDictionaryException>(
                async () => await _sut.UpdateAsync(voucherId, request));

            _uow.Verify(c => c.SaveChangeAsync(), Times.Never);
            _cache.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateVoucherAsync_ShouldThrowArgumentException_WhenRequestIsInvalid()
        {
            var voucherId = Guid.NewGuid();
            var request = new VoucherRequestDto
            {
                Code = "Hello",
                DiscountType = "percent",
                DiscountValue = 10,
                IsActive = true,
                MaxDiscount = 10000,
                MinOrderAmount = 50000,
                PerUserLimit = 1,
                UsageLimit = 20,
                StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
                EndDate = DateTimeOffset.UtcNow.AddMonths(2),
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _sut.UpdateAsync(voucherId, request));

            exception.Message.Should().Be("Thời điểm bắt đầu voucher đang khác với giờ hiện tại, hãy sửa lại giờ bắt đầu phù hợp");

            _uow.Verify(c => c.SaveChangeAsync(), Times.Never);
            _cache.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateVoucherAsync_ShouldUpdateVoucher_WhenRequestIsValid()
        {
            var voucherId = Guid.NewGuid();
            var request = new VoucherRequestDto
            {
                Code = "Hello",
                DiscountType = "percent",
                DiscountValue = 10,
                IsActive = true,
                MaxDiscount = 10000,
                MinOrderAmount = 50000,
                PerUserLimit = 1,
                UsageLimit = 20,
                StartDate = DateTimeOffset.UtcNow.AddMonths(1),
                EndDate = DateTimeOffset.UtcNow.AddMonths(2),
            };

            var voucher = new Voucher
            {
                Id = voucherId,
                IsActive = false,
                Code = "Hello",
                DiscountType = "percent",
                DiscountValue = 10,
                MaxDiscount = 10000,
                MinOrderAmount = 50000,
                PerUserLimit = 1,
                UsageLimit = 20,
                StartDate = DateTimeOffset.UtcNow.AddMonths(1),
                EndDate = DateTimeOffset.UtcNow.AddMonths(2),
            };

            _voucherRepoMock.Setup(v => v.GetByIdAsync(voucherId)).ReturnsAsync(voucher);
            _uow.Setup(uow => uow.SaveChangeAsync());

            await _sut.UpdateAsync(voucherId, request);

            voucher.IsActive.Should().Be(request.IsActive);
            voucher.DiscountType.Should().Be(request.DiscountType);
            voucher.DiscountValue.Should().Be(request.DiscountValue);
            voucher.MaxDiscount.Should().Be(request.MaxDiscount);
            voucher.MinOrderAmount.Should().Be(request.MinOrderAmount);
            voucher.PerUserLimit.Should().Be(request.PerUserLimit);
            voucher.UsageLimit.Should().Be(request.UsageLimit);
            voucher.StartDate.Should().Be(request.StartDate);
            voucher.EndDate.Should().Be(request.EndDate);

            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Once);
            _cache.Verify(c => c.RemoveAsync(CacheKeys.VOUCHER_ACTIVE), Times.Once);
            _cache.Verify(c => c.RemoveAsync(CacheKeys.VoucherDetail(voucherId)), Times.Once);
        }

        [Fact]
        public async Task UpdateVoucherAsync_ShouldUpdateVoucher_WhenRequestIsValidAndStatusIsSimilar()
        {
            var voucherId = Guid.NewGuid();
            var request = new VoucherRequestDto
            {
                Code = "Hello",
                DiscountType = "percent",
                DiscountValue = 10,
                IsActive = false,
                MaxDiscount = 10000,
                MinOrderAmount = 50000,
                PerUserLimit = 1,
                UsageLimit = 20,
                StartDate = DateTimeOffset.UtcNow.AddMonths(1),
                EndDate = DateTimeOffset.UtcNow.AddMonths(2),
            };

            var voucher = new Voucher
            {
                Id = voucherId,
                IsActive = false,
                Code = "Hello1",
                DiscountType = "percent",
                DiscountValue = 10,
                MaxDiscount = 10000,
                MinOrderAmount = 50000,
                PerUserLimit = 1,
                UsageLimit = 20,
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddMonths(2),
            };

            _voucherRepoMock.Setup(v => v.GetByIdAsync(voucherId)).ReturnsAsync(voucher);
            _uow.Setup(uow => uow.SaveChangeAsync());

            await _sut.UpdateAsync(voucherId, request);

            voucher.IsActive.Should().Be(request.IsActive);
            voucher.DiscountType.Should().Be(request.DiscountType);
            voucher.DiscountValue.Should().Be(request.DiscountValue);
            voucher.MaxDiscount.Should().Be(request.MaxDiscount);
            voucher.MinOrderAmount.Should().Be(request.MinOrderAmount);
            voucher.PerUserLimit.Should().Be(request.PerUserLimit);
            voucher.UsageLimit.Should().Be(request.UsageLimit);
            voucher.StartDate.Should().Be(request.StartDate);
            voucher.EndDate.Should().Be(request.EndDate);

            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Once);
            _cache.Verify(c => c.RemoveAsync(CacheKeys.VOUCHER_ACTIVE), Times.Never);
            _cache.Verify(c => c.RemoveAsync(CacheKeys.VoucherDetail(voucherId)), Times.Once);
        }

        [Fact]
        public async Task GetAllByAdminAsync_ShouldReturnVoucherInPagination_WhenRequestIsValid()
        {
            var vouchers = new List<Voucher>
            {
                new Voucher
                {
                    Id = Guid.NewGuid(),
                    IsActive = false,
                    Code = "Hello1",
                    DiscountType = "percent",
                    DiscountValue = 10,
                    MaxDiscount = 10000,
                    MinOrderAmount = 50000,
                    PerUserLimit = 1,
                    UsageLimit = 20,
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = DateTimeOffset.UtcNow.AddMonths(2),
                },
                new Voucher
                {
                    Id = Guid.NewGuid(),
                    IsActive = false,
                    Code = "Hello2",
                    DiscountType = "percent",
                    DiscountValue = 10,
                    MaxDiscount = 10000,
                    MinOrderAmount = 50000,
                    PerUserLimit = 1,
                    UsageLimit = 20,
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = DateTimeOffset.UtcNow.AddMonths(2),
                },
                new Voucher
                {
                    Id = Guid.NewGuid(),
                    IsActive = false,
                    Code = "Hello3",
                    DiscountType = "percent",
                    DiscountValue = 10,
                    MaxDiscount = 10000,
                    MinOrderAmount = 50000,
                    PerUserLimit = 1,
                    UsageLimit = 20,
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = DateTimeOffset.UtcNow.AddMonths(2),
                },
            };

            var voucherParams = new VoucherParams
            {
                Page = 1,
                PageSize = 2
            };

            var queryable = vouchers.BuildMockDbSet();

            _voucherRepoMock.Setup(v => v.GetAll()).Returns(queryable.Object);
            var result = await _sut.GetAllByAdminAsync(voucherParams);

            result.Total.Should().Be(3);
            result.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllByAdminAsync_ShouldReturnVoucher_WhenPageOrPageSizeIsZero()
        {
            var vouchers = new List<Voucher>
            {
                new Voucher
                {
                    Id = Guid.NewGuid(),
                    IsActive = false,
                    Code = "Hello1",
                    DiscountType = "percent",
                    DiscountValue = 10,
                    MaxDiscount = 10000,
                    MinOrderAmount = 50000,
                    PerUserLimit = 1,
                    UsageLimit = 20,
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = DateTimeOffset.UtcNow.AddMonths(2),
                },
                new Voucher
                {
                    Id = Guid.NewGuid(),
                    IsActive = false,
                    Code = "Hello2",
                    DiscountType = "percent",
                    DiscountValue = 10,
                    MaxDiscount = 10000,
                    MinOrderAmount = 50000,
                    PerUserLimit = 1,
                    UsageLimit = 20,
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = DateTimeOffset.UtcNow.AddMonths(2),
                },
                new Voucher
                {
                    Id = Guid.NewGuid(),
                    IsActive = false,
                    Code = "Hello3",
                    DiscountType = "percent",
                    DiscountValue = 10,
                    MaxDiscount = 10000,
                    MinOrderAmount = 50000,
                    PerUserLimit = 1,
                    UsageLimit = 20,
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = DateTimeOffset.UtcNow.AddMonths(2),
                },
            };

            var voucherParams = new VoucherParams
            {
                Page = 0,
                PageSize = 2
            };

            var queryable = vouchers.BuildMockDbSet();

            _voucherRepoMock.Setup(v => v.GetAll()).Returns(queryable.Object);
            var result = await _sut.GetAllByAdminAsync(voucherParams);

            result.Total.Should().Be(3);
            result.Data.Should().HaveCount(3);
        }

        [Fact]
        public async Task ValidateVoucherAsync_ShouldReturnCorrectCalculation_WhenValidPercentVoucher()
        {
            // Arrange
            var voucherId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var voucher = new Voucher
            {
                Id = voucherId,
                Code = "DISCOUNT20",
                DiscountType = "percent",
                DiscountValue = 20,
                MaxDiscount = 100000,
                MinOrderAmount = 50000,
                UsageLimit = 100,
                UsedCount = 10,
                PerUserLimit = 3,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            var cart = new Cart
            {
                UserId = userId,
                CartItems = new List<CartItem>
                {
                  new CartItem { UnitPrice = 60000, Quantity = 1 }
                }
            };

            var request = new ValidationVoucherRequestDto
            {
                VoucherId = voucherId,
                UserId = userId
            };

            _uow.Setup(x => x.Voucher.GetByIdAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Voucher, bool>>>()))
                .ReturnsAsync(voucher);

            _uow.Setup(x => x.Cart.GetCartByCustomerAsync(userId))
                .ReturnsAsync(cart);

            _uow.Setup(x => x.VoucherRedemption.TodayCountAsync(userId, voucherId))
                .ReturnsAsync(0);

            // Act
            var result = await _sut.ValidateVoucherAsync(request);

            result.DiscountAmount.Should().Be(12960);
            result.TotalAmount.Should().Be(51840);
        }

        [Fact]
        public async Task ValidateVoucherAsync_ShouldReturnCorrectCalculation_WhenValidFixedAmountVoucher()
        {
            // Arrange
            var voucherId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var voucher = new Voucher
            {
                Id = voucherId,
                Code = "FIXED50K",
                DiscountType = "fixed",
                DiscountValue = 50000,
                MaxDiscount = 50000,
                MinOrderAmount = 100000,
                UsageLimit = 100,
                UsedCount = 10,
                PerUserLimit = 5,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            var cart = new Cart
            {
                UserId = userId,
                CartItems = new List<CartItem>
                {
                  new CartItem { UnitPrice = 150000, Quantity = 1 }
                }
            };

            var request = new ValidationVoucherRequestDto
            {
                VoucherId = voucherId,
                UserId = userId
            };

            _uow.Setup(x => x.Voucher.GetByIdAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Voucher, bool>>>()))
                .ReturnsAsync(voucher);

            _uow.Setup(x => x.Cart.GetCartByCustomerAsync(userId))
                .ReturnsAsync(cart);

            _uow.Setup(x => x.VoucherRedemption.TodayCountAsync(userId, voucherId))
                .ReturnsAsync(0);

            // Act
            var result = await _sut.ValidateVoucherAsync(request);

            result.DiscountAmount.Should().Be(50000);
            result.TotalAmount.Should().Be(112000);
        }

        [Fact]
        public async Task ValidateVoucherAsync_ShouldCapAtMaxDiscount_WhenDiscountExceedsMaxDiscount()
        {
            // Arrange
            var voucherId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var voucher = new Voucher
            {
                Id = voucherId,
                Code = "BIGDISCOUNT",
                DiscountType = "percent",
                DiscountValue = 50,
                MaxDiscount = 30000, // Cap at 30k
                MinOrderAmount = 50000,
                UsageLimit = 100,
                UsedCount = 10,
                PerUserLimit = 3,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            var cart = new Cart
            {
                UserId = userId,
                CartItems = new List<CartItem>
                {
                  new CartItem { UnitPrice = 200000, Quantity = 1 }
                }
            };

            var request = new ValidationVoucherRequestDto
            {
                VoucherId = voucherId,
                UserId = userId
            };

            _uow.Setup(x => x.Voucher.GetByIdAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Voucher, bool>>>()))
                .ReturnsAsync(voucher);

            _uow.Setup(x => x.Cart.GetCartByCustomerAsync(userId))
                .ReturnsAsync(cart);

            _uow.Setup(x => x.VoucherRedemption.TodayCountAsync(userId, voucherId))
                .ReturnsAsync(0);

            // Act
            var result = await _sut.ValidateVoucherAsync(request);

            result.DiscountAmount.Should().Be(30000);
            result.TotalAmount.Should().Be(186000);
        }

        [Fact]
        public async Task ValidateVoucherAsync_ShouldThrowKeyNotFoundException_WhenVoucherNotFound()
        {
            // Arrange
            var request = new ValidationVoucherRequestDto
            {
                VoucherId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            _uow.Setup(x => x.Voucher.GetByIdAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Voucher, bool>>>()))
                .ReturnsAsync((Voucher?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sut.ValidateVoucherAsync(request));

            exception.Message.Should().Be("Mã giảm giá không tồn tại");
        }

        [Fact]
        public async Task ValidateVoucherAsync_ShouldThrowKeyNotFoundException_WhenCartIsEmpty()
        {
            // Arrange
            var voucherId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var voucher = new Voucher
            {
                Id = voucherId,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30),
                UsageLimit = 100,
                UsedCount = 10,
                IsActive = true
            };

            var request = new ValidationVoucherRequestDto
            {
                VoucherId = voucherId,
                UserId = userId
            };

            _uow.Setup(x => x.Voucher.GetByIdAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Voucher, bool>>>()))
                .ReturnsAsync(voucher);

            _uow.Setup(x => x.Cart.GetCartByCustomerAsync(userId))
                .ReturnsAsync((Cart)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sut.ValidateVoucherAsync(request));

            exception.Message.Should().Be("Giỏ hàng trống / không tồn tại");
        }

        [Fact]
        public async Task ValidateVoucherAsync_ShouldThrowKeyNotFoundException_WhenCartHasNoItems()
        {
            // Arrange
            var voucherId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var voucher = new Voucher
            {
                Id = voucherId,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30),
                UsageLimit = 100,
                UsedCount = 10,
                IsActive = true
            };

            var cart = new Cart
            {
                UserId = userId,
                CartItems = new List<CartItem>()
            };

            var request = new ValidationVoucherRequestDto
            {
                VoucherId = voucherId,
                UserId = userId
            };

            _uow.Setup(x => x.Voucher.GetByIdAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Voucher, bool>>>()))
                .ReturnsAsync(voucher);

            _uow.Setup(x => x.Cart.GetCartByCustomerAsync(userId))
                .ReturnsAsync(cart);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sut.ValidateVoucherAsync(request));

            exception.Message.Should().Be("Giỏ hàng trống / không tồn tại");
        }

        [Fact]
        public async Task ValidateVoucherAsync_ShouldThrowInvalidDataException_WhenUserExceededDailyLimit()
        {
            // Arrange
            var voucherId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var voucher = new Voucher
            {
                Id = voucherId,
                PerUserLimit = 2,
                MinOrderAmount = 0,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30),
                UsageLimit = 100,
                UsedCount = 10,
                IsActive = true
            };

            var cart = new Cart
            {
                UserId = userId,
                CartItems = new List<CartItem>
                {
                  new CartItem { UnitPrice = 100000, Quantity = 1 }
                }
            };

            var request = new ValidationVoucherRequestDto
            {
                VoucherId = voucherId,
                UserId = userId
            };

            _uow.Setup(x => x.Voucher.GetByIdAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Voucher, bool>>>()))
                .ReturnsAsync(voucher);

            _uow.Setup(x => x.Cart.GetCartByCustomerAsync(userId))
                .ReturnsAsync(cart);

            _uow.Setup(x => x.VoucherRedemption.TodayCountAsync(userId, voucherId))
                .ReturnsAsync(2); // Already used 2 times today

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
                _sut.ValidateVoucherAsync(request));

            exception.Message.Should().Be("Bạn đã sử dụng voucher này hôm nay rồi");
        }

        [Fact]
        public async Task ValidateVoucherAsync_ShouldThrowInvalidDataException_WhenOrderBelowMinimum()
        {
            // Arrange
            var voucherId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var voucher = new Voucher
            {
                Id = voucherId,
                MinOrderAmount = 100000,
                PerUserLimit = 3,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30),
                UsageLimit = 100,
                UsedCount = 10,
                IsActive = true
            };

            var cart = new Cart
            {
                UserId = userId,
                CartItems = new List<CartItem>
            {
                new CartItem { UnitPrice = 50000, Quantity = 1 } // Below minimum
            }
            };

            var request = new ValidationVoucherRequestDto
            {
                VoucherId = voucherId,
                UserId = userId
            };

            _uow.Setup(x => x.Voucher.GetByIdAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Voucher, bool>>>()))
               .ReturnsAsync(voucher);

            _uow.Setup(x => x.Cart.GetCartByCustomerAsync(userId))
                .ReturnsAsync(cart);

            _uow.Setup(x => x.VoucherRedemption.TodayCountAsync(userId, voucherId))
                .ReturnsAsync(0);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
                _sut.ValidateVoucherAsync(request));

            exception.Message.Should().Be($"Đơn hàng phải đạt giá trị tối thiểu {voucher.MinOrderAmount}");
        }

        [Fact]
        public async Task ValidateVoucherAsync_ShouldCalculateCorrectly_WithMultipleCartItems()
        {
            // Arrange
            var voucherId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var voucher = new Voucher
            {
                Id = voucherId,
                Code = "MULTI10",
                DiscountType = "percent",
                DiscountValue = 10,
                MaxDiscount = 50000,
                MinOrderAmount = 50000,
                UsageLimit = 100,
                UsedCount = 10,
                PerUserLimit = 3,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            var cart = new Cart
            {
                UserId = userId,
                CartItems = new List<CartItem>
                {
                  new CartItem { UnitPrice = 30000, Quantity = 2 }, // 60000
                  new CartItem { UnitPrice = 25000, Quantity = 3 }  // 75000
                }
            };

            var request = new ValidationVoucherRequestDto
            {
                VoucherId = voucherId,
                UserId = userId
            };

            _uow.Setup(x => x.Voucher.GetByIdAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Voucher, bool>>>()))
                .ReturnsAsync(voucher);

            _uow.Setup(x => x.Cart.GetCartByCustomerAsync(userId))
                .ReturnsAsync(cart);

            _uow.Setup(x => x.VoucherRedemption.TodayCountAsync(userId, voucherId))
                .ReturnsAsync(0);

            // Act
            var result = await _sut.ValidateVoucherAsync(request);

            result.DiscountAmount.Should().Be(14580);
            result.TotalAmount.Should().Be(131220);
        }

        [Fact]
        public async Task GetAllByCustomerAsync_ShouldReturnCachedData_WhenCacheExists()
        {
            // Arrange
            var cachedVouchers = new List<VoucherDto>
            {
               new VoucherDto { Id = Guid.NewGuid(), Code = "CACHED1" },
               new VoucherDto { Id = Guid.NewGuid(), Code = "CACHED2" }
            };

            _cache.Setup(x => x.GetAsync<IEnumerable<VoucherDto>>(CacheKeys.VOUCHER_ACTIVE))
                .ReturnsAsync(cachedVouchers);

            // Act
            var result = await _sut.GetAllByCustomerAsync();

            // Assert
            result.Should().BeEquivalentTo(cachedVouchers);
            _uow.Verify(x => x.Voucher.GetAll(), Times.Never);
        }

        [Fact]
        public async Task GetAllByCustomerAsync_ShouldQueryDatabaseAndCache_WhenCacheNotExists()
        {
            // Arrange
            var vouchers = new List<Voucher>
            {
                new Voucher
                {
                   Id = Guid.NewGuid(),
                   Code = "VOUCHER1",
                   DiscountType = "percent",
                   DiscountValue = 10,
                   IsActive = true
                },
                new Voucher
                {
                   Id = Guid.NewGuid(),
                   Code = "VOUCHER2",
                   DiscountType = "fixed",
                   DiscountValue = 50000,
                   IsActive = true
                },
                new Voucher
                {
                   Id = Guid.NewGuid(),
                   Code = "INACTIVE",
                   IsActive = false // Should be filtered out
                }
            }.BuildMockDbSet();

            _cache.Setup(x => x.GetAsync<IEnumerable<VoucherDto>>(CacheKeys.VOUCHER_ACTIVE))
                .ReturnsAsync((IEnumerable<VoucherDto>?)null);

            _uow.Setup(x => x.Voucher.GetAll())
                .Returns(vouchers.Object);

            _cache.Setup(x => x.SetAsync(
                CacheKeys.VOUCHER_ACTIVE,
                It.IsAny<IEnumerable<VoucherDto>>(),
                TimeSpan.FromMinutes(30)))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.GetAllByCustomerAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(v => v.Code != "INACTIVE");
            _cache.Verify(x => x.SetAsync(
                CacheKeys.VOUCHER_ACTIVE,
                It.IsAny<IEnumerable<VoucherDto>>(),
                TimeSpan.FromMinutes(30)), Times.Once);
        }

        [Fact]
        public async Task GetAllByCustomerAsync_ShouldOnlyReturnActiveVouchers()
        {
            // Arrange
            var vouchers = new List<Voucher>
            {
               new Voucher { Id = Guid.NewGuid(), Code = "ACTIVE1", IsActive = true },
               new Voucher { Id = Guid.NewGuid(), Code = "ACTIVE2", IsActive = true },
               new Voucher { Id = Guid.NewGuid(), Code = "INACTIVE1", IsActive = false },
               new Voucher { Id = Guid.NewGuid(), Code = "INACTIVE2", IsActive = false }
            }.BuildMockDbSet();

            _cache.Setup(x => x.GetAsync<IEnumerable<VoucherDto>>(CacheKeys.VOUCHER_ACTIVE))
                .ReturnsAsync((IEnumerable<VoucherDto>?)null);

            _uow.Setup(x => x.Voucher.GetAll())
                .Returns(vouchers.Object);

            // Act
            var result = await _sut.GetAllByCustomerAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(v => v.Code.StartsWith("ACTIVE"));
        }

        [Fact]
        public async Task GetByIdAsync_WhenCacheExists_ShouldReturnCachedData()
        {
            // Arrange
            var voucherId = Guid.NewGuid();
            var cachedVoucher = new VoucherDto
            {
                Id = voucherId,
                Code = "CACHED"
            };

            var cacheKey = CacheKeys.VoucherDetail(voucherId);

            _cache.Setup(x => x.GetAsync<VoucherDto>(cacheKey))
                .ReturnsAsync(cachedVoucher);

            // Act
            var result = await _sut.GetByIdAsync(voucherId);

            // Assert
            result.Should().BeEquivalentTo(cachedVoucher);
            _uow.Verify(x => x.Voucher.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_WhenCacheNotExists_ShouldQueryDatabaseAndCache()
        {
            // Arrange
            var voucherId = Guid.NewGuid();
            var voucher = new Voucher
            {
                Id = voucherId,
                Code = "NEWVOUCHER",
                DiscountType = "percent",
                DiscountValue = 15,
                MaxDiscount = 100000,
                MinOrderAmount = 50000,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            var cacheKey = CacheKeys.VoucherDetail(voucherId);

            _cache.Setup(x => x.GetAsync<VoucherDto>(cacheKey))
                .ReturnsAsync((VoucherDto?)null);

            _uow.Setup(x => x.Voucher.GetByIdAsync(voucherId))
                .ReturnsAsync(voucher);

            _cache.Setup(x => x.SetAsync(
                cacheKey,
                It.IsAny<VoucherDto>(),
                TimeSpan.FromHours(12)))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.GetByIdAsync(voucherId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(voucherId);
            result.Code.Should().Be("NEWVOUCHER");
            _cache.Verify(x => x.SetAsync(
                cacheKey,
                It.IsAny<VoucherDto>(),
                TimeSpan.FromHours(12)), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenVoucherNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var voucherId = Guid.NewGuid();
            var cacheKey = CacheKeys.VoucherDetail(voucherId);

            _cache.Setup(x => x.GetAsync<VoucherDto>(cacheKey))
                .ReturnsAsync((VoucherDto?)null);

            _uow.Setup(x => x.Voucher.GetByIdAsync(voucherId))
                .ReturnsAsync((Voucher?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sut.GetByIdAsync(voucherId));

            exception.Message.Should().Be("Mã giảm giá không tồn tại");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldUseCorrectCacheKey()
        {
            // Arrange
            var voucherId = Guid.NewGuid();
            var expectedCacheKey = CacheKeys.VoucherDetail(voucherId);

            var voucher = new Voucher
            {
                Id = voucherId,
                Code = "TEST",
                IsActive = true
            };

            _cache.Setup(x => x.GetAsync<VoucherDto>(expectedCacheKey))
                .ReturnsAsync((VoucherDto?)null);

            _uow.Setup(x => x.Voucher.GetByIdAsync(voucherId))
                .ReturnsAsync(voucher);

            // Act
            await _sut.GetByIdAsync(voucherId);

            // Assert
            _cache.Verify(x => x.GetAsync<VoucherDto>(expectedCacheKey), Times.Once);
            _cache.Verify(x => x.SetAsync(
                expectedCacheKey,
                It.IsAny<VoucherDto>(),
                TimeSpan.FromHours(12)), Times.Once);
        }
    }
}
