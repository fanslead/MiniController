using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Services;

// ʾ����Transient�������ڵķ�����������̶��������ڣ�
// ע�⣺����MiniController��ʹ��Transient��������
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
            Note = "ÿ�����󶼻ᴴ���µĿ�����ʵ�� (Transient)"
        });
    }

    [HttpGet("ping")]
    public IResult Ping()
    {
        return Results.Ok(new { Message = "Pong", Timestamp = DateTime.UtcNow });
    }
}