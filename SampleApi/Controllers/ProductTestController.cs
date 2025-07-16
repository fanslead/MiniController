using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Controllers;

// 示例：带参数的非静态控制器，支持依赖注入
// 注意：MiniController的生命周期固定为Transient
[MiniController("/api/[controller]")]
public class ProductTestController
{
    private readonly ILogger<ProductTestController> _logger;

    public ProductTestController(ILogger<ProductTestController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IResult GetProducts()
    {
        _logger.LogInformation("获取所有产品");
        return Results.Ok(new { Products = new[] { "产品1", "产品2", "产品3" } });
    }

    [HttpGet("{id:int}")]
    public IResult GetProduct(int id)
    {
        _logger.LogInformation("获取产品 {ProductId}", id);
        return Results.Ok(new { Id = id, Name = $"产品{id}" });
    }

    [HttpPost]
    public IResult CreateProduct([FromBody] CreateProductRequest request)
    {
        _logger.LogInformation("创建产品: {ProductName}", request.Name);
        return Results.Created($"/api/product/{Random.Shared.Next()}", 
            new { Id = Random.Shared.Next(), Name = request.Name });
    }

    [HttpGet("search")]
    public IResult SearchProducts([FromQuery] string keyword, [FromQuery] int page = 1)
    {
        _logger.LogInformation("搜索产品: {Keyword}, 页码: {Page}", keyword, page);
        return Results.Ok(new { 
            Keyword = keyword, 
            Page = page, 
            Results = new[] { $"搜索结果: {keyword}" } 
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IResult> UpdateProductAsync(int id, [FromBody] UpdateProductRequest request)
    {
        _logger.LogInformation("更新产品 {ProductId}: {ProductName}", id, request.Name);
        
        // 模拟异步操作
        await Task.Delay(100);
        
        return Results.Ok(new { Id = id, Name = request.Name, Updated = DateTime.Now });
    }
}

public record CreateProductRequest(string Name, decimal Price);
public record UpdateProductRequest(string Name, decimal Price);