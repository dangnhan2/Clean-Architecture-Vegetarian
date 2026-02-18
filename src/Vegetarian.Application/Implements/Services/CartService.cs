using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Dtos.Request;
using Vegetarian.Application.Dtos.Response;
using Vegetarian.Application.Implements.Interface;
using Vegetarian.Domain.Models;

namespace Vegetarian.Application.Implements.Services
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CartService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task AddToCartAsync(CartRequestDto request)
        {
            var user = await _unitOfWork.User.GetUserContainsCartAsync(request.UserId);

            if (user == null) throw new KeyNotFoundException("Người dùng không tồn tại");

            if (user.Cart == null)
            {
                var cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId
                };

                // Thêm món ăn vào cart
                foreach (var dish in request.CartItems)
                {
                    var item = MappingCartItem(cart.Id, dish);

                    cart.CartItems.Add(item);
                }
                await _unitOfWork.Cart.AddAsync(cart);
            }
            else
            {
                var cart = await _unitOfWork.Cart.GetCartByCustomerAsync(request.UserId);

                if (cart == null) throw new KeyNotFoundException("Giỏ hàng trống / không tồn tại");

                foreach (var dish in request.CartItems)
                {
                    // Find item if it already exists in cart
                    var existItem = cart.CartItems.FirstOrDefault(i => i.MenuId == dish.MenuId);
                    if (existItem != null)
                    {
                        // Increase/Decrease if quantity > 0 else quantity = 0 => remove
                        if (dish.Quantity > 0)
                            existItem.Quantity += dish.Quantity;
                        else
                            cart.CartItems.Remove(existItem);
                    }
                    else
                    {
                        // add to cart if its a new item
                        var item = MappingCartItem(cart.Id, dish);

                        cart.CartItems.Add(item);
                    }
                }

                if (cart.CartItems.Count() > 0)
                    _unitOfWork.Cart.Update(cart);
                else
                    _unitOfWork.Cart.Remove(cart);

            }
            await _unitOfWork.SaveChangeAsync();
        }

        public async Task<CartDto> GetCartByCustomer(Guid userId)
        {
            var cart = await _unitOfWork.Cart.GetCartByCustomerAsync(userId);

            if (cart == null) return new CartDto();

            var cartToDTO = new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                Items = cart.CartItems.Select(ct => new CartItemDto
                {
                    Id = ct.Id,
                    MenuId = ct.MenuId,
                    MenuName = ct.Menu.Name,
                    ImageUrl = ct.Menu.ImageUrl,
                    Quantity = ct.Quantity,
                    UnitPrice = ct.UnitPrice
                }).ToList()
            };

            return cartToDTO;
        }


        #region helper method
        private CartItem MappingCartItem(Guid cartId, CartItemRequestDto item)
        {
            var newItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cartId,
                MenuId= item.MenuId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            };

            return newItem;
        }
        #endregion
    }
}
