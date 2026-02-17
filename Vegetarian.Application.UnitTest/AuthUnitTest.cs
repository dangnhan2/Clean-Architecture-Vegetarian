using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.BackgroundJobs;
using Vegetarian.Application.Abstractions.Persistence;
using Vegetarian.Application.Abstractions.Token;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Implements.Services;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.UnitTest
{
    public class AuthUnitTest
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<ITokenGenerator> _tokenServiceMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly AuthService _authService;
        private readonly Mock<IHangfireJobClient> _mockHangfireClient;
        public AuthUnitTest()
        {
            var userStoreMock = new Mock<IUserStore<User>>();
            _userManagerMock = MockUserManager();

            _tokenServiceMock = new Mock<ITokenGenerator>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mockHangfireClient = new Mock<IHangfireJobClient>();

            _authService = new AuthService(
                _userManagerMock.Object,
                _tokenServiceMock.Object,
                _unitOfWorkMock.Object,
                _mockHangfireClient.Object
            );
        }

        [Fact]
        public async Task ChangePasswordAsync_WithValidRequest_ShouldChangePassword()
        {
            // Arrange
            var request = new PasswordRequestDto
            {
                Id = Guid.NewGuid(),
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };

            var user = new User { Id = request.Id };

            _userManagerMock.Setup(x => x.FindByIdAsync(request.Id.ToString()))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.CurrentPassword))
                .ReturnsAsync(true);
            _userManagerMock.Setup(x => x.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _authService.ChangePasswordAsync(request);

            // Assert
            _userManagerMock.Verify(x => x.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_WithNonExistentUser_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var request = new PasswordRequestDto
            {
                Id = Guid.NewGuid(),
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(request.Id.ToString()))
                .ReturnsAsync((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _authService.ChangePasswordAsync(request));
        }

        [Fact]
        public async Task ChangePasswordAsync_WithIncorrectCurrentPassword_ShouldThrowInvalidDataException()
        {
            // Arrange
            var request = new PasswordRequestDto
            {
                Id = Guid.NewGuid(),
                CurrentPassword = "WrongPassword123!",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };

            var user = new User { Id = request.Id };

            _userManagerMock.Setup(x => x.FindByIdAsync(request.Id.ToString()))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.CurrentPassword))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidDataException>(() => _authService.ChangePasswordAsync(request));
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnAuthResponse()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                EmailConfirmed = true
            };

            var authResponse = new AuthResponse
            {
                AccessToken = "access_token",
            };

            var httpContext = new DefaultHttpContext();

            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password))
                .ReturnsAsync(true);
            _userManagerMock.Setup(x => x.IsLockedOutAsync(user))
                .ReturnsAsync(false);
            _tokenServiceMock.Setup(x => x.GenerateToken(user, httpContext))
                .ReturnsAsync(authResponse);

            // Act
            var result = await _authService.LoginAsync(request, httpContext);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(authResponse);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidCredentials_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync((User)null);

            var httpContext = new DefaultHttpContext();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _authService.LoginAsync(request, httpContext));
        }

        [Fact]
        public async Task LoginAsync_WithLockedAccount_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email
            };

            var httpContext = new DefaultHttpContext();

            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password))
                .ReturnsAsync(true);
            _userManagerMock.Setup(x => x.IsLockedOutAsync(user))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(request, httpContext));
        }

        [Fact]
        public async Task LoginAsync_WithUnconfirmedEmail_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                EmailConfirmed = false
            };

            var httpContext = new DefaultHttpContext();

            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password))
                .ReturnsAsync(true);
            _userManagerMock.Setup(x => x.IsLockedOutAsync(user))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _authService.LoginAsync(request, httpContext));
        }

        [Fact]
        public async Task RegisterAsync_WithValidData_ShouldReturnEmail()
        {
            // Arrange
            var request = new RegisterRequestDto
            {
                UserName = "testuser",
                Email = "test@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync((User)null);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), request.Password))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "Customer"))
                .ReturnsAsync(IdentityResult.Success);

            var emailOtpRepoMock = new Mock<IEmailOtpRepo>();
            emailOtpRepoMock.Setup(e => e.AddAsync(It.IsAny<EmailOtp>())).Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(uow => uow.EmailOtp).Returns(emailOtpRepoMock.Object);

            // Act
            var result = await _authService.RegisterAsync(request);

            // Assert
            result.Should().Be(request.Email);
            _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<User>(), request.Password), Times.Once);
            _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), "Customer"), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ShouldThrowInvalidDataException()
        {
            // Arrange
            var request = new RegisterRequestDto
            {
                UserName = "testuser",
                Email = "existing@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var existingUser = new User { Email = request.Email };

            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync(existingUser);

            var emailOtpRepoMock = new Mock<IEmailOtpRepo>();
            emailOtpRepoMock.Setup(e => e.AddAsync(It.IsAny<EmailOtp>())).Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(uow => uow.EmailOtp).Returns(emailOtpRepoMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidDataException>(() => _authService.RegisterAsync(request));
        }

        [Fact]
        public async Task ForgotPasswordAsync_WithNonExistentEmail_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var request = new ForgotPasswordRequestDto
            {
                Email = "nonexistent@example.com"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _authService.ForgotPasswordAsync(request));
        }

        [Fact]
        public async Task ResetPasswordAsync_WithValidData_ShouldResetPassword()
        {
            var userId = Guid.NewGuid();
            // Arrange
            var request = new ResetPasswordRequestDto
            {
                Email = "test@example.com",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };

            var user = new User
            {
                Id = userId,
                Email = request.Email,
                EmailOtps = new List<EmailOtp>(),
                Cart = new Cart
                {
                    UserId = userId,
                },
                ImageUrl = "default.png",
                RefreshTokens = new List<RefreshToken>(),
                VoucherRedemptions = new List<VoucherRedemption>(),
                Orders = new List<Order>(),
                Addresses = new List<Address>(),
                Notifications = new List<Notification>()
            };

            var resetToken = "reset_token";

            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync(resetToken);
            _userManagerMock.Setup(x => x.ResetPasswordAsync(user, resetToken, request.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _authService.ResetPasswordAsync(request);

            // Assert
            _userManagerMock.Verify(x => x.ResetPasswordAsync(user, resetToken, request.NewPassword), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_WithNonExistentUser_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var request = new ResetPasswordRequestDto
            {
                Email = "nonexistent@example.com",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _authService.ResetPasswordAsync(request));
        }

        [Fact]
        public async Task LogoutAsync_WithValidRefreshToken_ShouldClearTokensAndDeleteCookie()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var refreshToken = "valid_refresh_token";
            httpContext.Request.Headers["Cookie"] = $"refreshToken={refreshToken}";

            var userId = Guid.NewGuid();
            var existToken = new RefreshToken
            {
                Token = refreshToken,
                UserId = userId,
                ExpriedAt = DateTime.UtcNow.AddDays(1)
            };

            var user = new User
            {
                Id = userId,
                RefreshTokens = new List<RefreshToken> { existToken }
            };

            var refreshTokenRepoMock = new Mock<IRefreshTokenRepo>();
            var userRepoMock = new Mock<IUserRepo>();

            _unitOfWorkMock.Setup(x => x.RefreshToken).Returns(refreshTokenRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.User).Returns(userRepoMock.Object);

            refreshTokenRepoMock.Setup(x => x.GetTokenByRefreshToken(refreshToken))
                .ReturnsAsync(existToken);
            userRepoMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            await _authService.LogoutAsync(httpContext);

            // Assert
            _unitOfWorkMock.Verify(x => x.SaveChangeAsync(), Times.Once);
            userRepoMock.Verify(x => x.Update(user), Times.Once);
            refreshTokenRepoMock.Verify(x => x.Remove(existToken), Times.Once);
        }


        [Fact]
        public async Task VerifyEmail_WithValidOtp_ShouldConfirmEmail()
        {
            // Arrange
            var request = new EmailVerifyRequestDto
            {
                Email = "test@example.com",
                Otp = "123456"
            };

            var emailOtp = new EmailOtp
            {
                Otp = request.Otp,
                IsUsed = false,
                ExpiredAt = DateTime.UtcNow.AddMinutes(10)
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                EmailConfirmed = false,
                EmailOtps = new List<EmailOtp> { emailOtp }
            };

            var userRepoMock = new Mock<IUserRepo>();
            var emailOtpRepoMock = new Mock<IEmailOtpRepo>();

            _unitOfWorkMock.Setup(x => x.User).Returns(userRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.EmailOtp).Returns(emailOtpRepoMock.Object);

            userRepoMock.Setup(x => x.GetUserByEmailAsync(request.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.VerifyEmail(request);

            // Assert
            result.Should().Be(request.Email);
            user.EmailConfirmed.Should().BeTrue();
            _unitOfWorkMock.Verify(x => x.SaveChangeAsync(), Times.Once);
        }

        [Fact]
        public async Task VerifyEmail_WithExpiredOtp_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new EmailVerifyRequestDto
            {
                Email = "test@example.com",
                Otp = "123456"
            };

            var emailOtp = new EmailOtp
            {
                Otp = request.Otp,
                IsUsed = false,
                ExpiredAt = DateTime.UtcNow.AddMinutes(-10)
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                EmailOtps = new List<EmailOtp> { emailOtp }
            };

            var userRepoMock = new Mock<IUserRepo>();
            _unitOfWorkMock.Setup(x => x.User).Returns(userRepoMock.Object);

            userRepoMock.Setup(x => x.GetUserByEmailAsync(request.Email))
                .ReturnsAsync(user);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _authService.VerifyEmail(request));
        }

        [Fact]
        public async Task ResendEmailAsync_WithValidEmail_ShouldMarkOldOtpsAndSendNew()
        {
            var userId = Guid.NewGuid();
            var request = new ResendEmailRequestDto
            {
                UserEmail = "test@example.com"
            };

            var user = new User
            {
                Id = userId,
                Email = request.UserEmail,
                EmailOtps = new List<EmailOtp>(),
                Cart = new Cart
                {
                    UserId = userId,
                },
                ImageUrl = "default.png",
                RefreshTokens = new List<RefreshToken>(),
                VoucherRedemptions = new List<VoucherRedemption>(),
                Orders = new List<Order>(),
                Addresses = new List<Address>(),
                Notifications = new List<Notification>()
            };

            var oldOtp = new EmailOtp
            {
                Id = Guid.NewGuid(),
                Otp = "123456",
                IsUsed = false,
                ExpiredAt = DateTime.UtcNow.AddMinutes(10),
                UserId = userId,
                User = user
            };

            user.EmailOtps.Add(oldOtp);

            var userRepoMock = new Mock<IUserRepo>();
            _unitOfWorkMock.Setup(x => x.User).Returns(userRepoMock.Object);

            var emailOtpRepoMock = new Mock<IEmailOtpRepo>();
            emailOtpRepoMock.Setup(e => e.AddAsync(It.IsAny<EmailOtp>())).Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(uow => uow.EmailOtp).Returns(emailOtpRepoMock.Object);

            userRepoMock.Setup(x => x.GetUserByEmailAsync(request.UserEmail))
                .ReturnsAsync(user);

            await _authService.ResendEmailAsync(request);

            oldOtp.IsUsed.Should().BeTrue();

            userRepoMock.Verify(x => x.GetUserByEmailAsync(request.UserEmail), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangeAsync(), Times.Once);
        }

        [Fact]
        public async Task ResendEmailAsync_WithNonExistentUser_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var request = new ResendEmailRequestDto
            {
                UserEmail = "nonexistent@example.com"
            };

            var userRepoMock = new Mock<IUserRepo>();
            _unitOfWorkMock.Setup(x => x.User).Returns(userRepoMock.Object);

            userRepoMock.Setup(x => x.GetUserByEmailAsync(request.UserEmail))
                .ReturnsAsync((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _authService.ResendEmailAsync(request));
        }

        private static Mock<UserManager<User>> MockUserManager()
        {
            var store = new Mock<IUserStore<User>>();

            return new Mock<UserManager<User>>(
               store.Object,
               null!, null!, null!, null!, null!, null!, null!, null!
            );
        }
    }
}
