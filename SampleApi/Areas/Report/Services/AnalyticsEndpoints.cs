using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Areas.Report.Services;

[Area("Report")]
[MiniController("/api/v2/[area]/[controller]/[action]")]
public class AnalyticsEndpoints
{
    // 对于包含[action]的路由模板，每个方法会自动生成唯一的action路径
    public static IResult GetSalesReport()
    {
        return Results.Ok("获取销售报告 - 路由: /api/v2/report/analytics/sales-report");
    }

    public static IResult GetUserActivity()
    {
        return Results.Ok("获取用户活动 - 路由: /api/v2/report/analytics/user-activity");
    }

    public static IResult PostGenerateReport()
    {
        return Results.Ok("生成报告 - 路由: /api/v2/report/analytics/generate-report");
    }

    // 这个方法会生成唯一的action路径
    public static IResult GetDailyRevenueAnalysis()
    {
        return Results.Ok("获取日收入分析 - 路由: /api/v2/report/analytics/daily-revenue-analysis");
    }

    // 添加更多方法来测试路由解析
    [HttpGet("weekly")]
    public static IResult GetWeeklyReport()
    {
        return Results.Ok("获取周报告 - 路由: /api/v2/report/analytics/weekly");
    }

    [HttpPost("export/{format}")]
    public static IResult PostExportReport(string format)
    {
        return Results.Ok($"导出报告为{format}格式 - 路由: /api/v2/report/analytics/export/{format}");
    }
}