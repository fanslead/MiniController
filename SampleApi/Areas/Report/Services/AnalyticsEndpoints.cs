using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace SampleApi.Areas.Report.Services;

[Area("Report")]
[MiniController("/api/v2/[area]/[controller]/[action]")]
public class AnalyticsEndpoints
{
    // 测试完整的模板解析：area + controller + action
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

    // 测试复杂的方法名处理
    public static IResult GetDailyRevenueAnalysis()
    {
        return Results.Ok("获取日收入分析 - 路由: /api/v2/report/analytics/daily-revenue-analysis");
    }
}