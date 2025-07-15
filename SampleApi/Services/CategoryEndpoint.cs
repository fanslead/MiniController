using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Services;

// 测试没有Area的情况，应该忽略[area]模板
[MiniController("/api/[area]/[controller]")]
public class CategoryEndpoint
{
    [HttpGet]
    public static IResult GetCategories()
    {
        return Results.Ok("获取所有分类 - 路由: /api/category (area被忽略)");
    }

    [HttpPost]
    public static IResult CreateCategory()
    {
        return Results.Ok("创建分类 - 路由: /api/category");
    }
}