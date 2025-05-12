using System.Data;
using Cwiczenia9_pd.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace Cwiczenia9_pd.Services;

public class WarehouseService : IWarehouseService
{
    private readonly string _connectionString = "Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True;Trust Server Certificate=True;";
    public async Task<string> AddProductAsync(ProductWarehouseDTO dto, CancellationToken cancellationToken)
    {
        if (dto.Amount <= 0)
            throw new Exception("Amount must be greater than 0");

        SqlConnection conn = new SqlConnection(_connectionString) ;
        await conn.OpenAsync(cancellationToken);
        
        using var tran = await conn.BeginTransactionAsync(cancellationToken);

    try
        {
        // Check if product exists
        var checkProductCmd = new SqlCommand("SELECT COUNT(*) FROM Product WHERE IdProduct = @IdProduct", conn, (SqlTransaction)tran);
        checkProductCmd.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
        int productExists = (int)(await checkProductCmd.ExecuteScalarAsync(cancellationToken));
        if (productExists == 0) throw new Exception("Product not found.");

        // Check if warehouse exists
        var checkWarehouseCmd = new SqlCommand("SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = @IdWarehouse", conn, (SqlTransaction)tran);
        checkWarehouseCmd.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
        int warehouseExists = (int)(await checkWarehouseCmd.ExecuteScalarAsync(cancellationToken));
        if (warehouseExists == 0) throw new Exception("Warehouse not found.");

        // Find a matching unfulfilled order
        var findOrderCmd = new SqlCommand(@"
            SELECT TOP 1 IdOrder, Amount FROM [Order]
            WHERE IdProduct = @IdProduct AND Amount >= @Amount AND CreatedAt < @CreatedAt
            ORDER BY CreatedAt", conn, (SqlTransaction)tran);

        findOrderCmd.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
        findOrderCmd.Parameters.AddWithValue("@Amount", dto.Amount);
        findOrderCmd.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);

        int orderId = 0;
        int orderAmount = 0;
        using (var reader = await findOrderCmd.ExecuteReaderAsync(cancellationToken))
        {
            if (!reader.HasRows)
            {
                await reader.CloseAsync();
                throw new Exception("No matching order found.");
            }

            await reader.ReadAsync(cancellationToken);
            orderId = reader.GetInt32(reader.GetOrdinal("IdOrder"));
            orderAmount = reader.GetInt32(reader.GetOrdinal("Amount"));
        }

        // Check if this order has already been fulfilled
        var checkOrderUsedCmd = new SqlCommand("SELECT COUNT(*) FROM Product_Warehouse WHERE IdOrder = @IdOrder", conn, (SqlTransaction)tran);
        checkOrderUsedCmd.Parameters.AddWithValue("@IdOrder", orderId);
        var count = (int)(await checkOrderUsedCmd.ExecuteScalarAsync(cancellationToken));
        if (count > 0)
        {
            throw new Exception("Order already fulfilled.");
        }

        // Update FulfilledAt on Order
        var updateOrderCmd = new SqlCommand("UPDATE [Order] SET FulfilledAt = GETDATE() WHERE IdOrder = @IdOrder", conn, (SqlTransaction)tran);
        updateOrderCmd.Parameters.AddWithValue("@IdOrder", orderId);
        await updateOrderCmd.ExecuteNonQueryAsync(cancellationToken);

        // Get product price
        var priceCmd = new SqlCommand("SELECT Price FROM Product WHERE IdProduct = @IdProduct", conn, (SqlTransaction)tran);
        priceCmd.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
        var unitPrice = (decimal)(await priceCmd.ExecuteScalarAsync(cancellationToken));
        var totalPrice = unitPrice * dto.Amount;
        var now = DateTime.Now;

        // Insert into Product_Warehouse
        var insertCmd = new SqlCommand(@"
            INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
            OUTPUT INSERTED.IdProductWarehouse
            VALUES(@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt)", conn, (SqlTransaction)tran);

        insertCmd.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
        insertCmd.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
        insertCmd.Parameters.AddWithValue("@IdOrder", orderId);
        insertCmd.Parameters.AddWithValue("@Amount", dto.Amount);
        insertCmd.Parameters.AddWithValue("@Price", totalPrice);
        insertCmd.Parameters.AddWithValue("@CreatedAt", now);

        var insertedId = (int)(await insertCmd.ExecuteScalarAsync(cancellationToken));

        await tran.CommitAsync(cancellationToken);
        return $"Inserted Product_Warehouse with Id: {insertedId}";
    }
    catch (Exception)
        { 
            await tran.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<int> AddProductViaProcedureAsync(ProductWarehouseDTO dto, CancellationToken cancellationToken)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        using var cmd = new SqlCommand("AddProductToWarehouse", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
        cmd.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
        cmd.Parameters.AddWithValue("@Amount", dto.Amount);
        cmd.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);

        try
        {
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }
        catch (SqlException ex)
        {
            throw new Exception($"SQL Error: {ex.Message}");
        }
    }
}