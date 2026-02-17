using DotNetEnv;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.Caching;
using Vegetarian.Application.Abstractions.Persistence;
using Vegetarian.Application.Abstractions.Storage;
using Vegetarian.Application.Contants;
using Vegetarian.Application.Dtos.QueryParams;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Implements.Services;
using Vegetarian.Application.Validator;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.UnitTest
{
    public class UserUnitTest
    {
        private readonly Mock<IUserRepo> _userRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICachingProvider> _cacheMock;
        private readonly Mock<ICloudinaryStorage> _cloudinaryMock;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<IFormFile> _mockFile;
        private readonly UserService _sut;

        public UserUnitTest()
        {
            _userRepoMock = new Mock<IUserRepo>();
            _uowMock = new Mock<IUnitOfWork>();
            _cacheMock = new Mock<ICachingProvider>();
            _cloudinaryMock = new Mock<ICloudinaryStorage>();

            // UserManager khó mock → cần helper
            _userManagerMock = MockUserManager();

            _uowMock.SetupGet(u => u.User).Returns(_userRepoMock.Object);

            _mockFile = new Mock<IFormFile>();
            _sut = new UserService(
                _uowMock.Object,
                _cloudinaryMock.Object,
                _cacheMock.Object,
                _userManagerMock.Object
            );
        }


        [Fact]
        public async Task BanUser_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            var userId = Guid.NewGuid();

            _userRepoMock.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _sut.BanUserAsync(userId));

            exception.Message.Should().Be("Người dùng không tồn tại");
        }

        [Fact]
        public async Task BanUser_ShouldBanUser_WhenRequestIsValid()
        {
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
            };

            var refreshTokens = new List<RefreshToken>
            {
                new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    User = user,
                    UserId = userId,
                    Token = Guid.NewGuid() + "" + Guid.NewGuid(),
                    IsRevoked = false
                }
            };

            var querayable = refreshTokens.BuildMockDbSet();

            var refreshTokenRepoMock = new Mock<IRefreshTokenRepo>();

            _userRepoMock.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(user);
            refreshTokenRepoMock.Setup(rt => rt.GetAll()).Returns(querayable.Object);
            _uowMock.Setup(u => u.RefreshToken).Returns(refreshTokenRepoMock.Object);
            _uowMock.Setup(uow => uow.SaveChangeAsync());
            _userManagerMock.Setup(u => u.SetLockoutEnabledAsync(user, true));
            _userManagerMock.Setup(u => u.SetLockoutEndDateAsync(user, It.IsAny<DateTimeOffset>()));

            await _sut.BanUserAsync(userId);

            _userManagerMock.Verify(u => u.SetLockoutEnabledAsync(user, true), Times.Once);
            _userManagerMock.Verify(u => u.SetLockoutEndDateAsync(user, It.IsAny<DateTimeOffset>()), Times.Once);
        }

        [Fact]
        public async Task UnBanUser_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            var userId = Guid.NewGuid();

            _userRepoMock.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _sut.UnBanUserAsync(userId));

            exception.Message.Should().Be("Người dùng không tồn tại");
        }

        [Fact]
        public async Task UnBanUser_ShouldUnBanUser_WhenRequestIsValid()
        {
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
            };

            _userRepoMock.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(user);
            _uowMock.Setup(uow => uow.SaveChangeAsync());
            _userManagerMock.Setup(u => u.SetLockoutEndDateAsync(user, null));

            await _sut.UnBanUserAsync(userId);

            _userManagerMock.Verify(u => u.SetLockoutEndDateAsync(user, null), Times.Once);
        }

        [Fact]
        public async Task GetUserById_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            var userId = Guid.NewGuid();

            _userRepoMock.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _sut.GetUserByIdAsync(userId));
            exception.Message.Should().Be("Người dùng không tồn tại");
        }

        [Fact]
        public async Task GetUserById_ShouldReturnCached_WhenCacheHit()
        {
            var userId = Guid.NewGuid();
            var cached = new UserDto
            {
                Id = Guid.NewGuid(),
            };

            var (sut, uow, userRepoMock, cache, userManagerMock, cloudinaryMock) = CreateSut(MockBehavior.Strict);

            cache.Setup(c => c.GetAsync<UserDto>(CacheKeys.UserDetail(userId))).ReturnsAsync(cached);
            var result = await sut.GetUserByIdAsync(userId);

            result.Should().BeSameAs(cached);
        }

        [Fact]
        public async Task GetUserById_ShouldReturnUser_WhenCacheMiss()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId
            };

            _userRepoMock.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(user);
            _cacheMock.Setup(c => c.GetAsync<UserDto>(CacheKeys.UserDetail(userId))).ReturnsAsync((UserDto?)null);

            var result = await _sut.GetUserByIdAsync(userId);

            result.Should().NotBeNull();

            _cacheMock.Verify(c => c.SetAsync(
                CacheKeys.UserDetail(userId),
                It.IsAny<UserDto>(),
                TimeSpan.FromMinutes(10)), Times.Once);
        }

        [Fact]
        public async Task UploadProfile_ShouldThrowValidationDictionaryException_WhenRequestIsInvalid()
        {
            var userId = Guid.NewGuid();
            var request = new UserRequestDto
            {
                PhoneNumber = ""
            };

            var exception = await Assert.ThrowsAsync<ValidationDictionaryException>(
                async () => await _sut.UploadProfileAsync(userId, request));

            exception.Should().NotBeNull();
        }

        [Fact]
        public async Task UploadProfile_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            var userId = Guid.NewGuid();
            var request = new UserRequestDto
            {
                Avatar = null,
                PhoneNumber = "0975605890"
            };

            _userRepoMock.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _sut.UploadProfileAsync(userId, request));

            exception.Message.Should().Be("Người dùng không tồn tại");
        }

        [Fact]
        public async Task UploadProfile_ShouldThrowArgumentException_WhenPhoneNumberAlreadyExist()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, PhoneNumber = "0975605844" };
            var users = new List<User>
            {
                user,
                new User
                {
                    Id = Guid.NewGuid(),
                    PhoneNumber = "0975605890"
                }

            };

            var request = new UserRequestDto
            {
                PhoneNumber = "0975605890"
            };

            var queryable = users.BuildMockDbSet();
            _userRepoMock.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(user);
            _userRepoMock.Setup(u => u.GetAll()).Returns(queryable.Object);

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _sut.UploadProfileAsync(userId, request));

            exception.Message.Should().Be("Số điện thoại đã đăng kí");
        }

        [Fact]
        public async Task UploadProfile_ShouldPhoneNumber_WhenRequestHasPhoneNumber()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, PhoneNumber = "0975605844" };
            var users = new List<User>
            {
                user,
                new User
                {
                    Id = Guid.NewGuid(),
                    PhoneNumber = "0975605890"
                }

            };

            var request = new UserRequestDto
            {
                PhoneNumber = "0975605891"
            };

            var queryable = users.BuildMockDbSet();
            _userRepoMock.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(user);
            _userRepoMock.Setup(u => u.GetAll()).Returns(queryable.Object);
            _uowMock.Setup(u => u.SaveChangeAsync());

            await _sut.UploadProfileAsync(userId, request);

            user.PhoneNumber.Should().Be(request.PhoneNumber);

            _uowMock.Verify(u => u.SaveChangeAsync(), Times.Once);
            _cacheMock.Verify(u => u.RemoveAsync(CacheKeys.UserDetail(userId)));
        }

        [Fact]
        public async Task UploadProfile_ShouldUploadAndDeleteOldAvatar_WhenHasAvatar_AndOldNotDefault()
        {
            Env.Load();
            var userId = Guid.NewGuid();
            var request = new UserRequestDto
            {
                PhoneNumber = "0123456789",
                Avatar = _mockFile.Object
            };

            var oldUrl = "old_url";
            var newUrl = "new_url";

            var defaultAvatar = $"{Env.GetString("DEFAULT_AVATAR")}";

            var user = new User { Id = userId, ImageUrl = oldUrl, PhoneNumber = "000" };

            var users = new List<User> { user }.BuildMockDbSet();

            _userRepoMock.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(user);
            _userRepoMock.Setup(u => u.GetAll()).Returns(users.Object);
            _cloudinaryMock.Setup(u => u.UploadImage(request.Avatar, It.IsAny<string>())).ReturnsAsync(newUrl);
            _cloudinaryMock.Setup(u => u.DeleteImage(oldUrl));
            _uowMock.Setup(uow => uow.SaveChangeAsync());

            await _sut.UploadProfileAsync(userId, request);

            _uowMock.Verify(uow => uow.SaveChangeAsync(), Times.Once);
            _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.UserDetail(userId)), Times.Once);

        }

        [Fact]
        public async Task UploadProfile_ShouldUploadButNotDelete_WhenOldIsDefaultAvatar()
        {
            Env.Load();
            var userId = Guid.NewGuid();
            var request = new UserRequestDto
            {
                PhoneNumber = "0123456789",
                Avatar = _mockFile.Object
            };

            var newUrl = "new_url";
            var defaultAvatar = $"{Env.GetString("DEFAULT_AVATAR")}";

            var user = new User { Id = userId, ImageUrl = defaultAvatar, PhoneNumber = "000" };

            var users = new List<User> { user }.BuildMockDbSet();

            _userRepoMock.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(user);
            _userRepoMock.Setup(u => u.GetAll()).Returns(users.Object);
            _cloudinaryMock.Setup(u => u.UploadImage(request.Avatar, It.IsAny<string>())).ReturnsAsync(newUrl);
            _uowMock.Setup(uow => uow.SaveChangeAsync());

            await _sut.UploadProfileAsync(userId, request);

            _uowMock.Verify(uow => uow.SaveChangeAsync(), Times.Once);
            _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.UserDetail(userId)), Times.Once);
        }

        [Fact]
        public async Task GetAllUsers_ShouldReturnUserInPagination_WhenRequestIsValid()
        {
            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), IsAdmin = true },
                new User { Id = Guid.NewGuid(), IsAdmin = false },
                new User { Id = Guid.NewGuid(), IsAdmin = false},
            };

            var userParams = new UserParams
            {
                Page = 1,
                PageSize = 2,
            };

            var queryable = users.BuildMockDbSet();
            _userRepoMock.Setup(u => u.GetAll()).Returns(queryable.Object);

            var result = await _sut.GetAllAsync(userParams);

            result.Total.Should().Be(2);
            result.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllUsers_ShouldReturnUser_WhenPageOrPageSizeIsZero()
        {
            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), IsAdmin = false },
                new User { Id = Guid.NewGuid(), IsAdmin = false },
                new User { Id = Guid.NewGuid(), IsAdmin = false},
            };

            var userParams = new UserParams
            {
                Page = 0,
                PageSize = 2,
            };

            var queryable = users.BuildMockDbSet();
            _userRepoMock.Setup(u => u.GetAll()).Returns(queryable.Object);

            var result = await _sut.GetAllAsync(userParams);

            result.Total.Should().Be(3);
            result.Data.Should().HaveCount(3);
        }


        private static Mock<UserManager<User>> MockUserManager()
        {
            var store = new Mock<IUserStore<User>>();

            return new Mock<UserManager<User>>(
                store.Object,
                null!, null!, null!, null!, null!, null!, null!, null!
            );
        }


        private (
            UserService sut,
            Mock<IUnitOfWork> uow,
            Mock<IUserRepo> userRepo,
            Mock<ICachingProvider> cache,
            Mock<UserManager<User>> userManager,
            Mock<ICloudinaryStorage> cloudinary) CreateSut(MockBehavior behavior)
        {
            var categoryRepo = new Mock<ICategoryRepo>();
            var uow = new Mock<IUnitOfWork>();
            var cache = new Mock<ICachingProvider>();
            var userManagerMock = MockUserManager();
            var cloudinaryMock = new Mock<ICloudinaryStorage>();
            var userRepoMock = new Mock<IUserRepo>();

            uow.SetupGet(uow => uow.Category).Returns(categoryRepo.Object);

            var sut = new UserService(uow.Object, cloudinaryMock.Object, cache.Object, userManagerMock.Object);

            return (sut, uow, userRepoMock, cache, userManagerMock, cloudinaryMock);
        }
    }
}
