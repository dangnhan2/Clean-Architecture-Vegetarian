using FluentAssertions;
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
    public class CartUnitTest
    {
        private readonly Mock<ICartRepo> _cartRepoMock;
        private readonly Mock<IUserRepo> _userRepoMock;
        private readonly Mock<IUnitOfWork> _uow;
        private readonly CartService _sut;
        public CartUnitTest()
        {
            _cartRepoMock = new Mock<ICartRepo>();
            _userRepoMock = new Mock<IUserRepo>();
            _uow = new Mock<IUnitOfWork>();

            _uow.SetupGet(uow => uow.Cart).Returns(_cartRepoMock.Object);
            _uow.SetupGet(uow => uow.User).Returns(_userRepoMock.Object);

            _sut = new CartService(_uow.Object);
        }

        [Fact]
        public async Task AddToCart_ThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            var userId = Guid.NewGuid();
            var request = new CartRequestDto
            {
                UserId = userId,
                CartItems = new List<CartItemRequestDto>
                {
                    new CartItemRequestDto
                    {
                        MenuId = Guid.NewGuid(),
                        Quantity = 1,
                        UnitPrice = 20000
                    }
                }
            };

            _userRepoMock.Setup(u => u.GetUserContainsCartAsync(userId)).ReturnsAsync((User?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _sut.AddToCartAsync(request));

            exception.Message.Should().Be("Người dùng không tồn tại");

            _userRepoMock.Verify(u => u.GetUserContainsCartAsync(userId), Times.Once);
        }

        [Fact]
        public async Task AddToCartAsync_ShouldThrowKeyNotFoundException_WhenCartNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Cart = new Cart()// User has Carts collection but cart not found
            };

            var request = new CartRequestDto
            {
                UserId = userId,
                CartItems = new List<CartItemRequestDto>
                {
                   new CartItemRequestDto { MenuId = Guid.NewGuid(), Quantity = 1, UnitPrice = 50000 }
                }
            };

            _userRepoMock.Setup(u => u.GetUserContainsCartAsync(userId))
                        .ReturnsAsync(user);
            _cartRepoMock.Setup(c => c.GetCartByCustomerAsync(userId))
                        .ReturnsAsync((Cart?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _sut.AddToCartAsync(request)
            );

            exception.Message.Should().Be("Giỏ hàng trống / không tồn tại");
        }

        [Fact]
        public async Task AddToCart_ShouldCreateNewCart_WhenUserHasNoCart()
        {
            var userId = Guid.NewGuid();
            var menuId = Guid.NewGuid();

            var request = new CartRequestDto
            {
                UserId = userId,
                CartItems = new List<CartItemRequestDto>{
                    new CartItemRequestDto
                    {
                        MenuId = menuId,
                        Quantity = 1,
                        UnitPrice = 20000
                    }
                }
            };

            var user = new User
            {
                Id = userId,
                Cart = null
            };

            _userRepoMock.Setup(u => u.GetUserContainsCartAsync(userId)).ReturnsAsync(user);
            _uow.Setup(uow => uow.SaveChangeAsync());

            await _sut.AddToCartAsync(request);

            _cartRepoMock.Verify(c => c.AddAsync(It.Is<Cart>(cart =>
              cart.Id != Guid.Empty &&
              cart.UserId == userId &&
              cart.CartItems.First().MenuId == menuId &&
              cart.CartItems.First().Quantity == 1 &&
              cart.CartItems.First().UnitPrice == 20000)), Times.Once);

            _uow.Verify(u => u.SaveChangeAsync(), Times.Once);

        }

        [Fact]
        public async Task AddToCart_ShouldIncreaseQuantity_WhenItemAlreadyExist()
        {
            var userId = Guid.NewGuid();
            var cartId = Guid.NewGuid();
            var cartItemId = Guid.NewGuid();
            var menuId = Guid.NewGuid();

            var existingCartItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cartId,
                MenuId = menuId,
                Quantity = 3,
                UnitPrice = 50000
            };

            var cart = new Cart
            {
                Id = cartId,
                UserId = userId,
                CartItems = new List<CartItem> { existingCartItem }
            };

            var user = new User
            {
                Id = userId,
                Cart = cart
            };

            var request = new CartRequestDto
            {
                UserId = userId,
                CartItems = new List<CartItemRequestDto>{
                    new CartItemRequestDto
                    {
                        MenuId = menuId,
                        Quantity = 3,
                        UnitPrice = 50000
                    }
                }
            };

            _userRepoMock.Setup(u => u.GetUserContainsCartAsync(userId)).ReturnsAsync(user);
            _cartRepoMock.Setup(c => c.GetCartByCustomerAsync(userId)).ReturnsAsync(cart);
            _uow.Setup(uow => uow.SaveChangeAsync());

            await _sut.AddToCartAsync(request);

            existingCartItem.Quantity.Should().Be(6);
            _cartRepoMock.Verify(c => c.Update(cart), Times.Once);
            _uow.Verify(u => u.SaveChangeAsync(), Times.Once);
        }

        [Fact]
        public async Task AddToCart_ShouldRemoveItem_WhenQuantityEqualZero()
        {
            var userId = Guid.NewGuid();
            var cartId = Guid.NewGuid();
            var cartItemId = Guid.NewGuid();
            var menuId = Guid.NewGuid();

            var existingCartItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cartId,
                MenuId = menuId,
                Quantity = 3,
                UnitPrice = 50000
            };

            var cart = new Cart
            {
                Id = cartId,
                UserId = userId,
                CartItems = new List<CartItem> { existingCartItem }
            };

            var user = new User
            {
                Id = userId,
                Cart = cart
            };

            var request = new CartRequestDto
            {
                UserId = userId,
                CartItems = new List<CartItemRequestDto>{
                    new CartItemRequestDto
                    {
                        MenuId = menuId,
                        Quantity = 0,
                        UnitPrice = 50000
                    }
                }
            };

            _userRepoMock.Setup(u => u.GetUserContainsCartAsync(userId)).ReturnsAsync(user);
            _cartRepoMock.Setup(c => c.GetCartByCustomerAsync(userId)).ReturnsAsync(cart);
            _uow.Setup(uow => uow.SaveChangeAsync());

            await _sut.AddToCartAsync(request);

            cart.CartItems.Should().BeEmpty();
            _cartRepoMock.Verify(c => c.Remove(cart), Times.Once);
            _uow.Verify(u => u.SaveChangeAsync(), Times.Once);
        }

        [Fact]
        public async Task AddToCart_ShouldAddNewItem_WhenItemDoesNotExistInCart()
        {
            var userId = Guid.NewGuid();
            var cartId = Guid.NewGuid();
            var cartItemId = Guid.NewGuid();
            var menuId = Guid.NewGuid();
            var newMenuId = Guid.NewGuid();

            var existingCartItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cartId,
                MenuId = menuId,
                Quantity = 3,
                UnitPrice = 50000
            };

            var cart = new Cart
            {
                Id = cartId,
                UserId = userId,
                CartItems = new List<CartItem> { existingCartItem }
            };

            var user = new User
            {
                Id = userId,
                Cart = cart
            };

            var request = new CartRequestDto
            {
                UserId = userId,
                CartItems = new List<CartItemRequestDto>{
                    new CartItemRequestDto
                    {
                        MenuId = newMenuId,
                        Quantity = 3,
                        UnitPrice = 50000
                    }
                }
            };

            _userRepoMock.Setup(u => u.GetUserContainsCartAsync(userId)).ReturnsAsync(user);
            _cartRepoMock.Setup(c => c.GetCartByCustomerAsync(userId)).ReturnsAsync(cart);
            _uow.Setup(uow => uow.SaveChangeAsync());

            await _sut.AddToCartAsync(request);

            cart.CartItems.Should().HaveCount(2);
            cart.CartItems.Should().Contain(item =>
              item.MenuId == newMenuId &&
              item.Quantity == 3 &&
              item.UnitPrice == 50000);

            _cartRepoMock.Verify(c => c.Update(cart), Times.Once);
            _uow.Verify(u => u.SaveChangeAsync(), Times.Once);
        }

        [Fact]
        public async Task AddToCart_ShouldRemoveCart_WhenAllItemsAreRemoved()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cartId = Guid.NewGuid();
            var menuId1 = Guid.NewGuid();
            var menuId2 = Guid.NewGuid();

            var cart = new Cart
            {
                Id = cartId,
                UserId = userId,
                CartItems = new List<CartItem>
                {
                   new CartItem { Id = Guid.NewGuid(), CartId = cartId, MenuId = menuId1, Quantity = 2, UnitPrice = 50000 },
                   new CartItem { Id = Guid.NewGuid(), CartId = cartId, MenuId = menuId2, Quantity = 1, UnitPrice = 30000 }
                }
            };

            var user = new User
            {
                Id = userId,
                Cart = cart
            };

            var request = new CartRequestDto
            {
                UserId = userId,
                CartItems = new List<CartItemRequestDto>
                {
                   new CartItemRequestDto { MenuId = menuId1, Quantity = 0, UnitPrice = 50000 }, // Remove both
                   new CartItemRequestDto { MenuId = menuId2, Quantity = 0, UnitPrice = 30000 }
                }
            };

            _userRepoMock.Setup(u => u.GetUserContainsCartAsync(userId))
                        .ReturnsAsync(user);
            _cartRepoMock.Setup(c => c.GetCartByCustomerAsync(userId))
                        .ReturnsAsync(cart);
            _uow.Setup(x => x.SaveChangeAsync());

            await _sut.AddToCartAsync(request);

            cart.CartItems.Should().BeEmpty();
            _cartRepoMock.Verify(c => c.Remove(cart), Times.Once);
            _cartRepoMock.Verify(c => c.Update(It.IsAny<Cart>()), Times.Never);
            _uow.Verify(u => u.SaveChangeAsync(), Times.Once);
        }

        [Fact]
        public async Task GetCart_ShouldReturnCart_WhenCartHasItems()
        {
            var userId = Guid.NewGuid();
            var cartId = Guid.NewGuid();
            var menuId = Guid.NewGuid();

            var cart = new Cart
            {
                Id = cartId,
                UserId = userId,
                CartItems = new List<CartItem> {
                   new CartItem {Id = menuId, Quantity = 2, UnitPrice = 2000, Menu = new Menu
                   {
                    Id = menuId,
                    Name = "Pizza",
                    ImageUrl = "img.png"
                   }}
                }
            };

            _cartRepoMock.Setup(c => c.GetCartByCustomerAsync(userId)).ReturnsAsync(cart);

            var result = await _sut.GetCartByCustomer(userId);

            result.Should().NotBeNull();
            result.Id.Should().Be(cartId);
            result.UserId.Should().Be(userId);

            _cartRepoMock.Verify(c => c.GetCartByCustomerAsync(userId), Times.Once);

        }

        [Fact]
        public async Task GetCart_ShouldReturnEmpty_WhenCartNull()
        {
            var userId = Guid.NewGuid();
            var cartId = Guid.NewGuid();

            var cart = new Cart
            {
                Id = cartId,
                UserId = userId,
                CartItems = new List<CartItem>() // Empty cart items
            };

            _cartRepoMock.Setup(c => c.GetCartByCustomerAsync(userId))
                        .ReturnsAsync(cart);
            var result = await _sut.GetCartByCustomer(userId);

            result.Should().NotBeNull();
            result.Id.Should().Be(cartId);
            result.UserId.Should().Be(userId);

            _cartRepoMock.Verify(c => c.GetCartByCustomerAsync(userId), Times.Once);
        }
    }
}
