using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Controllers;

// ʾ�����Ǿ�̬�֧࣬������ע��Ŀ�����
// ע�⣺MiniController���������ڹ̶�ΪTransient
[MiniController("/api/[controller]")]
public class UserTestController
{
    private readonly ILogger<UserTestController> _logger;

    // ֧�ֹ��캯��ע��
    public UserTestController(ILogger<UserTestController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IResult GetUsers([FromServices] TestDiService testDiService)
    {
        testDiService.TestMethod();
        _logger.LogInformation("��ȡ�����û�");
        return Results.Ok("��ȡ�����û� - ·��: /api/user");
    }

    [HttpGet("{id}")]
    public IResult GetUser(int id)
    {
        _logger.LogInformation("��ȡ�û� {UserId}", id);
        return Results.Ok($"��ȡ�û� {id} - ·��: /api/user/{id}");
    }

    [HttpPost]
    public IResult CreateUser()
    {
        _logger.LogInformation("�����û�");
        return Results.Ok("�����û� - ·��: /api/user");
    }

    [HttpGet("search")]
    public IResult SearchUsers(string keyword)
    {
        _logger.LogInformation("�����û�: {Keyword}", keyword);
        return Results.Ok($"�����û�: {keyword} - ·��: /api/user/search");
    }
}