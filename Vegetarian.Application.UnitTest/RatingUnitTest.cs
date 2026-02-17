using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.Notifications;
using Vegetarian.Application.Abstractions.Persistence;
using Vegetarian.Application.Abstractions.Storage;
using Vegetarian.Application.Dtos.QueryParams;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Implements.Services;
using Vegetarian.Application.Validator;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.UnitTest
{
    public class RatingUnitTest
    {
        private readonly Mock<IUnitOfWork> _uow;
        private readonly Mock<ICloudinaryStorage> _cloudinaryMock;
        private readonly Mock<IRatingRepo> _ratingRepoMock;
        private readonly RatingService _sut;
        private readonly Mock<IFormFile> _formMock;
        private readonly Mock<IOrderMenuRepo> _orderMenuRepoMock;
        private readonly Mock<INotificationSender> _notificationRepoMock;

        public RatingUnitTest()
        {
            _uow = new Mock<IUnitOfWork>();
            _cloudinaryMock = new Mock<ICloudinaryStorage>();
            _ratingRepoMock = new Mock<IRatingRepo>();

            _uow.SetupGet(uow => uow.Rating).Returns(_ratingRepoMock.Object);
            _formMock = new Mock<IFormFile>();
            _orderMenuRepoMock = new Mock<IOrderMenuRepo>();
            _notificationRepoMock = new Mock<INotificationSender>();
            _sut = new RatingService(_uow.Object, _cloudinaryMock.Object, _notificationRepoMock.Object);
        }


        [Fact]
        public async Task GetAllRatingsByMenu_ShouldReturnsRatingInPagination_WhenRequestIsValid()
        {
            var menuId = Guid.NewGuid();
            var ratings = new List<Rating>
            {
                new Rating
                {
                    Id = Guid.NewGuid(),
                    MenuId = menuId,
                    Menu = new Menu { Id = menuId},
                    User = new User { Id = Guid.NewGuid()}
                },
                new Rating
                {
                    Id = Guid.NewGuid(),
                    MenuId = menuId,
                    Menu = new Menu { Id = menuId},
                    User = new User { Id = Guid.NewGuid()}
                },
                new Rating
                {
                    Id = Guid.NewGuid(),
                    MenuId = menuId,
                    Menu = new Menu { Id = menuId},
                    User = new User { Id = Guid.NewGuid()}
                }
            };

            var ratingParams = new RatingParams
            {
                Page = 1,
                PageSize = 2
            };

            var queryable = ratings.BuildMockDbSet();
            _ratingRepoMock.Setup(r => r.GetAll()).Returns(queryable.Object);

            var result = await _sut.GetAllRatingsByMenuAsync(menuId, ratingParams);

            result.Total.Should().Be(3);
            result.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllRatingByMenu_ShouldReturnRatings_WhenPageOrPageSizeIsZero()
        {
            var menuId = Guid.NewGuid();
            var ratings = new List<Rating>
            {
                new Rating
                {
                    Id = Guid.NewGuid(),
                    MenuId = menuId,
                    Menu = new Menu { Id = menuId},
                    User = new User { Id = Guid.NewGuid()}
                },
                new Rating
                {
                    Id = Guid.NewGuid(),
                    MenuId = menuId,
                    Menu = new Menu { Id = menuId},
                    User = new User { Id = Guid.NewGuid()}
                },
                new Rating
                {
                    Id = Guid.NewGuid(),
                    MenuId = menuId,
                    Menu = new Menu { Id = menuId},
                    User = new User { Id = Guid.NewGuid()}
                }
            };

            var ratingParams = new RatingParams
            {
                Page = 0,
                PageSize = 0
            };

            var queryable = ratings.BuildMockDbSet();
            _ratingRepoMock.Setup(r => r.GetAll()).Returns(queryable.Object);

            var result = await _sut.GetAllRatingsByMenuAsync(menuId, ratingParams);

            result.Total.Should().Be(3);
            result.Data.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetAllRatingByMenu_ShouldReturnRatings_WhenFilterApplied()
        {
            var menuId = Guid.NewGuid();
            var ratings = new List<Rating>
            {
                new Rating
                {
                    Id = Guid.NewGuid(),
                    MenuId = menuId,
                    Menu = new Menu { Id = menuId},
                    User = new User { Id = Guid.NewGuid()},
                    Stars = 4
                },
                new Rating
                {
                    Id = Guid.NewGuid(),
                    MenuId = menuId,
                    Menu = new Menu { Id = menuId},
                    User = new User { Id = Guid.NewGuid()},
                    Stars = 4
                },
                new Rating
                {
                    Id = Guid.NewGuid(),
                    MenuId = menuId,
                    Menu = new Menu { Id = menuId},
                    User = new User { Id = Guid.NewGuid()},
                    Stars = 2
                }
            };

            var ratingParams = new RatingParams
            {
                Page = 1,
                PageSize = 2,
                Stars = 2
            };

            var queryable = ratings.BuildMockDbSet();
            _ratingRepoMock.Setup(r => r.GetAll()).Returns(queryable.Object);

            var result = await _sut.GetAllRatingsByMenuAsync(menuId, ratingParams);

            result.Total.Should().Be(1);
            result.Data.Should().HaveCount(1);
        }

        [Fact]
        public async Task RatingOrder_ShouldThrowValidationDictionaryException_WhenRequestIsInvalid()
        {
            var request = new RatingRequestDto
            {
                MenuId = Guid.Empty,
                Stars = 6,
                OrderId = Guid.Empty
            };

            var exception = await Assert.ThrowsAsync<ValidationDictionaryException>(
                async () => await _sut.RatingPaidOrderAsync(request));

            exception.Should().NotBeNull();

        }
    }
}
