using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fit3d.BLL.DTOs;
using FIt3d.DAL.Common;

namespace Fit3d.BLL.Interfaces
{
    public interface IProductService
    {
        Task<ICollection<ProductDTO>> GetAllAsync();
        Task<PagingResponse<ProductDTO>> GetPagingAsync(int page, int size, string? search = null, Guid? categoryId = null);
        Task<ProductDTO?> GetByIdAsync(Guid id);
        Task<ProductDTO> CreateAsync(CreateProductDTO createDto);
        Task<ProductDTO?> UpdateAsync(Guid id, UpdateProductDTO updateDto);
        Task<bool> DeleteAsync(Guid id);

        // Sub-resources management
        Task<ProductColorDTO?> AddColorAsync(Guid productId, CreateProductColorDTO colorDto);
        Task<bool> DeleteColorAsync(Guid colorId);
        Task<ProductSizeDTO?> AddSizeAsync(Guid productId, CreateProductSizeDTO sizeDto);
        Task<bool> DeleteSizeAsync(Guid sizeId);
    }
}
