using Microsoft.CodeAnalysis;
using System;
using System.Text;
using MiniController.Extensions;

namespace MiniController.Helpers;

/// <summary>
/// 路由模板解析器，支持 [area]、[controller]、[action] 占位符
/// </summary>
public static class RouteTemplateResolver
{
    /// <summary>
    /// 解析控制器级别的路由模板
    /// </summary>
    /// <param name="template">路由模板，如 "/api/[area]/[controller]"</param>
    /// <param name="classSymbol">类符号</param>
    /// <param name="className">类名</param>
    /// <returns>解析后的路由</returns>
    public static string ResolveControllerTemplate(string template, ISymbol classSymbol, string className)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        var result = new StringBuilder(template);

        // 解析 [area] 占位符
        var areaName = ExtractAreaName(classSymbol);
        if (!string.IsNullOrEmpty(areaName))
        {
            result.Replace("[area]", areaName!.ToLowerInvariant());
        }
        else
        {
            // 移除包含 [area] 的路径段
            result = RemovePathSegmentWithPlaceholder(result, "[area]");
        }

        // 解析 [controller] 占位符
        var controllerName = ExtractControllerName(className);
        result.Replace("[controller]", controllerName);

        return NormalizePath(result.ToString());
    }

    /// <summary>
    /// 解析动作级别的路由模板（主要用于 [action] 占位符）
    /// </summary>
    /// <param name="template">路由模板</param>
    /// <param name="methodName">方法名</param>
    /// <param name="httpMethod">HTTP方法</param>
    /// <returns>解析后的路由</returns>
    public static string ResolveActionTemplate(string template, string methodName, string httpMethod)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        var result = new StringBuilder(template);

        // 解析 [action] 占位符
        var actionName = ExtractActionName(methodName, httpMethod);
        result.Replace("[action]", actionName);

        return NormalizePath(result.ToString());
    }

    /// <summary>
    /// 从类符号中提取Area名称
    /// </summary>
    private static string? ExtractAreaName(ISymbol classSymbol)
    {
        if (classSymbol == null) return null;
        
        foreach (var attr in classSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "AreaAttribute")
            {
                if (attr.ConstructorArguments.Length > 0 &&
                    attr.ConstructorArguments[0].Value is string areaName)
                {
                    return areaName;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 从类名中提取控制器名称，移除常见后缀
    /// </summary>
    private static string ExtractControllerName(string className)
    {
        var name = className;
        
        // 移除常见后缀（不区分大小写）
        string[] suffixes = { "Service", "Controller", "Endpoint", "Endpoints" };
        
        foreach (var suffix in suffixes)
        {
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - suffix.Length);
                break;
            }
        }

        return StringExtensions.ConvertToKebabCase(name);
    }

    /// <summary>
    /// 从方法名中提取动作名称，移除HTTP动词前缀
    /// </summary>
    private static string ExtractActionName(string methodName, string httpMethod)
    {
        var name = methodName;

        // 移除HTTP动词前缀
        string[] prefixes = { httpMethod, "Get", "Post", "Put", "Delete", "Patch", "Head", "Options", "Create", "Add", "Update", "Remove" };
        
        foreach (var prefix in prefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(prefix.Length);
                break;
            }
        }

        // 如果移除前缀后为空，则使用原始方法名
        if (string.IsNullOrEmpty(name))
        {
            name = methodName;
        }

        return StringExtensions.ConvertToKebabCase(name);
    }

    /// <summary>
    /// 移除包含指定占位符的路径段
    /// </summary>
    private static StringBuilder RemovePathSegmentWithPlaceholder(StringBuilder path, string placeholder)
    {
        var pathStr = path.ToString();
        var placeholderIndex = pathStr.IndexOf(placeholder, StringComparison.Ordinal);
        
        if (placeholderIndex == -1)
            return path;

        // 找到包含占位符的路径段的开始和结束位置
        var segmentStart = placeholderIndex;
        var segmentEnd = placeholderIndex + placeholder.Length;

        // 向前查找路径段开始（最近的 '/' 或字符串开始）
        while (segmentStart > 0 && pathStr[segmentStart - 1] != '/')
        {
            segmentStart--;
        }

        // 向后查找路径段结束（最近的 '/' 或字符串结束）
        while (segmentEnd < pathStr.Length && pathStr[segmentEnd] != '/')
        {
            segmentEnd++;
        }

        // 如果路径段后面有 '/', 也一起移除
        if (segmentEnd < pathStr.Length && pathStr[segmentEnd] == '/')
        {
            segmentEnd++;
        }
        // 如果路径段前面有 '/' 且前面还有内容，保留前面的 '/'
        else if (segmentStart > 0 && pathStr[segmentStart - 1] == '/')
        {
            segmentStart--;
        }

        path.Remove(segmentStart, segmentEnd - segmentStart);
        return path;
    }

    /// <summary>
    /// 规范化路径，移除多余的斜杠，确保以 '/' 开头
    /// </summary>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        // 移除多余的斜杠
        var normalized = new StringBuilder();
        var lastWasSlash = false;

        foreach (var c in path)
        {
            if (c == '/')
            {
                if (!lastWasSlash)
                {
                    normalized.Append(c);
                    lastWasSlash = true;
                }
            }
            else
            {
                normalized.Append(c);
                lastWasSlash = false;
            }
        }

        var result = normalized.ToString();

        // 确保以 '/' 开头（除非为空）
        if (!string.IsNullOrEmpty(result) && !result.StartsWith("/"))
        {
            result = "/" + result;
        }

        // 移除末尾的 '/'（除非是根路径）
        if (result.Length > 1 && result.EndsWith("/"))
        {
            result = result.Substring(0, result.Length - 1);
        }

        return result;
    }
}