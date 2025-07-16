using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Controllers;

// 示例：非静态类，支持依赖注入的控制器
// 注意：MiniController的生命周期固定为Transient
[MiniController("/api/[controller]")]
public class UserTestController
{
    private readonly ILogger<UserTestController> _logger;

    // 支持构造函数注入
    public UserTestController(ILogger<UserTestController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IResult GetUsers([FromServices] TestDiService testDiService)
    {
        testDiService.TestMethod();
        _logger.LogInformation("获取所有用户");
        return Results.Ok("获取所有用户 - 路由: /api/user");
    }

    [HttpGet("{id}")]
    public IResult GetUser(int id)
    {
        _logger.LogInformation("获取用户 {UserId}", id);
        return Results.Ok($"获取用户 {id} - 路由: /api/user/{id}");
    }

    [HttpPost]
    public IResult CreateUser()
    {
        _logger.LogInformation("创建用户");
        return Results.Ok("创建用户 - 路由: /api/user");
    }

    [HttpGet("search")]
    public IResult SearchUsers(string keyword)
    {
        _logger.LogInformation("搜索用户: {Keyword}", keyword);
        return Results.Ok($"搜索用户: {keyword} - 路由: /api/user/search");
    }
}