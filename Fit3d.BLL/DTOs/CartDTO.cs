using System;
using System.ComponentModel.DataAnnotations;

namespace Fit3d.BLL.DTOs
{
    public class CartItemDTO
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal ProductPrice { get; set; }
        public string? ProductImageUrl { get; set; }
        public int Quantity { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public decimal TotalPrice => ProductPrice * Quantity;
    }

    public class CreateCartItemDTO
    {
        [Required]
        public Guid UserId { get; set; }
        [Required]
        public Guid ProductId { get; set; }
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
        public string? Size { get; set; }
        public string? Color { get; set; }
    }

    public class UpdateCartItemDTO
    {
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
