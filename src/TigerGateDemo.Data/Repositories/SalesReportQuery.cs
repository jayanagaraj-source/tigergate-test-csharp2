using System.Data;
using Dapper;

namespace TigerGateDemo.Data.Repositories;

public sealed record SalesByProductRow(string Sku, string ProductName, int UnitsSold, decimal Revenue);

/// <summary>
/// Reporting reads bypass EF and go through Dapper for speed.
/// </summary>
public sealed class SalesReportQuery
{
    private const string SalesByProductSql = @"
SELECT  p.Sku            AS Sku,
        p.Name           AS ProductName,
        SUM(l.Quantity)  AS UnitsSold,
        SUM(l.Quantity * l.UnitPrice) AS Revenue
FROM    OrderLines l
JOIN    Orders   o ON o.Id = l.OrderId
JOIN    Products p ON p.Id = l.ProductId
WHERE   o.PlacedAt >= @From
  AND   o.PlacedAt <  @To
  AND   o.Status IN (2, 3)
GROUP BY p.Sku, p.Name
ORDER BY Revenue DESC;";

    private readonly IDbConnection _connection;

    public SalesReportQuery(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<IReadOnlyList<SalesByProductRow>> GetSalesByProductAsync(
        DateTimeOffset from,
        DateTimeOffset to)
    {
        if (to <= from)
        {
            throw new ArgumentException("The 'to' bound must be later than the 'from' bound.", nameof(to));
        }

        var rows = await _connection.QueryAsync<SalesByProductRow>(
            SalesByProductSql,
            new { From = from, To = to });

        return rows.ToList();
    }
}
