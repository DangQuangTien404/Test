using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FIt3d.DAL.Entities;

namespace Fit3d.BLL.DTOs
{
    public class OrderItemDTO
    {
        public Guid Id { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImageUrl { get; set; }
    }

    public class OrderDTO
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public OrderStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string? PaymentMethod { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public ICollection<OrderItemDTO> OrderItems { get; set; } = new List<OrderItemDTO>();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateOrderItemDTO
    {
        [Required]
        public Guid ProductId { get; set; }
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
    }

    public class CreateOrderDTO
    {
        public decimal? DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public string? PaymentMethod { get; set; }

        [Required]
        [MaxLength(500)]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ReceiverName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string ReceiverPhone { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Note { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public List<CreateOrderItemDTO> OrderItems { get; set; } = new List<CreateOrderItemDTO>();
    }

    public class UpdateOrderDTO
    {
        public OrderStatus? Status { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        [MaxLength(1000)]
        public string? Note { get; set; }
    }
}
