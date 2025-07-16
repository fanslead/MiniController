using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Services;

// 示例：Transient生命周期的服务控制器（固定生命周期）
// 注意：所有MiniController都使用Transient生命周期，每次请求创建新实例
[MiniController("/api/[controller]")]
public class CounterController
{
    private readonly Guid _instanceId = Guid.NewGuid();
    private int _callCount = 0;

    [HttpGet]
    public IResult GetCounter()
    {
        _callCount++;
        
        return Results.Ok(new 
        { 
            InstanceId = _instanceId,
            CallCount = _callCount,
            Timestamp = DateTime.UtcNow,
            Note = "每次请求都会看到新的InstanceId，因为控制器使用Transient生命周期"
        });
    }

    [HttpPost("increment")]
    public IResult IncrementCounter()
    {
        _callCount++;
        
        return Results.Ok(new 
        { 
            InstanceId = _instanceId,
            NewCount = _callCount,
            Message = "计数器已递增",
            Note = "CallCount在单次请求内有效，下次请求会重置"
        });
    }
}