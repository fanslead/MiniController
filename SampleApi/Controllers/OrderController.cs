using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Controllers;

[MiniController("/api/[controller]")]
public class OrderController
{
    [HttpGet]
    public static IResult GetOrders()
    {
        return Results.Ok("获取所有订单 - 路由: /api/order");
    }

    [HttpGet("{id}")]
    public static IResult GetOrder(int id)
    {
        return Results.Ok($"获取订单 {id} - 路由: /api/order/{{id}}");
    }

    [HttpPost]
    public static IResult CreateOrder()
    {
        return Results.Ok("创建订单 - 路由: /api/order");
    }

    [HttpGet("search")]
    public static IResult SearchOrders(string keyword)
    {
        return Results.Ok($"搜索订单: {keyword} - 路由: /api/order/search");
    }
}