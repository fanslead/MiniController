using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Controllers;

// ����Ĭ��·�����ɣ�û��ָ��·��ģ�壩
[MiniController]
public class NotificationEndpoints
{
    [HttpGet]
    public static IResult GetNotifications()
    {
        return Results.Ok("��ȡ֪ͨ - Ĭ��·��: /api/notification");
    }

    [HttpPost]
    public static IResult SendNotification()
    {
        return Results.Ok("����֪ͨ - Ĭ��·��: /api/notification");
    }
}

// �����Զ���·�ɣ���ʹ��ģ�壩
[MiniController("/custom/path/messages")]
public class MessageController
{
    [HttpGet]
    public static IResult GetMessages()
    {
        return Results.Ok("��ȡ��Ϣ - �Զ���·��: /custom/path/messages");
    }

    [HttpPost]
    public static IResult SendMessage()
    {
        return Results.Ok("������Ϣ - �Զ���·��: /custom/path/messages");
    }
}