using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Areas.Report.Services;

[Area("Report")]
[MiniController("/api/v2/[area]/[controller]/[action]")]
public class AnalyticsEndpoints
{
    // ���ڰ���[action]��·��ģ�壬ÿ���������Զ�����Ψһ��action·��
    public static IResult GetSalesReport()
    {
        return Results.Ok("��ȡ���۱��� - ·��: /api/v2/report/analytics/sales-report");
    }

    public static IResult GetUserActivity()
    {
        return Results.Ok("��ȡ�û�� - ·��: /api/v2/report/analytics/user-activity");
    }

    public static IResult PostGenerateReport()
    {
        return Results.Ok("���ɱ��� - ·��: /api/v2/report/analytics/generate-report");
    }

    // �������������Ψһ��action·��
    public static IResult GetDailyRevenueAnalysis()
    {
        return Results.Ok("��ȡ��������� - ·��: /api/v2/report/analytics/daily-revenue-analysis");
    }

    // ��Ӹ��෽��������·�ɽ���
    [HttpGet("weekly")]
    public static IResult GetWeeklyReport()
    {
        return Results.Ok("��ȡ�ܱ��� - ·��: /api/v2/report/analytics/weekly");
    }

    [HttpPost("export/{format}")]
    public static IResult PostExportReport(string format)
    {
        return Results.Ok($"��������Ϊ{format}��ʽ - ·��: /api/v2/report/analytics/export/{format}");
    }
}