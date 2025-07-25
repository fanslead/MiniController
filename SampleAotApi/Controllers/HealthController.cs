using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Controllers;

// ʾ����Transient�������ڵķ�����������̶��������ڣ�
// ע�⣺����MiniController��ʹ��Transient��������
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
"ÿ�����󶼻ᴴ���µĿ�����ʵ�� (Transient)"
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
