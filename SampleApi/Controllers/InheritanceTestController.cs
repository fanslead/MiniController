using MiniController.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace SampleApi.Controllers;

/// <summary>
/// 基础控制器类，使用 MiniControllerAttribute
/// </summary>
[MiniController("/api/base")]
public class BaseController
{
    protected readonly ILogger _logger;

    public BaseController(ILogger logger)
    {
        _logger = logger;
    }

    [HttpGet("health")]
    public virtual IResult GetHealth()
    {
        _logger.LogInformation("Base health check");
        return Results.Ok("Base health check OK");
    }
}

/// <summary>
/// 继承的控制器类，应该能够自动检测到父类的 MiniControllerAttribute
/// </summary>
public class InheritedController : BaseController
{
    public InheritedController(ILogger<InheritedController> logger) : base(logger)
    {
    }

    [HttpGet("inherited")]
    public IResult GetInherited()
    {
        _logger.LogInformation("Inherited endpoint called");
        return Results.Ok("Inherited endpoint - Route: /api/base/inherited");
    }

    [HttpPost("create")]
    public IResult CreateSomething()
    {
        _logger.LogInformation("Creating something in inherited controller");
        return Results.Ok("Created in inherited controller");
    }

    // 重写父类方法
    [HttpGet("health")]
    public override IResult GetHealth()
    {
        _logger.LogInformation("Inherited health check");
        return Results.Ok("Inherited health check OK");
    }
}