using Cwiczenia9_pd.Models.DTOs;
using Cwiczenia9_pd.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cwiczenia9_pd.Controllers;


[ApiController]
[Route("api/[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehouseController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddProductAsync(ProductWarehouseDTO dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _warehouseService.AddProductAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("procedure")]
    public async Task<IActionResult> AddProductByProcedure([FromBody] ProductWarehouseDTO dto, CancellationToken cancellationToken)
    {
        try
        {
            var newId = await _warehouseService.AddProductViaProcedureAsync(dto, cancellationToken);
            return Ok(new { Message = $"Inserted using procedure. New Product_Warehouse ID: {newId}" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

}
