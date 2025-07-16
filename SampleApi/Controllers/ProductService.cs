using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Controllers;

[MiniController("/api/v1/[controller]/[action]")]
public class ProductService
{
    // ��Щ�������Զ��ӷ������ƶϳ���ͬ��action·��
    public static IResult GetList()
    {
        return Results.Ok("��ȡ��Ʒ�б� - ·��: /api/v1/product/list");
    }

    public static IResult GetDetails(int id)
    {
        return Results.Ok($"��ȡ��Ʒ���� {id} - ·��: /api/v1/product/details");
    }

    public static IResult PostCreate()
    {
        return Results.Ok("������Ʒ - ·��: /api/v1/product/create");
    }

    public static IResult PutUpdate(int id)
    {
        return Results.Ok($"���²�Ʒ {id} - ·��: /api/v1/product/update");
    }

    public static IResult DeleteRemove(int id)
    {
        return Results.Ok($"ɾ����Ʒ {id} - ·��: /api/v1/product/remove");
    }

    // �������������Ψһ��action·��
    public static IResult GetSearchByCategory(string category)
    {
        return Results.Ok($"������������Ʒ: {category} - ·��: /api/v1/product/search-by-category");
    }

    public static IResult PostBatchImport()
    {
        return Results.Ok("���������Ʒ - ·��: /api/v1/product/batch-import");
    }

    // �����ȷ��·��ģ��������
    [HttpGet("featured")]
    public static IResult GetFeaturedProducts()
    {
        return Results.Ok("��ȡ�Ƽ���Ʒ - ·��: /api/v1/product/featured");
    }

    [HttpPost("validate")]
    public static IResult PostValidateProduct([FromBody] object productData)
    {
        return Results.Ok("��֤��Ʒ���� - ·��: /api/v1/product/validate");
    }
}