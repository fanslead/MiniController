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
        return Results.Ok("��ȡ���ж��� - ·��: /api/order");
    }

    [HttpGet("{id}")]
    public static IResult GetOrder(int id)
    {
        return Results.Ok($"��ȡ���� {id} - ·��: /api/order/{{id}}");
    }

    [HttpPost]
    public static IResult CreateOrder()
    {
        return Results.Ok("�������� - ·��: /api/order");
    }

    [HttpGet("search")]
    public static IResult SearchOrders(string keyword)
    {
        return Results.Ok($"��������: {keyword} - ·��: /api/order/search");
    }
}