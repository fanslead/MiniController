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
        return Results.Ok("获取所有用户 - 路由: /api/admin/user");
    }

    [HttpGet("{id}")]
    public static IResult GetUser(int id)
    {
        return Results.Ok($"获取用户 {id} - 路由: /api/admin/user/{{id}}");
    }

    [HttpPost]
    public static IResult CreateUser()
    {
        return Results.Ok("创建用户 - 路由: /api/admin/user");
    }

    [HttpPut("{id}")]
    public static IResult UpdateUser(int id)
    {
        return Results.Ok($"更新用户 {id} - 路由: /api/admin/user/{{id}}");
    }

    [HttpDelete("{id}")]
    public static IResult DeleteUser(int id)
    {
        return Results.Ok($"删除用户 {id} - 路由: /api/admin/user/{{id}}");
    }
}