using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fit3d.BLL.DTOs;
using Fit3d.BLL.Interfaces;
using FIt3d.DAL.Entities;
using FIt3d.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Fit3d.BLL.Services
{
    public class CartService : ICartService
    {
        private readonly IGenericRepository<CartItem> _cartRepository;
        private readonly IGenericRepository<Product> _productRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CartService(
            IGenericRepository<CartItem> cartRepository,
            IGenericRepository<Product> productRepository,
            IUnitOfWork unitOfWork)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ICollection<CartItemDTO>> GetCartByUserIdAsync(Guid userId)
        {
            var items = await _cartRepository.GetListAsync(
                predicate: x => x.UserId == userId,
                include: x => x.Include(c => c.Product)
            );

            return items.Select(ToDTO).ToList();
        }

        public async Task<CartItemDTO> AddToCartAsync(CreateCartItemDTO createDto)
        {
            // Check if product exists
            var product = await _productRepository.GetByIdAsync(createDto.ProductId);
            if (product == null || product.IsDeleted) throw new ArgumentException("Product not found");

            // Check if item already exists in cart
            var existingItem = await _cartRepository.SingleOrDefaultAsync(
                predicate: x => x.UserId == createDto.UserId &&
                                x.ProductId == createDto.ProductId &&
                                x.Size == createDto.Size &&
                                x.Color == createDto.Color,
                include: x => x.Include(c => c.Product)
            );

            if (existingItem != null)
            {
                existingItem.Quantity += createDto.Quantity;
                _cartRepository.UpdateAsync(existingItem);
                await _unitOfWork.SaveChangesAsync();
                return ToDTO(existingItem);
            }

            var newItem = new CartItem
            {
                Id = Guid.NewGuid(),
                UserId = createDto.UserId,
                ProductId = createDto.ProductId,
                Quantity = createDto.Quantity,
                Size = createDto.Size,
                Color = createDto.Color,
                CreatedAt = DateTime.UtcNow
            };

            await _cartRepository.InsertAsync(newItem);
            await _unitOfWork.SaveChangesAsync();

            // Re-fetch or manually set product for DTO
            newItem.Product = product;
            return ToDTO(newItem);
        }

        public async Task<CartItemDTO?> UpdateQuantityAsync(Guid id, int quantity)
        {
            var item = await _cartRepository.SingleOrDefaultAsync(
                predicate: x => x.Id == id,
                include: x => x.Include(c => c.Product)
            );

            if (item == null) return null;

            item.Quantity = quantity;
            _cartRepository.UpdateAsync(item);
            await _unitOfWork.SaveChangesAsync();

            return ToDTO(item);
        }

        public async Task<bool> RemoveFromCartAsync(Guid id)
        {
            var item = await _cartRepository.GetByIdAsync(id);
            if (item == null) return false;

            _cartRepository.DeleteAsync(item); // Hard delete for cart items usually
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ClearCartAsync(Guid userId)
        {
            var items = await _cartRepository.GetListAsync(x => x.UserId == userId);
            if (!items.Any()) return false;

            _cartRepository.DeleteRangeAsync(items);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static CartItemDTO ToDTO(CartItem entity)
        {
            return new CartItemDTO
            {
                Id = entity.Id,
                ProductId = entity.ProductId,
                ProductName = entity.Product?.Name ?? "Unknown",
                ProductPrice = entity.Product?.SalePrice ?? entity.Product?.Price ?? 0,
                ProductImageUrl = entity.Product?.ImageUrl,
                Quantity = entity.Quantity,
                Size = entity.Size,
                Color = entity.Color
            };
        }
    }
}
