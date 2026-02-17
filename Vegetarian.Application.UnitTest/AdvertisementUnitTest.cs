using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.Caching;
using Vegetarian.Application.Abstractions.Storage;
using Vegetarian.Application.Contants;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Implements.Services;
using Vegetarian.Application.Validator;
using Vegetarian.Domain.Enum;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.UnitTest
{
    public class AdvertisementUnitTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ICachingProvider> _cachingMock;
        private readonly Mock<ICloudinaryStorage> _cloudinaryMock;
        private readonly AdvertisementService _service;
        private readonly Mock<IFormFile> _formFileMock;

        public AdvertisementUnitTest()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _cachingMock = new Mock<ICachingProvider>();
            _cloudinaryMock = new Mock<ICloudinaryStorage>();
            _service = new AdvertisementService(_unitOfWorkMock.Object, _cachingMock.Object, _cloudinaryMock.Object);
            _formFileMock = new Mock<IFormFile>();
        }


        [Fact]
        public async Task GetAdvertisementsAsync_ShouldReturnCachedData_WhenCacheHíts()
        {
            // Arrange
            var cachedData = new List<AdvertisementDto>
            {
               new AdvertisementDto { Id = Guid.NewGuid(), Title = "Ad 1", IsActive = true }
            };

            _cachingMock.Setup(x => x.GetAsync<IEnumerable<AdvertisementDto>>(CacheKeys.ADVERTISEMENT_ACTIVE))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _service.GetAdvertisementsAsync();

            // Assert
            result.Should().BeEquivalentTo(cachedData);
            _unitOfWorkMock.Verify(x => x.Advertisement.GetAll(), Times.Never);
        }

        [Fact]
        public async Task GetAdvertisementsAsync_ShouldQueryDatabaseAndCache_WhenCacheMiss()
        {
            // Arrange
            var advertisements = new List<Advertisement>{
               new Advertisement
               {
                   Id = Guid.NewGuid(),
                   Title = "Ad 1",
                   BannerUrl = "url1",
                   AdTargetType = AdTargetType.OnSellerPage,
                   TargetKey = "key1",
                   StartAt = DateTimeOffset.Now,
                   EndAt = DateTimeOffset.Now.AddDays(7),
                   IsActive = true
               },

               new Advertisement
               {
                   Id = Guid.NewGuid(),
                   Title = "Ad 2",
                   IsActive = false
               }
           };

            var queryable = advertisements.BuildMockDbSet();

            _cachingMock.Setup(x => x.GetAsync<IEnumerable<AdvertisementDto>>(CacheKeys.ADVERTISEMENT_ACTIVE))
                .ReturnsAsync((IEnumerable<AdvertisementDto>)null);

            _unitOfWorkMock.Setup(x => x.Advertisement.GetAll())
                .Returns(queryable.Object);

            _cachingMock.Setup(x => x.SetAsync(CacheKeys.ADVERTISEMENT_ACTIVE, It.IsAny<IEnumerable<AdvertisementDto>>(), TimeSpan.FromMinutes(30)))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.GetAdvertisementsAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Ad 1");
            _cachingMock.Verify(x => x.SetAsync(CacheKeys.ADVERTISEMENT_ACTIVE, It.IsAny<IEnumerable<AdvertisementDto>>(), TimeSpan.FromMinutes(30)), Times.Once);
        }

        [Fact]
        public async Task GetAdvertisementsByAdminAsync_WhenCacheExists_ShouldReturnCachedData()
        {
            // Arrange
            var cachedData = new List<AdvertisementDto>
            {
               new AdvertisementDto { Id = Guid.NewGuid(), Title = "Ad 1", IsActive = true },
               new AdvertisementDto { Id = Guid.NewGuid(), Title = "Ad 2", IsActive = false }
            };

            _cachingMock.Setup(x => x.GetAsync<IEnumerable<AdvertisementDto>>(CacheKeys.ADVERTISEMENT_PREFIX))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _service.GetAdvertisementsByAdminAsync();

            // Assert
            result.Should().HaveCount(2);
            _unitOfWorkMock.Verify(x => x.Advertisement.GetAll(), Times.Never);
        }

        [Fact]
        public async Task GetAdvertisementsByAdminAsync_ShouldReturnAllAdvertisements_WhenCacheNotExists()
        {
            // Arrange
            var advertisements = new List<Advertisement>{
               new Advertisement
               {
                   Id = Guid.NewGuid(),
                   Title = "Ad 1",
                   BannerUrl = "url1",
                   AdTargetType = AdTargetType.OnSellerPage,
                   TargetKey = "key1",
                   StartAt = DateTimeOffset.Now,
                   EndAt = DateTimeOffset.Now.AddDays(7),
                   IsActive = true
               },

               new Advertisement
               {
                   Id = Guid.NewGuid(),
                   Title = "Ad 2",
                   IsActive = false
               }
           }.BuildMockDbSet();

            _cachingMock.Setup(x => x.GetAsync<IEnumerable<AdvertisementDto>>(CacheKeys.ADVERTISEMENT_PREFIX))
                .ReturnsAsync((IEnumerable<AdvertisementDto>)null);

            _unitOfWorkMock.Setup(x => x.Advertisement.GetAll())
                .Returns(advertisements.Object);

            // Act
            var result = await _service.GetAdvertisementsByAdminAsync();

            // Assert
            result.Should().HaveCount(2);
            _cachingMock.Verify(x => x.SetAsync(CacheKeys.ADVERTISEMENT_PREFIX, It.IsAny<IEnumerable<AdvertisementDto>>(), TimeSpan.FromMinutes(30)), Times.Once);
        }

        [Fact]
        public async Task AddAdvertisementAsync_ShouldAddAdvertisement_WhenValidRequest()
        {
            // Arrange
            var request = new AdvertisementRequestDto
            {
                Title = "New Ad",
                BannerUrl = _formFileMock.Object,
                AdTargetType = AdTargetType.OnSellerPage,
                TargetKey = "product-123",
                StartAt = DateTimeOffset.Now,
                EndAt = DateTimeOffset.Now.AddDays(7),
                IsActive = false
            };

            var advertisements = new List<Advertisement>().BuildMockDbSet();

            _unitOfWorkMock.Setup(x => x.Advertisement.GetAll())
                .Returns(advertisements.Object);

            _unitOfWorkMock.Setup(x => x.Advertisement.AddAsync(It.IsAny<Advertisement>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(x => x.SaveChangeAsync())
                .Returns(Task.CompletedTask);

            _cachingMock.Setup(x => x.RemoveAsync(CacheKeys.ADVERTISEMENT_PREFIX))
                .Returns(Task.CompletedTask);

            // Act
            await _service.AddAdvertisementAsync(request);

            // Assert
            _unitOfWorkMock.Verify(x => x.Advertisement.AddAsync(It.IsAny<Advertisement>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangeAsync(), Times.Once);
            _cachingMock.Verify(x => x.RemoveAsync(CacheKeys.ADVERTISEMENT_PREFIX), Times.Once);
        }

        [Fact]
        public async Task AddAdvertisementAsync_ShouldClearActiveCacheAsWell_WhenIsActive()
        {
            // Arrange
            var request = new AdvertisementRequestDto
            {
                Title = "New Active Ad",
                BannerUrl = _formFileMock.Object,
                AdTargetType = AdTargetType.OnSellerPage,
                TargetKey = "product-123",
                StartAt = DateTimeOffset.Now,
                IsActive = true
            };

            var advertisements = new List<Advertisement>().BuildMockDbSet();

            _unitOfWorkMock.Setup(x => x.Advertisement.GetAll())
                .Returns(advertisements.Object);

            _unitOfWorkMock.Setup(x => x.Advertisement.AddAsync(It.IsAny<Advertisement>()))
                .Returns(Task.CompletedTask);

            _cachingMock.Setup(x => x.RemoveAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.AddAdvertisementAsync(request);

            // Assert
            _cachingMock.Verify(x => x.RemoveAsync(CacheKeys.ADVERTISEMENT_PREFIX), Times.Once);
            _cachingMock.Verify(x => x.RemoveAsync(CacheKeys.ADVERTISEMENT_ACTIVE), Times.Once);
        }

        [Fact]
        public async Task AddAdvertisementAsync_ShouldThrowArgumentException_WhenTitleExists()
        {
            // Arrange
            var request = new AdvertisementRequestDto
            {
                Title = "Existing Ad",
                BannerUrl = _formFileMock.Object,
                AdTargetType = AdTargetType.OnSellerPage,
                TargetKey = "key",
                StartAt = DateTimeOffset.Now,
                IsActive = false
            };

            var advertisements = new List<Advertisement>
            {
                new Advertisement { Title = "ExistingAd" }
            }.BuildMockDbSet();

            _unitOfWorkMock.Setup(x => x.Advertisement.GetAll())
                .Returns(advertisements.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.AddAdvertisementAsync(request));
        }

        [Fact]
        public async Task AddAdvertisementAsync_ShouldThrowValidationException_WhenInvalidRequest()
        {
            // Arrange
            var request = new AdvertisementRequestDto
            {
                Title = "", // Invalid
                BannerUrl = _formFileMock.Object,
                StartAt = DateTimeOffset.Now,
                IsActive = false
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationDictionaryException>(() => _service.AddAdvertisementAsync(request));
        }

        [Fact]
        public async Task UpdateAdvertisementAsync_ShouldUpdateAdvertisement_WhenValidRequest()
        {
            // Arrange
            var adId = Guid.NewGuid();
            var existingAd = new Advertisement
            {
                Id = adId,
                Title = "Old Title",
                BannerUrl = "old-url.jpg",
                AdTargetType = AdTargetType.MenuPage,
                TargetKey = "old-key",
                StartAt = DateTimeOffset.Now,
                IsActive = false
            };

            var request = new AdvertisementRequestDto
            {
                Title = "Updated Title",
                BannerUrl = null,
                AdTargetType = AdTargetType.MenuPage,
                TargetKey = "new-key",
                StartAt = DateTimeOffset.Now,
                EndAt = DateTimeOffset.Now.AddDays(10),
                IsActive = false
            };

            var advertisements = new List<Advertisement>().BuildMockDbSet();

            _unitOfWorkMock.Setup(x => x.Advertisement.GetByIdAsync(adId))
                .ReturnsAsync(existingAd);

            _unitOfWorkMock.Setup(x => x.Advertisement.GetAll())
                .Returns(advertisements.Object);

            _unitOfWorkMock.Setup(x => x.Advertisement.Update(It.IsAny<Advertisement>()));

            _unitOfWorkMock.Setup(x => x.SaveChangeAsync())
                .Returns(Task.CompletedTask);

            _cachingMock.Setup(x => x.RemoveAsync(CacheKeys.ADVERTISEMENT_PREFIX))
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateAdvertisementAsync(adId, request);

            // Assert
            existingAd.Title.Should().Be("Updated Title");
            existingAd.TargetKey.Should().Be("new-key");
            _unitOfWorkMock.Verify(x => x.Advertisement.Update(existingAd), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangeAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAdvertisementAsync_ShouldDeleteOldAndUploadNew_WhenBannerUrlChanged()
        {
            // Arrange
            var adId = Guid.NewGuid();
            var existingAd = new Advertisement
            {
                Id = adId,
                Title = "Ad",
                BannerUrl = "old-banner.jpg",
                AdTargetType = AdTargetType.OnSellerPage,
                TargetKey = "key",
                StartAt = DateTimeOffset.Now,
                IsActive = false
            };

            var request = new AdvertisementRequestDto
            {
                Title = "Ad",
                BannerUrl = _formFileMock.Object,
                AdTargetType = AdTargetType.OnSellerPage,
                TargetKey = "key",
                StartAt = DateTimeOffset.Now,
                IsActive = false
            };

            var advertisements = new List<Advertisement>().BuildMockDbSet();

            _unitOfWorkMock.Setup(x => x.Advertisement.GetByIdAsync(adId))
                .ReturnsAsync(existingAd);

            _unitOfWorkMock.Setup(x => x.Advertisement.GetAll())
                .Returns(advertisements.Object);

            _cloudinaryMock.Setup(x => x.DeleteImage("old-banner.jpg"))
                .Returns(Task.CompletedTask);

            _cloudinaryMock.Setup(x => x.UploadImage(_formFileMock.Object, AdvertisementService.folder))
                .ReturnsAsync("new-uploaded-url.jpg");

            // Act
            await _service.UpdateAdvertisementAsync(adId, request);

            // Assert
            _cloudinaryMock.Verify(x => x.DeleteImage("old-banner.jpg"), Times.Once);
            _cloudinaryMock.Verify(x => x.UploadImage(_formFileMock.Object, AdvertisementService.folder), Times.Once);
            existingAd.BannerUrl.Should().Be("new-uploaded-url.jpg");
        }

        [Fact]
        public async Task UpdateAdvertisementAsync_ShouldClearActiveCache_WhenActiveStatusChanged()
        {
            // Arrange
            var adId = Guid.NewGuid();
            var existingAd = new Advertisement
            {
                Id = adId,
                Title = "Ad",
                BannerUrl = "url.jpg",
                AdTargetType = AdTargetType.OnSellerPage,
                TargetKey = "key",
                StartAt = DateTimeOffset.Now,
                IsActive = false
            };

            var request = new AdvertisementRequestDto
            {
                Title = "Ad",
                AdTargetType = AdTargetType.OnSellerPage,
                TargetKey = "key",
                StartAt = DateTimeOffset.Now,
                IsActive = true // Changed to active
            };

            var advertisements = new List<Advertisement>().BuildMockDbSet();

            _unitOfWorkMock.Setup(x => x.Advertisement.GetByIdAsync(adId))
                .ReturnsAsync(existingAd);

            _unitOfWorkMock.Setup(x => x.Advertisement.GetAll())
                .Returns(advertisements.Object);

            _cachingMock.Setup(x => x.RemoveAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateAdvertisementAsync(adId, request);

            // Assert
            _cachingMock.Verify(x => x.RemoveAsync(CacheKeys.ADVERTISEMENT_ACTIVE), Times.Once);
        }

        [Fact]
        public async Task UpdateAdvertisementAsync_ShouldThrowKeyNotFoundException_WhenAdvertisementNotFound()
        {
            // Arrange
            var adId = Guid.NewGuid();
            var request = new AdvertisementRequestDto
            {
                Title = "Ad",
                StartAt = DateTimeOffset.UtcNow,
                IsActive = false,
                AdTargetType = AdTargetType.OnSellerPage,
                BannerUrl = null,
                EndAt = null,
                TargetKey = "product",
            };

            _unitOfWorkMock.Setup(x => x.Advertisement.GetByIdAsync(adId))
                .ReturnsAsync((Advertisement?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _service.UpdateAdvertisementAsync(adId, request));
        }

        [Fact]
        public async Task UpdateAdvertisementAsync_ShouldThrowArgumentException_WhenTitleExistsForDifferentAd()
        {
            // Arrange
            var adId = Guid.NewGuid();
            var existingAd = new Advertisement
            {
                Id = adId,
                Title = "Original Title",
                StartAt = DateTimeOffset.Now,
                IsActive = false
            };

            var request = new AdvertisementRequestDto
            {
                Title = "DuplicateTitle",
                StartAt = DateTimeOffset.UtcNow,
                IsActive = false,
                AdTargetType = AdTargetType.OnSellerPage,
                BannerUrl = null,
                EndAt = null,
                TargetKey = "product",
            };

            var advertisements = new List<Advertisement>
            {
               new Advertisement { Id = Guid.NewGuid(), Title = "DuplicateTitle" }
            }.BuildMockDbSet();

            _unitOfWorkMock.Setup(x => x.Advertisement.GetByIdAsync(adId))
                .ReturnsAsync(existingAd);

            _unitOfWorkMock.Setup(x => x.Advertisement.GetAll())
                .Returns(advertisements.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await _service.UpdateAdvertisementAsync(adId, request));
        }
    }
}
