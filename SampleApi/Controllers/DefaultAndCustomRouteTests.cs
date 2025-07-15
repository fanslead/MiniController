using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Controllers;

// 测试默认路由生成（没有指定路由模板）
[MiniController]
public class NotificationEndpoints
{
    [HttpGet]
    public static IResult GetNotifications()
    {
        return Results.Ok("获取通知 - 默认路由: /api/notification");
    }

    [HttpPost]
    public static IResult SendNotification()
    {
        return Results.Ok("发送通知 - 默认路由: /api/notification");
    }
}

// 测试自定义路由（不使用模板）
[MiniController("/custom/path/messages")]
public class MessageController
{
    [HttpGet]
    public static IResult GetMessages()
    {
        return Results.Ok("获取消息 - 自定义路由: /custom/path/messages");
    }

    [HttpPost]
    public static IResult SendMessage()
    {
        return Results.Ok("发送消息 - 自定义路由: /custom/path/messages");
    }
}