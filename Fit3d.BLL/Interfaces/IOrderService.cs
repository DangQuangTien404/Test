using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fit3d.BLL.DTOs;
using FIt3d.DAL.Common;

namespace Fit3d.BLL.Interfaces
{
    public interface IOrderService
    {
        Task<ICollection<OrderDTO>> GetAllAsync();
        Task<PagingResponse<OrderDTO>> GetPagingAsync(int page, int size, Guid? userId = null, int? status = null);
        Task<OrderDTO?> GetByIdAsync(Guid id);
        Task<OrderDTO> CreateAsync(CreateOrderDTO createDto);
        Task<OrderDTO?> UpdateAsync(Guid id, UpdateOrderDTO updateDto);
        Task<bool> DeleteAsync(Guid id);
    }
}
