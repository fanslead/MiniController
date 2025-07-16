using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Controllers;

// ʾ�����������ķǾ�̬��������֧������ע��
// ע�⣺MiniController���������ڹ̶�ΪTransient
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
        _logger.LogInformation("��ȡ���в�Ʒ");
        return Results.Ok(new { Products = new[] { "��Ʒ1", "��Ʒ2", "��Ʒ3" } });
    }

    [HttpGet("{id:int}")]
    public IResult GetProduct(int id)
    {
        _logger.LogInformation("��ȡ��Ʒ {ProductId}", id);
        return Results.Ok(new { Id = id, Name = $"��Ʒ{id}" });
    }

    [HttpPost]
    public IResult CreateProduct([FromBody] CreateProductRequest request)
    {
        _logger.LogInformation("������Ʒ: {ProductName}", request.Name);
        return Results.Created($"/api/product/{Random.Shared.Next()}", 
            new { Id = Random.Shared.Next(), Name = request.Name });
    }

    [HttpGet("search")]
    public IResult SearchProducts([FromQuery] string keyword, [FromQuery] int page = 1)
    {
        _logger.LogInformation("������Ʒ: {Keyword}, ҳ��: {Page}", keyword, page);
        return Results.Ok(new { 
            Keyword = keyword, 
            Page = page, 
            Results = new[] { $"�������: {keyword}" } 
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IResult> UpdateProductAsync(int id, [FromBody] UpdateProductRequest request)
    {
        _logger.LogInformation("���²�Ʒ {ProductId}: {ProductName}", id, request.Name);
        
        // ģ���첽����
        await Task.Delay(100);
        
        return Results.Ok(new { Id = id, Name = request.Name, Updated = DateTime.Now });
    }
}

public record CreateProductRequest(string Name, decimal Price);
public record UpdateProductRequest(string Name, decimal Price);