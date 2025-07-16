using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Services;

// ʾ����Transient�������ڵķ�����������̶��������ڣ�
// ע�⣺����MiniController��ʹ��Transient�������ڣ�ÿ�����󴴽���ʵ��
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
            Note = "ÿ�����󶼻ῴ���µ�InstanceId����Ϊ������ʹ��Transient��������"
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
            Message = "�������ѵ���",
            Note = "CallCount�ڵ�����������Ч���´����������"
        });
    }
}