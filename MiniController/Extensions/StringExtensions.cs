using Microsoft.CodeAnalysis;
using MiniController.Helpers;
using System.Collections.Concurrent;
using System.Text;

namespace MiniController.Extensions;

public static class StringExtensions
{
    // 优化缓存以避免重复生成，减少字符串拼接
    private static readonly ConcurrentDictionary<string, string> RouteNameCache = new();
    private static readonly ConcurrentDictionary<(string methodName, string prefixes), string> KebabCaseCache = new();

    /// <summary>
    /// 从类名获取或添加路由，支持模板语法解析
    /// </summary>
    /// <param name="className">类名</param>
    /// <param name="classSymbol">类符号（可选，用于模板解析）</param>
    /// <param name="routeTemplate">路由模板（可选）</param>
    /// <returns>解析后的路由</returns>
    public static string GetOrAddRouteFromClassName(string className, ISymbol? classSymbol = null, string? routeTemplate = null)
    {
        // 如果提供了路由模板且包含占位符，则使用模板解析
        if (!string.IsNullOrEmpty(routeTemplate) && classSymbol != null && ContainsTemplatePlaceholders(routeTemplate!))
        {
            return RouteTemplateResolver.ResolveControllerTemplate(routeTemplate!, classSymbol, className);
        }

        // 如果提供了明确的路由模板且不包含占位符，直接使用
        if (!string.IsNullOrEmpty(routeTemplate))
        {
            return routeTemplate!.StartsWith("/") ? routeTemplate : "/" + routeTemplate;
        }

        // 使用默认逻辑
        return RouteNameCache.GetOrAdd(className, static name =>
        {
            var processedName = name.ToLowerInvariant()
                .Replace("service", "")
                .Replace("controller", "")
                .Replace("endpoint", "")
                .Replace("endpoints", "");
            return $"/api/{processedName}";
        });
    }

    /// <summary>
    /// 检查路由模板是否包含占位符
    /// </summary>
    private static bool ContainsTemplatePlaceholders(string template)
    {
        return template.Contains("[area]") ||
               template.Contains("[controller]") ||
               template.Contains("[action]");
    }

    public static string InferRouteFromMethodName(string methodName, string[] prefixes)
    {
        if (string.IsNullOrEmpty(methodName) || prefixes == null || prefixes.Length == 0)
            return string.Empty;

        var cacheKey = (methodName, string.Join(",", prefixes));
        return KebabCaseCache.GetOrAdd(cacheKey, static key =>
        {
            var (name, prefixString) = key;
            var prefixArray = prefixString.Split(',');

            string routeName = name;

            foreach (var prefix in prefixArray)
            {
                if (routeName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    routeName = routeName.Substring(prefix.Length);
                    break;
                }
            }

            if (string.IsNullOrEmpty(routeName))
                return string.Empty;

            return ConvertToKebabCase(routeName);
        });
    }

    public static string InferRouteFromMethodName(string methodName, string prefix)
    {
        return InferRouteFromMethodName(methodName, [prefix]);
    }

    public static string ConvertToKebabCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // 移除 "Async" 后缀（如果存在）
        const string asyncSuffix = "Async";
        if (input.EndsWith(asyncSuffix, StringComparison.Ordinal))
        {
            input = input.Substring(0, input.Length - asyncSuffix.Length);
        }

        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // 预估 StringBuilder 容量以减少重分配
        var estimatedLength = input.Length + (input.Length / 3); // 约增加 33% 的连字符
        var result = new StringBuilder(estimatedLength);

        // 第一个字符转换为小写
        result.Append(char.ToLowerInvariant(input[0]));

        // 处理剩余字符
        for (int i = 1; i < input.Length; i++)
        {
            var currentChar = input[i];
            if (char.IsUpper(currentChar))
            {
                result.Append('-');
                result.Append(char.ToLowerInvariant(currentChar));
            }
            else
            {
                result.Append(currentChar);
            }
        }

        return result.ToString();
    }
}