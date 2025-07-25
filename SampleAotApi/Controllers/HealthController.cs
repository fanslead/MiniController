using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Controllers;

// 示例：Transient生命周期的服务控制器（固定生命周期）
// 注意：所有MiniController都使用Transient生命周期
[MiniController("/api/[controller]")]
public class HealthController
{
    private readonly DateTime _startTime = DateTime.UtcNow;
    private static int _requestCount = 0;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HealthData))]
    public IResult GetHealth()
    {
        Interlocked.Increment(ref _requestCount);
        
        return Results.Ok(new HealthData(
"Healthy",
_startTime,
DateTime.UtcNow - _startTime,
_requestCount,
_startTime,
"每次请求都会创建新的控制器实例 (Transient)"
        ));
    }

    [HttpGet("ping")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PingData))]
    public IResult Ping()
    {
        return Results.Ok(new PingData("Pong", DateTime.UtcNow));
    }
}

public record HealthData(string Status, DateTime StartTime, TimeSpan Uptime, int RequestCount, DateTime InstanceCreatedAt, string Note);

public record PingData(string Message, DateTime Timestamp);
