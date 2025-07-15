using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Services;

// ����û��Area�������Ӧ�ú���[area]ģ��
[MiniController("/api/[area]/[controller]")]
public class CategoryEndpoint
{
    [HttpGet]
    public static IResult GetCategories()
    {
        return Results.Ok("��ȡ���з��� - ·��: /api/category (area������)");
    }

    [HttpPost]
    public static IResult CreateCategory()
    {
        return Results.Ok("�������� - ·��: /api/category");
    }
}