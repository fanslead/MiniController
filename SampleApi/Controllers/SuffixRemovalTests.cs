using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Controllers;

// 测试后缀移除功能
[MiniController("/api/[controller]")]
public class AccountService
{
    [HttpGet]
    public static IResult GetAccounts()
    {
        return Results.Ok("获取账户 - 路由: /api/account (Service后缀已移除)");
    }
}

[MiniController("/api/[controller]")]
public class PaymentController
{
    [HttpPost]
    public static IResult ProcessPayment()
    {
        return Results.Ok("处理支付 - 路由: /api/payment (Controller后缀已移除)");
    }
}

[MiniController("/api/[controller]")]
public class FileEndpoint
{
    [HttpPost("upload")]
    public static IResult UploadFile()
    {
        return Results.Ok("上传文件 - 路由: /api/file/upload (Endpoint后缀已移除)");
    }
}

[MiniController("/api/[controller]")]
public class DataEndpoints
{
    [HttpGet]
    public static IResult GetData()
    {
        return Results.Ok("获取数据 - 路由: /api/data (Endpoints后缀已移除)");
    }
}