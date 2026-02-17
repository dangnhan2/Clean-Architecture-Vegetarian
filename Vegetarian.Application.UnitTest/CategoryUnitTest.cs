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
using Vegetarian.Application.Contants;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Implements.Services;
using Vegetarian.Application.Validator;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.UnitTest
{
    public class CategoryUnitTest
    {
        private readonly Mock<IUnitOfWork> _uow;
        private readonly Mock<ICachingProvider> _cache;
        private readonly CategoryService _sut;
        private readonly Mock<ICategoryRepo> _categoryRepoMock;

        public CategoryUnitTest()
        {
            _uow = new Mock<IUnitOfWork>();
            _cache = new Mock<ICachingProvider>();
            _categoryRepoMock = new Mock<ICategoryRepo>();

            _uow.SetupGet(uow => uow.Category).Returns(_categoryRepoMock.Object);
            _sut = new CategoryService(_uow.Object, _cache.Object);
        }

        [Fact]
        public async Task GetCategories_ShouldReturnCachedCategory_WhenCacheHit()
        {
            string cacheKey = CacheKeys.CATEGORIES_PREFIX;

            var cached = new List<CategoryDto>
            {
                new CategoryDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Test",
                }
            };

            var (sut, uow, categoryRepo, cache) = CreateSut(MockBehavior.Strict);

            cache.Setup(c => c.GetAsync<IEnumerable<CategoryDto>>(cacheKey)).ReturnsAsync(cached);

            var result = await sut.GetAllAsync();

            result.Should().BeSameAs(cached);

            categoryRepo.Verify(c => c.GetAll(), Times.Never);
            cache.Verify(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<CategoryDto>>(),
                It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task GetCategories_ShouldReturnCategory_WhenCacheMiss()
        {
            string cacheKey = CacheKeys.CATEGORIES_PREFIX;

            var mockData = new List<Category>
            {
                new Category { Id = Guid.NewGuid(), Name = "Test" }
            };

            var queryable = mockData.BuildMockDbSet();

            _cache.Setup(c => c.GetAsync<IEnumerable<CategoryDto>>(cacheKey)).ReturnsAsync((IEnumerable<CategoryDto>?)null);
            _categoryRepoMock.Setup(c => c.GetAll()).Returns(queryable.Object);

            var result = (await _sut.GetAllAsync()).ToList();

            result.Should().HaveCount(1);

            _categoryRepoMock.Verify(c => c.GetAll(), Times.Once);
            _cache.Verify(c => c.SetAsync(
                cacheKey,
                It.Is<IEnumerable<CategoryDto>>(c => c.Count() == 1),
                TimeSpan.FromMinutes(30)), Times.Once);
        }

        [Fact]
        public async Task AddCategory_ShouldThrowValidatorException_WhenRequestIsInvalid()
        {
            var request = new CategoryRequestDto
            {
                Name = "",
            };

            var exception = await Assert.ThrowsAsync<ValidationDictionaryException>(
                async () => await _sut.AddAsync(request));

            exception.Should().NotBeNull();

            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Never);
            _cache.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);

        }

        [Fact]
        public async Task AddCategory_ShouldThrowDuplicateNameException_WhenCategoryAlreadyExist()
        {
            var request = new CategoryRequestDto
            {
                Name = "Test",
            };

            var categoríes = new List<Category>
            {
                new Category { Id = Guid.NewGuid(), Name = "Test" }
            };

            var queryable = categoríes.BuildMockDbSet();

            _categoryRepoMock.Setup(c => c.GetAll()).Returns(queryable.Object);

            var exception = await Assert.ThrowsAsync<DuplicateNameException>(
               async () => await _sut.AddAsync(request));

            exception.Message.Should().Be($"{request.Name} đã tồn tại");

            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Never);
            _cache.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AddCategory_ShouldAddCategory_WhenRequestIsValid()
        {
            var request = new CategoryRequestDto
            {
                Name = "Test1",
            };

            var categoríes = new List<Category>
            {
                new Category { Id = Guid.NewGuid(), Name = "Test" }
            };

            var queryable = categoríes.BuildMockDbSet();
            _categoryRepoMock.Setup(c => c.GetAll()).Returns(queryable.Object);
            _uow.Setup(uow => uow.SaveChangeAsync());

            await _sut.AddAsync(request);

            _categoryRepoMock.Verify(c => c.AddAsync(It.Is<Category>(c =>
              c.Name == request.Name)), Times.Once);
            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Once);
            _cache.Verify(c => c.RemoveAsync(CacheKeys.CATEGORIES_PREFIX), Times.Once);
        }

        [Fact]
        public async Task UpdateAddress_ShouldThrowValidatorException_WhenRequestIsInvalid()
        {
            var categoryId = Guid.NewGuid();
            var request = new CategoryRequestDto
            {
                Name = "",
            };

            var exception = await Assert.ThrowsAsync<ValidationDictionaryException>(
                async () => await _sut.UpdateAsync(categoryId, request));

            exception.Should().NotBeNull();

            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Never);
            _cache.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAddress_ShouldThrowKeyNotFoundException_WhenCategoryDoesNotExist()
        {
            var categoryId = Guid.NewGuid();
            var request = new CategoryRequestDto
            {
                Name = "Test",
            };

            _categoryRepoMock.Setup(c => c.GetByIdAsync(categoryId)).ReturnsAsync((Category?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _sut.UpdateAsync(categoryId, request));

            exception.Message.Should().Be("Không tìm thấy danh mục");

            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Never);
            _cache.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAddress_ShouldThrowDuplicateNameException_WhenCategoryAlreadyExist()
        {
            var categoryId = Guid.NewGuid();
            var request = new CategoryRequestDto
            {
                Name = "Test",
            };

            var current = new Category { Id = categoryId, Name = "Test1" };

            var categoríes = new List<Category>
            {
                current,
                new Category { Id = Guid.NewGuid(), Name = "Test" },
            };

            var queryable = categoríes.BuildMockDbSet();

            _categoryRepoMock.Setup(c => c.GetByIdAsync(categoryId)).ReturnsAsync(current);
            _categoryRepoMock.Setup(c => c.GetAll()).Returns(queryable.Object);

            var exception = await Assert.ThrowsAsync<DuplicateNameException>(
               async () => await _sut.UpdateAsync(categoryId, request));

            exception.Message.Should().Be($"{request.Name} đã tồn tại");

            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Never);
            _cache.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAddress_ShouldUpdateAddress_WhenRequestIsValid()
        {
            var categoryId = Guid.NewGuid();
            var request = new CategoryRequestDto
            {
                Name = "TestUpdate",
            };

            var current = new Category { Id = categoryId, Name = "Test1" };

            var categoríes = new List<Category>
            {
                current,
                new Category { Id = Guid.NewGuid(), Name = "Test" },
            };

            var queryable = categoríes.BuildMockDbSet();

            _categoryRepoMock.Setup(c => c.GetByIdAsync(categoryId)).ReturnsAsync(current);
            _categoryRepoMock.Setup(c => c.GetAll()).Returns(queryable.Object);
            _uow.Setup(uow => uow.SaveChangeAsync());

            await _sut.UpdateAsync(categoryId, request);

            current.Name.Should().Be(request.Name);

            _categoryRepoMock.Verify(c => c.Update(current), Times.Once);
            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Once);
            _cache.Verify(c => c.RemoveAsync(CacheKeys.CATEGORIES_PREFIX), Times.Once);
        }

        [Fact]
        public async Task DeleteAddress_ShouldThrowKeyNotFoundException_WhenCategoryDoesNotExist()
        {
            var categoryId = Guid.NewGuid();

            _categoryRepoMock.Setup(c => c.GetByIdAsync(categoryId)).ReturnsAsync((Category?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _sut.DeleteAsync(categoryId));

            exception.Message.Should().Be("Không tìm thấy danh mục");

            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Never);
            _cache.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAddress_ShouldDeleteAddress_WhenCategoryExist()
        {
            var categoryId = Guid.NewGuid();

            var current = new Category
            {
                Id = categoryId,
                Name = "Test1"
            };

            _categoryRepoMock.Setup(c => c.GetByIdAsync(categoryId)).ReturnsAsync(current);
            _uow.Setup(uow => uow.SaveChangeAsync());

            await _sut.DeleteAsync(categoryId);

            _categoryRepoMock.Verify(c => c.Remove(current), Times.Once);
            _uow.Verify(uow => uow.SaveChangeAsync(), Times.Once);
            _cache.Verify(c => c.RemoveAsync(CacheKeys.CATEGORIES_PREFIX), Times.Once);
        }

        private (CategoryService sut, Mock<IUnitOfWork> uow, Mock<ICategoryRepo> categoryRepo, Mock<ICachingProvider> cache) CreateSut(MockBehavior behavior)
        {
            var categoryRepo = new Mock<ICategoryRepo>();
            var uow = new Mock<IUnitOfWork>();
            var cache = new Mock<ICachingProvider>();

            uow.SetupGet(uow => uow.Category).Returns(categoryRepo.Object);

            var sut = new CategoryService(uow.Object, cache.Object);

            return (sut, uow, categoryRepo, cache);
        }
    }
}
