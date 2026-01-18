using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fit3d.BLL.DTOs
{
    public class ProductColorDTO
    {
        public Guid Id { get; set; }
        public string ColorName { get; set; } = string.Empty;
        public string? ColorCode { get; set; }
        public string? ImageUrl { get; set; }
        public int StockQuantity { get; set; }
    }

    public class CreateProductColorDTO
    {
        [Required]
        public string ColorName { get; set; } = string.Empty;
        public string? ColorCode { get; set; }
        public string? ImageUrl { get; set; }
        public int StockQuantity { get; set; }
    }

    public class ProductSizeDTO
    {
        public Guid Id { get; set; }
        public string Size { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
    }

    public class CreateProductSizeDTO
    {
        [Required]
        public string Size { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
    }

    public class ProductDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public string? SKU { get; set; }
        public string? Brand { get; set; }
        public string? ImageUrl { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public Guid CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public ICollection<ProductColorDTO> ProductColors { get; set; } = new List<ProductColorDTO>();
        public ICollection<ProductSizeDTO> ProductSizes { get; set; } = new List<ProductSizeDTO>();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateProductDTO
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        public decimal? SalePrice { get; set; }

        [MaxLength(100)]
        public string? SKU { get; set; }

        [MaxLength(100)]
        public string? Brand { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public int StockQuantity { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public bool IsFeatured { get; set; } = false;

        [Required]
        public Guid CategoryId { get; set; }

        public List<CreateProductColorDTO> ProductColors { get; set; } = new List<CreateProductColorDTO>();
        public List<CreateProductSizeDTO> ProductSizes { get; set; } = new List<CreateProductSizeDTO>();
    }

    public class UpdateProductDTO
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        public decimal? SalePrice { get; set; }

        [MaxLength(100)]
        public string? SKU { get; set; }

        [MaxLength(100)]
        public string? Brand { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public int StockQuantity { get; set; }

        public bool IsActive { get; set; }

        public bool IsFeatured { get; set; }

        [Required]
        public Guid CategoryId { get; set; }
    }
}
