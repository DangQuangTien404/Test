using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fit3d.BLL.DTOs;

namespace Fit3d.BLL.Interfaces
{
    public interface ICartService
    {
        Task<ICollection<CartItemDTO>> GetCartByUserIdAsync(Guid userId);
        Task<CartItemDTO> AddToCartAsync(CreateCartItemDTO createDto);
        Task<CartItemDTO?> UpdateQuantityAsync(Guid id, int quantity);
        Task<bool> RemoveFromCartAsync(Guid id);
        Task<bool> ClearCartAsync(Guid userId);
    }
}
