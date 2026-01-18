using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fit3d.BLL.DTOs;
using Fit3d.BLL.Interfaces;
using FIt3d.DAL.Common;
using FIt3d.DAL.Entities;
using FIt3d.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Fit3d.BLL.Services
{
    public class OrderService : IOrderService
    {
        private readonly IGenericRepository<Order> _orderRepository;
        private readonly IGenericRepository<Product> _productRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public OrderService(
            IGenericRepository<Order> orderRepository,
            IGenericRepository<Product> productRepository,
            IGenericRepository<User> userRepository,
            IUnitOfWork unitOfWork)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ICollection<OrderDTO>> GetAllAsync()
        {
            var entities = await _orderRepository.GetListAsync(
                predicate: x => !x.IsDeleted,
                include: x => x.Include(o => o.OrderItems).ThenInclude(oi => oi.Product).Include(o => o.User)
            );
            return entities.Select(ToDTO).ToList();
        }

        public async Task<PagingResponse<OrderDTO>> GetPagingAsync(int page, int size, Guid? userId = null, int? status = null)
        {
            var result = await _orderRepository.GetPagingListSelectedAsync(
                selector: x => ToDTO(x),
                predicate: x => !x.IsDeleted &&
                                (!userId.HasValue || x.UserId == userId) &&
                                (!status.HasValue || (int)x.Status == status),
                orderBy: x => x.OrderByDescending(o => o.CreatedAt),
                include: x => x.Include(o => o.OrderItems).ThenInclude(oi => oi.Product).Include(o => o.User),
                page: page,
                size: size
            );
            return result;
        }

        public async Task<OrderDTO?> GetByIdAsync(Guid id)
        {
            var entity = await _orderRepository.SingleOrDefaultAsync(
                predicate: x => x.Id == id && !x.IsDeleted,
                include: x => x.Include(o => o.OrderItems).ThenInclude(oi => oi.Product).Include(o => o.User)
            );
            return entity == null ? null : ToDTO(entity);
        }

        public async Task<OrderDTO> CreateAsync(CreateOrderDTO createDto)
        {
            var user = await _userRepository.GetByIdAsync(createDto.UserId);
            if (user == null || user.IsDeleted) throw new ArgumentException("User not found");

            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = DateTime.UtcNow.ToString("yyyyMMddHHmmss") + new Random().Next(100, 999),
                ShippingFee = createDto.ShippingFee,
                DiscountAmount = createDto.DiscountAmount,
                Status = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                PaymentMethod = createDto.PaymentMethod,
                ShippingAddress = createDto.ShippingAddress,
                ReceiverName = createDto.ReceiverName,
                ReceiverPhone = createDto.ReceiverPhone,
                Note = createDto.Note,
                UserId = createDto.UserId,
                CreatedAt = DateTime.UtcNow
            };

            decimal totalAmount = 0;

            foreach (var itemDto in createDto.OrderItems)
            {
                var product = await _productRepository.GetByIdAsync(itemDto.ProductId);
                if (product == null || product.IsDeleted) throw new ArgumentException($"Product {itemDto.ProductId} not found");

                // Check stock logic could be added here
                if (product.StockQuantity < itemDto.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for product {product.Name}");
                }

                // Decrease stock
                product.StockQuantity -= itemDto.Quantity;
                _productRepository.UpdateAsync(product);

                decimal price = product.SalePrice ?? product.Price;
                decimal itemTotal = price * itemDto.Quantity;

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = price,
                    TotalPrice = itemTotal,
                    Size = itemDto.Size,
                    Color = itemDto.Color,
                    CreatedAt = DateTime.UtcNow
                };

                order.OrderItems.Add(orderItem);
                totalAmount += itemTotal;
            }

            order.TotalAmount = totalAmount + order.ShippingFee - (order.DiscountAmount ?? 0);

            await _orderRepository.InsertAsync(order);
            await _unitOfWork.SaveChangesAsync();

            // Re-fetch with includes for DTO
            return (await GetByIdAsync(order.Id))!;
        }

        public async Task<OrderDTO?> UpdateAsync(Guid id, UpdateOrderDTO updateDto)
        {
            var entity = await _orderRepository.GetByIdAsync(id);
            if (entity == null || entity.IsDeleted) return null;

            if (updateDto.Status.HasValue) entity.Status = updateDto.Status.Value;
            if (updateDto.PaymentStatus.HasValue) entity.PaymentStatus = updateDto.PaymentStatus.Value;
            if (updateDto.ShippedAt.HasValue) entity.ShippedAt = updateDto.ShippedAt.Value;
            if (updateDto.DeliveredAt.HasValue) entity.DeliveredAt = updateDto.DeliveredAt.Value;
            if (updateDto.Note != null) entity.Note = updateDto.Note;

            entity.UpdatedAt = DateTime.UtcNow;

            _orderRepository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _orderRepository.GetByIdAsync(id);
            if (entity == null || entity.IsDeleted) return false;

            // Maybe restore stock if cancelled?
            // For now just soft delete.

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            _orderRepository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static OrderDTO ToDTO(Order entity)
        {
            return new OrderDTO
            {
                Id = entity.Id,
                OrderNumber = entity.OrderNumber,
                TotalAmount = entity.TotalAmount,
                DiscountAmount = entity.DiscountAmount,
                ShippingFee = entity.ShippingFee,
                Status = entity.Status,
                PaymentStatus = entity.PaymentStatus,
                PaymentMethod = entity.PaymentMethod,
                ShippingAddress = entity.ShippingAddress,
                ReceiverName = entity.ReceiverName,
                ReceiverPhone = entity.ReceiverPhone,
                Note = entity.Note,
                ShippedAt = entity.ShippedAt,
                DeliveredAt = entity.DeliveredAt,
                UserId = entity.UserId,
                UserName = entity.User?.FullName,
                OrderItems = entity.OrderItems.Select(oi => new OrderItemDTO
                {
                    Id = oi.Id,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice,
                    Size = oi.Size,
                    Color = oi.Color,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name,
                    ProductImageUrl = oi.Product?.ImageUrl
                }).ToList(),
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
