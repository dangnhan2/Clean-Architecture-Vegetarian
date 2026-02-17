using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using Vegetarian.Application.Abstractions.Caching;
using Vegetarian.Application.Abstractions.Persistence;
using Vegetarian.Application.Contants;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Implements.Services;
using Vegetarian.Application.Validator;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.UnitTest
{
    public class AddressUnitTest
    {
        private readonly Mock<ICachingProvider> _cacheMock;
        private readonly Mock<IAddressRepo> _addressRepoMock;
        private readonly Mock<IUnitOfWork> _uow;
        private readonly AddressService _sut;

        public AddressUnitTest()
        {
            _cacheMock = new Mock<ICachingProvider>();
            _addressRepoMock = new Mock<IAddressRepo>();
            _uow = new Mock<IUnitOfWork>();

            _uow.SetupGet(x => x.Address).Returns(_addressRepoMock.Object);

            _sut = new AddressService
             (
                 _uow.Object,
                 _cacheMock.Object
             );
        }

        [Fact]
        public async Task GetAddressesByUser_ShouldReturnCachedAddress_WhenCacheHit()
        {
            var userId = Guid.NewGuid();
            var cacheKey = CacheKeys.UserAddresses(userId);

            // mock data
            var cached = new List<AddressDto>
            {
                new AddressDto
                {
                    Id = Guid.NewGuid(),
                    Address = "Test",
                    FullName = "Nhân",
                    PhoneNumber = "0123456789",
                    District = "",
                    Province = ""
                }
            };

            var (sut, uow, addressRepo, cache) = CreateSut(MockBehavior.Strict);

            cache.Setup(x => x.GetAsync<IEnumerable<AddressDto>>(cacheKey))
                     .ReturnsAsync(cached);

            var result = await sut.GetAllByUserAsync(userId);

            // Assert
            result.Should().BeSameAs(cached);

            // verify repository dont be called => if it called -> test fail
            addressRepo.Verify(r => r.GetAll(), Times.Never);

            // verify cache dont be reset
            cache.Verify(c => c.SetAsync(It.IsAny<string>(),
                                         It.IsAny<IEnumerable<AddressDto>>(),
                                         It.IsAny<TimeSpan>()),
                             Times.Never);
        }

        [Fact]
        public async Task GetAddressesByUser_ShouldReturnAddress_WhenCacheMiss()
        {
            var userId = Guid.NewGuid();
            var cacheKey = CacheKeys.UserAddresses(userId);

            var mockData = new List<Address>
            {
               new Address
               {
                   Id = Guid.NewGuid(),
                   AddressName = "Test 1",
                   FullName = "Nhan",
                   PhoneNumber = "1234567890",
                   UserId = userId
               }
            };

            var queryableMock = mockData.BuildMockDbSet();

            _cacheMock.Setup(c => c.GetAsync<IEnumerable<AddressDto>>(cacheKey)).ReturnsAsync((IEnumerable<AddressDto>?)null);

            _addressRepoMock.Setup(a => a.GetAll()).Returns(queryableMock.Object);

            var result = (await _sut.GetAllByUserAsync(userId)).ToList();

            result.Should().HaveCount(1);

            _addressRepoMock.Verify(a => a.GetAll(), Times.Once);

            _cacheMock.Verify(c => c.SetAsync(
                cacheKey,
                It.Is<IEnumerable<AddressDto>>(v => v.Count() == 1),
                TimeSpan.FromHours(1)),
                Times.Once);
        }

        [Fact]
        public async Task AddAddress_ShouldThrowValidationException_WhenRequestIsInvalid()
        {
            var userId = Guid.NewGuid();
            var request = new AddressRequestDto
            {
                Address = "",
                FullName = "",
                PhoneNumber = "1234567890",
                UserId = userId,
            };

            await Assert.ThrowsAsync<ValidationDictionaryException>(
                async () => await _sut.AddAsync(request));

            _addressRepoMock.Verify(a => a.AddAsync(It.IsAny<Address>()), Times.Never);
            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Never);
            _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.UserAddresses(userId)), Times.Never);
        }

        [Fact]
        public async Task AddAddress_ShouldAddAddress_WhenRequestIsValid()
        {
            var userId = Guid.NewGuid();
            var request = new AddressRequestDto
            {
                Address = "Home",
                FullName = "Nhan Nguyen",
                PhoneNumber = "0123456789",
                UserId = userId,
            };

            _uow.Setup(x => x.SaveChangeAsync());

            await _sut.AddAsync(request);
            _addressRepoMock.Verify(a => a.AddAsync(It.Is<Address>(add =>
               add.AddressName == request.Address &&
               add.FullName == request.FullName &&
               add.PhoneNumber == request.PhoneNumber &&
               add.UserId == request.UserId)), Times.Once);

            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Once);
            _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.UserAddresses(userId)), Times.Once);
        }

        [Fact]
        public async Task UpdateAddress_ShouldThrowValidationException_WhenRequestIsInvalid()
        {
            var addressId = Guid.NewGuid();

            var request = new AddressRequestDto
            {
                Address = "",
                FullName = "",
                PhoneNumber = "",
                UserId = Guid.NewGuid()
            };

            await Assert.ThrowsAsync<ValidationDictionaryException>(
                async () => await _sut.UpdateAsync(addressId, request));

            _addressRepoMock.Verify(a => a.Update(It.IsAny<Address>()), Times.Never);
            _addressRepoMock.Verify(a => a.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Never);
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAddress_ShouldThrowKeyNotFoundException_WhenAddressIsNull()
        {
            var addressId = Guid.NewGuid();

            var request = new AddressRequestDto
            {
                Address = "Test",
                FullName = "nhan",
                PhoneNumber = "0975605820",
                UserId = Guid.NewGuid(),
            };

            _addressRepoMock.Setup(a => a.GetByIdAsync(addressId)).ReturnsAsync((Address?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _sut.UpdateAsync(addressId, request));

            exception.Message.Should().Be("Không tìm thấy địa chỉ");

            _addressRepoMock.Verify(a => a.GetByIdAsync(It.IsAny<Guid>()), Times.Once);
            _addressRepoMock.Verify(a => a.Update(It.IsAny<Address>()), Times.Never);
            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Never);
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAddress_ShouldUpdateAddress_WhenRequestIsValid()
        {
            var addressId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var address = new Address
            {
                Id = addressId,
                AddressName = "Test",
                FullName = "Test",
                PhoneNumber = "0975605820",
                UserId = userId,
            };

            var request = new AddressRequestDto
            {
                Address = "Test1",
                FullName = "Test1",
                PhoneNumber = "0975605820",
                UserId = userId,
            };

            _addressRepoMock.Setup(a => a.GetByIdAsync(addressId)).ReturnsAsync(address);
            _uow.Setup(uow => uow.SaveChangeAsync());

            await _sut.UpdateAsync(addressId, request);

            address.AddressName.Should().Be(request.Address);
            address.FullName.Should().Be(request.FullName);
            address.PhoneNumber.Should().Be(request.PhoneNumber);
            address.UserId.Should().Be(request.UserId);

            _addressRepoMock.Verify(a => a.GetByIdAsync(addressId), Times.Once);
            _addressRepoMock.Verify(a => a.Update(address), Times.Once);
            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Once);
            _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.UserAddresses(userId)), Times.Once);
        }

        [Fact]
        public async Task RemoveAddress_ShouldThrowKeyNotFoundException_WhenAddressIsNull()
        {
            var addressId = Guid.NewGuid();

            _addressRepoMock.Setup(a => a.GetByIdAsync(addressId)).ReturnsAsync((Address?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _sut.DeleteAsync(addressId));

            exception.Message.Should().Be("Không tìm thấy địa chỉ");

            _addressRepoMock.Verify(a => a.GetByIdAsync(It.IsAny<Guid>()), Times.Once);
            _addressRepoMock.Verify(a => a.Update(It.IsAny<Address>()), Times.Never);
            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Never);
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RemoveAddress_ShouldRemoveAddress_WhenAddressExist()
        {
            var addressId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var address = new Address
            {
                Id = addressId,
                AddressName = "Test",
                FullName = "Test",
                PhoneNumber = "0975605820",
                UserId = userId,
            };

            _addressRepoMock.Setup(a => a.GetByIdAsync(addressId)).ReturnsAsync(address);
            _uow.Setup(uow => uow.SaveChangeAsync());

            await _sut.DeleteAsync(addressId);

            _addressRepoMock.Verify(a => a.GetByIdAsync(addressId), Times.Once);
            _addressRepoMock.Verify(a => a.Remove(address), Times.Once);
            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Once);
            _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.UserAddresses(userId)), Times.Once);
        }

        [Fact]
        public async Task RemoveAddress_ShouldThrowException_WhenSaveAddressFail()
        {
            var addressId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var address = new Address
            {
                Id = addressId,
                AddressName = "Test",
                FullName = "Test",
                PhoneNumber = "0975605820",
                UserId = userId,
            };

            _addressRepoMock.Setup(a => a.GetByIdAsync(addressId))
                   .ReturnsAsync(address);

            _uow.Setup(x => x.SaveChangeAsync())
                 .ThrowsAsync(new Exception("Database error"));

            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _sut.DeleteAsync(addressId));

            exception.InnerException?.Message.Should().Be("Database error");

            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        private (AddressService sut, Mock<IUnitOfWork> uow, Mock<IAddressRepo> addressRepo, Mock<ICachingProvider> cache) CreateSut(MockBehavior behavior)
        {
            var addressRepo = new Mock<IAddressRepo>();
            var uow = new Mock<IUnitOfWork>();
            var cache = new Mock<ICachingProvider>();

            uow.SetupGet(uow => uow.Address).Returns(addressRepo.Object);

            var sut = new AddressService(uow.Object, cache.Object);

            return (sut, uow, addressRepo, cache);
        }
    }
}