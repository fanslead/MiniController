using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Controllers;

[MiniController("/api/v1/[controller]/[action]")]
public class ProductService
{
    // 测试自动推断HTTP方法和[action]模板
    public static IResult GetList()
    {
        return Results.Ok("获取产品列表 - 路由: /api/v1/product/list");
    }

    public static IResult GetDetails(int id)
    {
        return Results.Ok($"获取产品详情 {id} - 路由: /api/v1/product/details");
    }

    public static IResult PostCreate()
    {
        return Results.Ok("创建产品 - 路由: /api/v1/product/create");
    }

    public static IResult PutUpdate(int id)
    {
        return Results.Ok($"更新产品 {id} - 路由: /api/v1/product/update");
    }

    public static IResult DeleteRemove(int id)
    {
        return Results.Ok($"删除产品 {id} - 路由: /api/v1/product/remove");
    }

    // 测试复杂动作名称
    public static IResult GetSearchByCategory(string category)
    {
        return Results.Ok($"按分类搜索产品: {category} - 路由: /api/v1/product/search-by-category");
    }

    public static IResult PostBatchImport()
    {
        return Results.Ok("批量导入产品 - 路由: /api/v1/product/batch-import");
    }
}