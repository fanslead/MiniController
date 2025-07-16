using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Services;

// 示例：Transient生命周期的服务控制器（固定生命周期）
// 注意：所有MiniController都使用Transient生命周期
[MiniController("/api/[controller]")]
public class HealthController
{
    private readonly DateTime _startTime = DateTime.UtcNow;
    private static int _requestCount = 0;

    [HttpGet]
    public IResult GetHealth()
    {
        Interlocked.Increment(ref _requestCount);
        
        return Results.Ok(new 
        { 
            Status = "Healthy",
            StartTime = _startTime,
            Uptime = DateTime.UtcNow - _startTime,
            RequestCount = _requestCount,
            InstanceCreatedAt = _startTime,
            Note = "每次请求都会创建新的控制器实例 (Transient)"
        });
    }

    [HttpGet("ping")]
    public IResult Ping()
    {
        return Results.Ok(new { Message = "Pong", Timestamp = DateTime.UtcNow });
    }
}