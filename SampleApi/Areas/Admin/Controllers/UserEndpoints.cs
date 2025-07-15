using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Areas.Admin.Controllers;

[Area("Admin")]
[MiniController("/api/[area]/[controller]")]
public class UserEndpoints
{
    [HttpGet]
    public static IResult GetUsers()
    {
        return Results.Ok("��ȡ�����û� - ·��: /api/admin/user");
    }

    [HttpGet("{id}")]
    public static IResult GetUser(int id)
    {
        return Results.Ok($"��ȡ�û� {id} - ·��: /api/admin/user/{{id}}");
    }

    [HttpPost]
    public static IResult CreateUser()
    {
        return Results.Ok("�����û� - ·��: /api/admin/user");
    }

    [HttpPut("{id}")]
    public static IResult UpdateUser(int id)
    {
        return Results.Ok($"�����û� {id} - ·��: /api/admin/user/{{id}}");
    }

    [HttpDelete("{id}")]
    public static IResult DeleteUser(int id)
    {
        return Results.Ok($"ɾ���û� {id} - ·��: /api/admin/user/{{id}}");
    }
}