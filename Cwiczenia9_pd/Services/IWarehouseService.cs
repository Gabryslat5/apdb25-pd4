using Cwiczenia9_pd.Models.DTOs;

namespace Cwiczenia9_pd.Services;

public interface IWarehouseService
{
    Task<string> AddProductAsync(ProductWarehouseDTO dto, CancellationToken cancellationToken);
    //Task<string> AddProductWithProcedureAsync(ProductWarehouseDTO dto, CancellationToken cancellationToken);
    
    Task<int> AddProductViaProcedureAsync(ProductWarehouseDTO dto, CancellationToken cancellationToken);
}