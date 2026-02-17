using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
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
    public class MenuUnitTest
    {
        private readonly Mock<IUnitOfWork> _uow;
        private readonly Mock<ICachingProvider> _cache;
        private readonly Mock<ICloudinaryStorage> _cloudinaryMock;
        private readonly Mock<IMenuRepo> _menuRepoMock;
        private readonly MenuService _sut;

        public MenuUnitTest()
        {
            _uow = new Mock<IUnitOfWork>();
            _cache = new Mock<ICachingProvider>();
            _cloudinaryMock = new Mock<ICloudinaryStorage>();
            _menuRepoMock = new Mock<IMenuRepo>();

            _uow.SetupGet(uow => uow.Menu).Returns(_menuRepoMock.Object);

            _sut = new MenuService(_uow.Object, _cache.Object, _cloudinaryMock.Object);
        }


        [Fact]
        public async Task AddMenu_ShouldThrowValidationDictionaryException_WhenRequestIsInvalid()
        {
            var request = new MenuRequestDto
            {
                Name = "",
                CategoryId = Guid.NewGuid(),
                Description = "",
                OriginalPrice = 1000,
                DiscountPrice = null,
                IsAvailable = false,
                IsOnSale = false,
            };

            var exception = await Assert.ThrowsAsync<ValidationDictionaryException>(
                async () => await _sut.AddMenuAsync(request));

            exception.Should().NotBeNull();

            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Never);

        }

        [Fact]
        public async Task AddMenu_ShouldThrowDuplicateNameException_WhenMenuAlreadyExist()
        {
            var menu = new List<Menu>
            {
                new Menu { Name = "test" },
                new Menu { Name = "test2"}
            };

            var request = new MenuRequestDto
            {
                Name = "test",
                CategoryId = Guid.NewGuid(),
                Description = "",
                OriginalPrice = 1000,
                DiscountPrice = 200,
                IsAvailable = false,
                IsOnSale = true,
            };

            var queryable = menu.BuildMockDbSet();
            _menuRepoMock.Setup(m => m.GetAll()).Returns(queryable.Object);

            var exception = await Assert.ThrowsAsync<DuplicateNameException>(
                async () => await _sut.AddMenuAsync(request));

            exception.Message.Should().Be($"Menu {request.Name} đã tồn tại");

            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Never);
        }

        // discount price equal 0 but is onSale equal true
        [Fact]
        public async Task AddMenu_ShouldThrowArgumentException_WhenDiscountPriceEqualZero()
        {
            var request = new MenuRequestDto
            {
                Name = "Test",
                CategoryId = Guid.NewGuid(),
                Description = "",
                OriginalPrice = 1000,
                DiscountPrice = null,
                IsAvailable = false,
                IsOnSale = true,
            };

            var menu = new List<Menu>
            {
                new Menu { Name = "test1" },
                new Menu { Name = "test2"}
            };

            var queryable = menu.BuildMockDbSet();
            _menuRepoMock.Setup(m => m.GetAll()).Returns(queryable.Object);

            var exception = await Assert.ThrowsAsync<ArgumentException>(
               async () => await _sut.AddMenuAsync(request));

            exception.Message.Should().Be("Món ăn đang có trạng thái giảm giá nhưng chưa cập nhật giá khuyến mãi. Hãy cập nhập giá khuyến mãi");

            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Never);
        }

        [Fact]
        public async Task AddMenu_ShouldAddMenu_WhenRequestIsValid()
        {
            var request = new MenuRequestDto
            {
                Name = "Test",
                CategoryId = Guid.NewGuid(),
                Description = "",
                OriginalPrice = 1000,
                DiscountPrice = null,
                IsAvailable = false,
                IsOnSale = false,
            };

            var menu = new List<Menu>
            {
                new Menu { Name = "test1" },
                new Menu { Name = "test2"}
            };

            var queryable = menu.BuildMockDbSet();
            _menuRepoMock.Setup(m => m.GetAll()).Returns(queryable.Object);
            _uow.Setup(uow => uow.SaveChangeAsync());

            await _sut.AddMenuAsync(request);

            _menuRepoMock.Verify(m => m.AddAsync(It.Is<Menu>(m =>
               m.Name == request.Name &&
               m.CategoryId == request.CategoryId &&
               m.Description == request.Description &&
               m.OriginalPrice == request.OriginalPrice &&
               m.DiscountPrice == request.DiscountPrice &&
               m.IsAvailable == request.IsAvailable &&
               m.IsOnSale == request.IsOnSale)), Times.Once);
            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteMenu_ShouldThrowKeyNotFoundException_WhenMenuDoesNotExist()
        {
            var menuId = Guid.NewGuid();
            _menuRepoMock.Setup(m => m.GetByIdAsync(menuId)).ReturnsAsync((Menu?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _sut.DeleteMenuAsync(menuId));

            exception.Message.Should().Be("Món ăn không tồn tại");

            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteMenu_ShouldThrowInvalidOperationException_WhenMenuIsOnSaleOrOnAvailable()
        {
            var menuId = Guid.NewGuid();

            var menu = new Menu
            {
                Id = menuId,
                Name = "Test",
                OriginalPrice = 1000,
                DiscountPrice = 200,
                IsAvailable = true,
                IsOnSale = false
            };

            _menuRepoMock.Setup(m => m.GetByIdAsync(menuId)).ReturnsAsync(menu);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _sut.DeleteMenuAsync(menuId));

            exception.Message.Should().Be("Món ăn đang được bán, hãy cập nhật lại trạng thái trước khi xóa");

            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteMenu_ShouldDeleteMenu_WhenMenuDoesExist()
        {
            var menuId = Guid.NewGuid();

            var menu = new Menu
            {
                Id = menuId,
                Name = "Test",
                OriginalPrice = 1000,
                DiscountPrice = 200,
                IsAvailable = false,
                IsOnSale = false
            };

            _menuRepoMock.Setup(m => m.GetByIdAsync(menuId)).ReturnsAsync(menu);
            _uow.Setup(uow => uow.SaveChangeAsync());

            await _sut.DeleteMenuAsync(menuId);

            _menuRepoMock.Verify(uow => uow.Remove(menu), Times.Once);
            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateMenu_ShouldThrowValidationDictionaryException_WhenRequestIsInvalid()
        {
            var menuId = Guid.NewGuid();

            var request = new MenuRequestDto
            {
                Name = "",
                CategoryId = Guid.NewGuid(),
                Description = "",
                OriginalPrice = 1000,
                DiscountPrice = null,
                IsAvailable = false,
                IsOnSale = false,
            };

            var exception = await Assert.ThrowsAsync<ValidationDictionaryException>(
                async () => await _sut.UpdateMenuAsync(menuId, request));

            exception.Should().NotBeNull();

            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateMenu_ShouldThrowKeyNotFoundException_WhenMenuDoesNotExist()
        {
            var menuId = Guid.NewGuid();

            var request = new MenuRequestDto
            {
                Name = "Test",
                CategoryId = Guid.NewGuid(),
                Description = "",
                OriginalPrice = 1000,
                DiscountPrice = null,
                IsAvailable = false,
                IsOnSale = false,
            };

            _menuRepoMock.Setup(m => m.GetByIdAsync(menuId)).ReturnsAsync((Menu?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _sut.UpdateMenuAsync(menuId, request));

            exception.Message.Should().Be("Món ăn không tồn tại");

            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateMenu_ShouldThrowDuplicateNameException_WhenMenuAlreadyExist()
        {
            var menuId = Guid.NewGuid();
            var current = new Menu { Id = menuId, Name = "test1" };
            var menu = new List<Menu>
            {
               current,
               new Menu { Name = "test"}
            };

            var request = new MenuRequestDto
            {
                Name = "test",
                CategoryId = Guid.NewGuid(),
                Description = "",
                OriginalPrice = 1000,
                DiscountPrice = null,
                IsAvailable = false,
                IsOnSale = false,
            };

            var queryable = menu.BuildMockDbSet();
            _menuRepoMock.Setup(m => m.GetByIdAsync(menuId)).ReturnsAsync(current);
            _menuRepoMock.Setup(m => m.GetAll()).Returns(queryable.Object);

            var exception = await Assert.ThrowsAsync<DuplicateNameException>(
                async () => await _sut.UpdateMenuAsync(menuId, request));

            exception.Message.Should().Be($"Menu {request.Name} đã tồn tại");

            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateMenu_ShouldUpdateMenun_WhenRequestIsValid()
        {
            var menuId = Guid.NewGuid();
            var current = new Menu { Id = menuId, Name = "test1" };
            var menu = new List<Menu>
            {
               current,
               new Menu { Name = "test2"}
            };

            var request = new MenuRequestDto
            {
                Name = "test",
                CategoryId = Guid.NewGuid(),
                Description = "",
                OriginalPrice = 1000,
                DiscountPrice = null,
                IsAvailable = false,
                IsOnSale = false,
            };

            var queryable = menu.BuildMockDbSet();
            _menuRepoMock.Setup(m => m.GetByIdAsync(menuId)).ReturnsAsync(current);
            _menuRepoMock.Setup(m => m.GetAll()).Returns(queryable.Object);
            _uow.Setup(uow => uow.SaveChangeAsync());

            await _sut.UpdateMenuAsync(menuId, request);

            current.Name.Should().Be(request.Name);
            current.CategoryId.Should().Be(request.CategoryId);
            current.Description.Should().Be(request.Description);
            current.OriginalPrice.Should().Be(request.OriginalPrice);
            current.DiscountPrice.Should().Be(request.DiscountPrice);
            current.IsAvailable.Should().Be(request.IsAvailable);
            current.IsOnSale.Should().Be(request.IsOnSale);

            _menuRepoMock.Verify(uow => uow.Update(current), Times.Once);
            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Once);
        }

        [Fact]
        public async Task GetRelatedMenus_ShouldReturnEmpty_WhenCurrentMenuDoesNotExist()
        {
            var menuId = Guid.NewGuid();

            _menuRepoMock.Setup(m => m.GetMenuWithCategoryAsync(menuId)).ReturnsAsync((MenuDto?)null);

            var result = await _sut.GetRelatedMenusAsync(menuId);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetRelatedMenus_ShouldReturnMenus_WhenCurrentMenuExists()
        {
            var menuId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var category = new Category { Id = categoryId, Name = "Food" };
            var current = new Menu
            {
                Id = menuId,
                CategoryId = categoryId,
                IsAvailable = true,
                Ratings = new List<Rating>()
            };

            var menus = new List<Menu>
            {
                current,
                new Menu {Id = Guid.NewGuid(), CategoryId = categoryId, Category = category, IsAvailable = true },
                new Menu {Id = Guid.NewGuid(), CategoryId = categoryId, Category = category, IsAvailable = true },
                new Menu {Id = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Category = new Category { Id = Guid.NewGuid(), Name = "Other" }, IsAvailable = true}
            };

            var queryable = menus.BuildMockDbSet();

            _menuRepoMock.Setup(m => m.GetByIdAsync(menuId)).ReturnsAsync(current);
            _menuRepoMock.Setup(m => m.GetAll()).Returns(queryable.Object);

            var result = await _sut.GetRelatedMenusAsync(menuId);

            result.Should().HaveCount(2);

            _menuRepoMock.Verify(m => m.GetByIdAsync(menuId), Times.Once);
        }

        [Fact]
        public async Task GetFeaturedMenus_ShouldReturnMenus_WhenRequestIsValid()
        {
            var categoryId = Guid.NewGuid();
            var menus = new List<Menu>
            {
                new Menu {Id = Guid.NewGuid(), Category = new Category { Id = Guid.NewGuid(), Name = "Other1" }, IsAvailable = true},
                new Menu {Id = Guid.NewGuid(), Category = new Category { Id = Guid.NewGuid(), Name = "Other2" }, IsAvailable = true},
                new Menu {Id = Guid.NewGuid(), Category = new Category { Id = Guid.NewGuid(), Name = "Other3" }, IsAvailable = true}
            };

            var queryable = menus.BuildMockDbSet();

            _menuRepoMock.Setup(m => m.GetAll()).Returns(queryable.Object);

            var result = await _sut.GetFeaturedMenusAsync();

            result.Should().HaveCount(3);

            _menuRepoMock.Verify(m => m.GetAll(), Times.Once);
        }

        [Fact]
        public async Task GetMenuById_ShouldThrowKeyNotFoundException_WhenMenuDoesNotExist()
        {
            var menuId = Guid.NewGuid();

            _menuRepoMock.Setup(m => m.GetByIdAsync(menuId)).ReturnsAsync((Menu?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _sut.GetMenuByIdAsync(menuId));

            exception.Message.Should().Be("Món ăn không tồn tại");

            _cache.Verify(m => m.GetAsync<MenuDto>(It.IsAny<string>()), Times.Once);
            _menuRepoMock.Verify(m => m.GetMenuWithCategoryAsync(menuId), Times.Once);
        }

        [Fact]
        public async Task GetMenuById_ShouldReturnCached_WhenCacheHit()
        {
            var menuId = Guid.NewGuid();

            var cached = new MenuDto
            {
                Id = menuId,
            };

            _cache.Setup(c => c.GetAsync<MenuDto>(CacheKeys.MenuDetail(menuId))).ReturnsAsync(cached);

            var result = await _sut.GetMenuByIdAsync(menuId);

            result.Should().BeSameAs(cached);

            _menuRepoMock.Verify(m => m.GetMenuWithCategoryAsync(menuId), Times.Never);
            _cache.Verify(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<MenuDto>(),
                It.IsAny<TimeSpan>()
                ), Times.Never);
        }

        [Fact]
        public async Task GetMenuById_ShouldReturnMenu_WhenCacheMiss()
        {
            var menuId = Guid.NewGuid();
            var menu = new MenuDto
            {
                Id = menuId,
                Category = "Vn Food",
            };

            _cache.Setup(c => c.GetAsync<MenuDto>(CacheKeys.MenuDetail(menuId))).ReturnsAsync((MenuDto?)null);
            _menuRepoMock.Setup(c => c.GetMenuWithCategoryAsync(menuId)).ReturnsAsync(menu);

            var result = await _sut.GetMenuByIdAsync(menuId);

            result.Should().NotBeNull();

            _menuRepoMock.Verify(m => m.GetMenuWithCategoryAsync(menuId), Times.Once);
            _cache.Verify(c => c.SetAsync(
                CacheKeys.MenuDetail(menuId),
                It.IsAny<MenuDto>(),
                TimeSpan.FromMinutes(10)
                ), Times.Once);
        }

        [Fact]
        public async Task GetAllMenus_ShouldReturnAllMenus_WhenNoFiltersApplied()
        {
            var categoryId = Guid.NewGuid();
            var category = new Category { Id = categoryId, Name = "Vietnamese Food" };
            var menus = new List<Menu>()
            {
                new Menu
                {
                    Id = Guid.NewGuid(),
                    Category = category
                },

                new Menu
                {
                    Id = Guid.NewGuid(),
                    Category = category
                },
            };

            var queryable = menus.BuildMockDbSet();

            _menuRepoMock.Setup(m => m.GetAll()).Returns(queryable.Object);

            var menuParams = new MenuParams
            {
                Page = 1,
                PageSize = 3
            };

            var result = await _sut.GetAllMenusAsync(menuParams);

            result.Should().NotBeNull();
            result.Total.Should().Be(2);
            result.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllMenusAsync_ShouldSortByPriceAscending_WhenSortByPriceAsc()
        {
            var categoryId = Guid.NewGuid();
            var category = new Category { Id = categoryId, Name = "VN Food" };
            var mockData = new List<Menu>
            {
                 new Menu
                 {
                     Id = Guid.NewGuid(),
                     Name = "Expensive Dish",
                     CategoryId = categoryId,
                     Category = category,
                     OriginalPrice = 100000,
                     DiscountPrice = null,
                     IsOnSale = false,
                     IsAvailable = true,
                     SoldQuantity = 3,
                     CreatedAt = DateTime.UtcNow,
                 },

                 new Menu
                 {
                     Id = Guid.NewGuid(),
                     Name = "Cheap Dish",
                     CategoryId = categoryId,
                     Category = category,
                     OriginalPrice = 30000,
                     DiscountPrice = null,
                     IsOnSale = false,
                     IsAvailable = true,
                     SoldQuantity = 1,
                     CreatedAt = DateTime.UtcNow,
                 },
                 new Menu
                 {
                     Id = Guid.NewGuid(),
                     Name = "Medium Dish",
                     CategoryId = categoryId,
                     Category = category,
                     OriginalPrice = 50000,
                     DiscountPrice = null,
                     IsOnSale = false,
                     IsAvailable = true,
                     SoldQuantity = 2,
                     CreatedAt = DateTime.UtcNow,
                 }
            };

            var queryableMock = mockData.BuildMockDbSet();

            _menuRepoMock.Setup(m => m.GetAll()).Returns(queryableMock.Object);

            var menuParams = new MenuParams
            {
                SortBy = "price",
                SortOrder = "asc",
                Page = 1,
                PageSize = 10
            };

            var result = await _sut.GetAllMenusAsync(menuParams);

            result.Data.Should().HaveCount(3);
            result.Data.First().Name.Should().Be("Cheap Dish");
            result.Data.Last().Name.Should().Be("Expensive Dish");
            result.Data.Select(m => m.OriginalPrice).Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task GetAllMenusAsync_ShouldReturnAllData_WhenPageIsZero()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var category = new Category { Id = categoryId, Name = "VN Food" };
            var mockData = new List<Menu>
            {
                 new Menu
                 {
                     Id = Guid.NewGuid(),
                     Name = "Expensive Dish",
                     CategoryId = categoryId,
                     Category = category,
                     OriginalPrice = 100000,
                     DiscountPrice = null,
                     IsOnSale = false,
                     IsAvailable = true,
                     SoldQuantity = 3,
                     CreatedAt = DateTime.UtcNow,
                 },

                 new Menu
                 {
                     Id = Guid.NewGuid(),
                     Name = "Cheap Dish",
                     CategoryId = categoryId,
                     Category= category,
                     OriginalPrice = 30000,
                     DiscountPrice = null,
                     IsOnSale = false,
                     IsAvailable = true,
                     SoldQuantity = 1,
                     CreatedAt = DateTime.UtcNow,
                 },
            };

            var queryableMock = mockData.BuildMockDbSet();

            _menuRepoMock.Setup(m => m.GetAll()).Returns(queryableMock.Object);

            var menuParams = new MenuParams
            {
                Page = 0,
                PageSize = 0
            };

            var result = await _sut.GetAllMenusAsync(menuParams);
            // Assert
            result.Total.Should().Be(2);
            result.Data.Should().HaveCount(2);
        }
    }
}
