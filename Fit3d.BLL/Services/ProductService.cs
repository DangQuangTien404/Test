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
    public class ProductService : IProductService
    {
        private readonly IGenericRepository<Product> _productRepository;
        private readonly IGenericRepository<ProductColor> _colorRepository;
        private readonly IGenericRepository<ProductSize> _sizeRepository;
        private readonly IGenericRepository<Category> _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ProductService(
            IGenericRepository<Product> productRepository,
            IGenericRepository<ProductColor> colorRepository,
            IGenericRepository<ProductSize> sizeRepository,
            IGenericRepository<Category> categoryRepository,
            IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _colorRepository = colorRepository;
            _sizeRepository = sizeRepository;
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ICollection<ProductDTO>> GetAllAsync()
        {
            var entities = await _productRepository.GetListAsync(
                predicate: x => !x.IsDeleted,
                include: x => x.Include(p => p.Category).Include(p => p.ProductColors).Include(p => p.ProductSizes)
            );
            return entities.Select(ToDTO).ToList();
        }

        public async Task<PagingResponse<ProductDTO>> GetPagingAsync(int page, int size, string? search = null, Guid? categoryId = null)
        {
            var result = await _productRepository.GetPagingListSelectedAsync(
                selector: x => ToDTO(x),
                predicate: x => !x.IsDeleted &&
                                (string.IsNullOrEmpty(search) || x.Name.Contains(search)) &&
                                (!categoryId.HasValue || x.CategoryId == categoryId),
                orderBy: x => x.OrderByDescending(p => p.CreatedAt),
                include: x => x.Include(p => p.Category).Include(p => p.ProductColors).Include(p => p.ProductSizes),
                page: page,
                size: size
            );
            return result;
        }

        public async Task<ProductDTO?> GetByIdAsync(Guid id)
        {
            var entity = await _productRepository.SingleOrDefaultAsync(
                predicate: x => x.Id == id && !x.IsDeleted,
                include: x => x.Include(p => p.Category).Include(p => p.ProductColors).Include(p => p.ProductSizes)
            );
            return entity == null ? null : ToDTO(entity);
        }

        public async Task<ProductDTO> CreateAsync(CreateProductDTO createDto)
        {
            var entity = new Product
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Description = createDto.Description,
                Price = createDto.Price,
                SalePrice = createDto.SalePrice,
                SKU = createDto.SKU,
                Brand = createDto.Brand,
                ImageUrl = createDto.ImageUrl,
                StockQuantity = createDto.StockQuantity,
                IsActive = createDto.IsActive,
                IsFeatured = createDto.IsFeatured,
                CategoryId = createDto.CategoryId,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var color in createDto.ProductColors)
            {
                entity.ProductColors.Add(new ProductColor
                {
                    Id = Guid.NewGuid(),
                    ColorName = color.ColorName,
                    ColorCode = color.ColorCode,
                    ImageUrl = color.ImageUrl,
                    StockQuantity = color.StockQuantity,
                    CreatedAt = DateTime.UtcNow
                });
            }

            foreach (var size in createDto.ProductSizes)
            {
                entity.ProductSizes.Add(new ProductSize
                {
                    Id = Guid.NewGuid(),
                    Size = size.Size,
                    StockQuantity = size.StockQuantity,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _productRepository.InsertAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            // Fetch again to get Category name if needed, or just construct DTO
            // For simplicity, constructing DTO, but CategoryName might be missing.
            // Let's fetch the category to be nice
            var category = await _categoryRepository.GetByIdAsync(entity.CategoryId);
            if (category != null) entity.Category = category;

            return ToDTO(entity);
        }

        public async Task<ProductDTO?> UpdateAsync(Guid id, UpdateProductDTO updateDto)
        {
            var entity = await _productRepository.SingleOrDefaultAsync(
                predicate: x => x.Id == id && !x.IsDeleted,
                include: x => x.Include(p => p.Category).Include(p => p.ProductColors).Include(p => p.ProductSizes)
            );

            if (entity == null) return null;

            entity.Name = updateDto.Name;
            entity.Description = updateDto.Description;
            entity.Price = updateDto.Price;
            entity.SalePrice = updateDto.SalePrice;
            entity.SKU = updateDto.SKU;
            entity.Brand = updateDto.Brand;
            entity.ImageUrl = updateDto.ImageUrl;
            entity.StockQuantity = updateDto.StockQuantity;
            entity.IsActive = updateDto.IsActive;
            entity.IsFeatured = updateDto.IsFeatured;
            entity.CategoryId = updateDto.CategoryId;
            entity.UpdatedAt = DateTime.UtcNow;

            _productRepository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            // Refresh category reference if changed
            if (entity.Category == null || entity.CategoryId != entity.Category.Id)
            {
                 var category = await _categoryRepository.GetByIdAsync(entity.CategoryId);
                 if (category != null) entity.Category = category;
            }

            return ToDTO(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _productRepository.GetByIdAsync(id);
            if (entity == null || entity.IsDeleted) return false;

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            _productRepository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<ProductColorDTO?> AddColorAsync(Guid productId, CreateProductColorDTO colorDto)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null || product.IsDeleted) return null;

            var color = new ProductColor
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                ColorName = colorDto.ColorName,
                ColorCode = colorDto.ColorCode,
                ImageUrl = colorDto.ImageUrl,
                StockQuantity = colorDto.StockQuantity,
                CreatedAt = DateTime.UtcNow
            };

            await _colorRepository.InsertAsync(color);
            await _unitOfWork.SaveChangesAsync();

            return new ProductColorDTO
            {
                Id = color.Id,
                ColorName = color.ColorName,
                ColorCode = color.ColorCode,
                ImageUrl = color.ImageUrl,
                StockQuantity = color.StockQuantity
            };
        }

        public async Task<bool> DeleteColorAsync(Guid colorId)
        {
            var color = await _colorRepository.GetByIdAsync(colorId);
            if (color == null) return false;

            // Hard delete for sub-resource usually, or soft delete if BaseEntity
            // ProductColor is BaseEntity, so soft delete
            if (color.IsDeleted) return false;

            color.IsDeleted = true;
            color.UpdatedAt = DateTime.UtcNow;
            _colorRepository.UpdateAsync(color);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<ProductSizeDTO?> AddSizeAsync(Guid productId, CreateProductSizeDTO sizeDto)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null || product.IsDeleted) return null;

            var size = new ProductSize
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                Size = sizeDto.Size,
                StockQuantity = sizeDto.StockQuantity,
                CreatedAt = DateTime.UtcNow
            };

            await _sizeRepository.InsertAsync(size);
            await _unitOfWork.SaveChangesAsync();

            return new ProductSizeDTO
            {
                Id = size.Id,
                Size = size.Size,
                StockQuantity = size.StockQuantity
            };
        }

        public async Task<bool> DeleteSizeAsync(Guid sizeId)
        {
            var size = await _sizeRepository.GetByIdAsync(sizeId);
            if (size == null) return false;

            if (size.IsDeleted) return false;

            size.IsDeleted = true;
            size.UpdatedAt = DateTime.UtcNow;
            _sizeRepository.UpdateAsync(size);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static ProductDTO ToDTO(Product entity)
        {
            return new ProductDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                Price = entity.Price,
                SalePrice = entity.SalePrice,
                SKU = entity.SKU,
                Brand = entity.Brand,
                ImageUrl = entity.ImageUrl,
                StockQuantity = entity.StockQuantity,
                IsActive = entity.IsActive,
                IsFeatured = entity.IsFeatured,
                CategoryId = entity.CategoryId,
                CategoryName = entity.Category?.Name,
                ProductColors = entity.ProductColors
                    .Where(c => !c.IsDeleted)
                    .Select(c => new ProductColorDTO
                    {
                        Id = c.Id,
                        ColorName = c.ColorName,
                        ColorCode = c.ColorCode,
                        ImageUrl = c.ImageUrl,
                        StockQuantity = c.StockQuantity
                    }).ToList(),
                ProductSizes = entity.ProductSizes
                    .Where(s => !s.IsDeleted)
                    .Select(s => new ProductSizeDTO
                    {
                        Id = s.Id,
                        Size = s.Size,
                        StockQuantity = s.StockQuantity
                    }).ToList(),
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
