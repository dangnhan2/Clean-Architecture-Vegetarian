using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.Persistence;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Implements.Services;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.UnitTest
{
    public class NotificationUnitTest
    {
        private readonly Mock<IUnitOfWork> _uow;
        private readonly Mock<INotificationRepo> _notificationRepoMock;
        private readonly NotificationService _sut;

        public NotificationUnitTest()
        {   
            _uow = new Mock<IUnitOfWork>();
            _notificationRepoMock = new Mock<INotificationRepo>();
            _uow.SetupGet(x => x.Notification).Returns(_notificationRepoMock.Object);
            _sut = new NotificationService(_uow.Object);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowKeyNotFoundException_WhenNotificationNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _notificationRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Notification?)null);

            // Act
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.DeleteAsync(id));

            // Assert
            Assert.Equal("Thông báo không tồn tại", ex.Message);
            _notificationRepoMock.Verify(r => r.Remove(It.IsAny<Notification>()), Times.Never);
            _uow.Verify(u => u.SaveChangeAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveAndSave_WhenNotificationExist()
        {
            var userId = Guid.NewGuid();
            var id = Guid.NewGuid();
            var entity = new Notification { Id = id, UserId = userId};
            _notificationRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(entity);

            // Act
            await _sut.DeleteAsync(id);

            // Assert
            _notificationRepoMock.Verify(r => r.Remove(entity), Times.Once);
            _uow.Verify(u => u.SaveChangeAsync(), Times.Once);
        }

        [Fact]
        public async Task GetNotificationsAsync_ShouldReturnOrderedDescByCreatedAt_WhenHasNotifications()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var data = new List<Notification>
            {
              new Notification { Id = Guid.NewGuid(), UserId = userId, Tiltle = "T1", Message = "M1", Type = "info", Data="{}", IsRead=false, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
              new Notification { Id = Guid.NewGuid(), UserId = userId, Tiltle = "T2", Message = "M2", Type = "info", Data="{}", IsRead=true,  CreatedAt = DateTime.UtcNow.AddMinutes(-1)  },
              new Notification { Id = Guid.NewGuid(), UserId = userId, Tiltle = "Other", Message = "X", Type = "info", Data="{}", IsRead=false, CreatedAt = DateTime.UtcNow }
            };

            var queryable = data.BuildMockDbSet();
            _notificationRepoMock.Setup(r => r.GetAll()).Returns(queryable.Object);

            // Act
            var result = (await _sut.GetNotificationsAsync(userId)).ToList();

            // Assert
            Assert.Equal(3, result.Count);
            // Desc by CreatedAt => item gần nhất trước
            Assert.Equal("Other", result[0].Title);
            Assert.Equal("T2", result[1].Title);
            Assert.Equal("T1", result[2].Title);
        }

        [Fact]
        public async Task GetUnReadNotificationsAsync_ShouldReturnOnlyUnreadAndTotal_WhenHasUnread()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var data = new List<Notification>
        {
            new Notification { Id = Guid.NewGuid(), UserId = userId, Tiltle="U1", Message="M", Type="info", Data="{}", IsRead=false, CreatedAt=DateTime.UtcNow.AddMinutes(-2) },
            new Notification { Id = Guid.NewGuid(), UserId = userId, Tiltle="U2", Message="M", Type="info", Data="{}", IsRead=false, CreatedAt=DateTime.UtcNow.AddMinutes(-1) },
            new Notification { Id = Guid.NewGuid(), UserId = userId, Tiltle="R1", Message="M", Type="info", Data="{}", IsRead=true,  CreatedAt=DateTime.UtcNow },
            new Notification { Id = Guid.NewGuid(), UserId = userId, Tiltle="Other", Message="M", Type="info", Data="{}", IsRead=false, CreatedAt=DateTime.UtcNow }
        };

            var queryable = data.BuildMockDbSet();
            _notificationRepoMock.Setup(r => r.GetAll()).Returns(queryable.Object);

            // Act
            var result = await _sut.GetUnReadNotificationsAsync(userId);

            // Assert
            Assert.Equal(3, result.Total);

            Assert.All(result.UnreadNotifications, n => Assert.False(n.IsRead));
        }

        [Fact]
        public async Task GetUnReadNotificationsAsync_ShouldLimitTo99_WhenMoreThan99Unread()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var data = Enumerable.Range(1, 120)
                .Select(i => new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Tiltle = $"U{i}",
                    Message = "M",
                    Type = "info",
                    Data = "{}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddSeconds(-i)
                })
                .ToList();

            var queryable = data.BuildMockDbSet();
            _notificationRepoMock.Setup(r => r.GetAll()).Returns(queryable.Object);

            // Act
            var result = await _sut.GetUnReadNotificationsAsync(userId);

            result.UnreadNotifications.Should().NotBeNull();
            result.Total.Should().Be(99);
        }

        [Fact]
        public async Task MarkAsReadAsync_ShouldMarkExistingNotificationsAsRead_WhenIdsExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var n1 = new Notification { Id = Guid.NewGuid(), UserId = userId, IsRead = false };
            var n2 = new Notification { Id = Guid.NewGuid(), UserId = userId, IsRead = false };
            var missingId = Guid.NewGuid();

            _notificationRepoMock.Setup(r => r.GetByIdAsync(n1.Id)).ReturnsAsync(n1);
            _notificationRepoMock.Setup(r => r.GetByIdAsync(n2.Id)).ReturnsAsync(n2);
            _notificationRepoMock.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((Notification?)null);

            var req = new MarkNotificationRequestDto
            {
                NotificationIds = new List<Guid> { n1.Id, n2.Id, missingId }
            };

            // Act
            await _sut.MarkAsReadAsync(req);

            // Assert
            Assert.True(n1.IsRead);
            Assert.True(n2.IsRead);
            _uow.Verify(u => u.SaveChangeAsync(), Times.Once);
        }
    }

}
