using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Controllers;

// ���Ժ�׺�Ƴ�����
[MiniController("/api/[controller]")]
public class AccountService
{
    [HttpGet]
    public static IResult GetAccounts()
    {
        return Results.Ok("��ȡ�˻� - ·��: /api/account (Service��׺���Ƴ�)");
    }
}

[MiniController("/api/[controller]")]
public class PaymentController
{
    [HttpPost]
    public static IResult ProcessPayment()
    {
        return Results.Ok("����֧�� - ·��: /api/payment (Controller��׺���Ƴ�)");
    }
}

[MiniController("/api/[controller]")]
public class FileEndpoint
{
    [HttpPost("upload")]
    public static IResult UploadFile()
    {
        return Results.Ok("�ϴ��ļ� - ·��: /api/file/upload (Endpoint��׺���Ƴ�)");
    }
}

[MiniController("/api/[controller]")]
public class DataEndpoints
{
    [HttpGet]
    public static IResult GetData()
    {
        return Results.Ok("��ȡ���� - ·��: /api/data (Endpoints��׺���Ƴ�)");
    }
}