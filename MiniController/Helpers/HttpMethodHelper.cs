using System;
using System.Linq;
using MiniController.Constants;
using MiniController.Extensions;

namespace MiniController.Helpers;

public static class HttpMethodHelper
{
    public static (string HttpMethod, string Template)? InferHttpMethodFromName(string methodName)
    {
        if (string.IsNullOrEmpty(methodName))
            return null;

        var span = methodName.AsSpan();

        foreach (var prefix in HttpConstants.HttpMethodPrefixes)
        {
            if (span.StartsWith(prefix.AsSpan(), StringComparison.Ordinal))
            {
                var template = StringExtensions.InferRouteFromMethodName(methodName, prefix);
                return (prefix, template);
            }
        }

        // 使用常见的命名约定字符串常量
        const string createPrefix = "Create";
        const string addPrefix = "Add";
        const string updatePrefix = "Update";
        const string removePrefix = "Remove";

        if (span.StartsWith(createPrefix.AsSpan(), StringComparison.Ordinal) ||
            span.StartsWith(addPrefix.AsSpan(), StringComparison.Ordinal))
        {
            var template = StringExtensions.InferRouteFromMethodName(methodName, new[] { "Post", createPrefix, addPrefix });
            return ("Post", template);
        }

        if (span.StartsWith(updatePrefix.AsSpan(), StringComparison.Ordinal))
        {
            var template = StringExtensions.InferRouteFromMethodName(methodName, new[] { "Put", updatePrefix });
            return ("Put", template);
        }

        if (span.StartsWith(removePrefix.AsSpan(), StringComparison.Ordinal))
        {
            var template = StringExtensions.InferRouteFromMethodName(methodName, new[] { "Delete", removePrefix });
            return ("Delete", template);
        }

        return null;
    }

    public static string GetRouteTemplate(Microsoft.CodeAnalysis.ISymbol methodSymbol, string httpMethod)
    {
        foreach (var attr in methodSymbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.Name;
            if (attrName == $"Http{httpMethod}Attribute" ||
                attrName == $"{httpMethod}Attribute")
            {
                if (attr.ConstructorArguments.Length > 0 &&
                    attr.ConstructorArguments[0].Value is string template)
                {
                    return template;
                }
            }
        }

        var methodName = methodSymbol.Name;

        string[] prefixes = httpMethod switch
        {
            "Get" => new[] { "Get" },
            "Post" => new[] { "Post", "Create", "Add" },
            "Put" => new[] { "Put", "Update" },
            "Delete" => new[] { "Delete", "Remove" },
            "Patch" => new[] { "Patch" },
            "Head" => new[] { "Head" },
            "Options" => new[] { "Options" },
            _ => Array.Empty<string>()
        };

        if (prefixes.Length > 0 && prefixes.Any(p => methodName.StartsWith(p, StringComparison.Ordinal)))
        {
            return StringExtensions.InferRouteFromMethodName(methodName, prefixes);
        }

        return string.Empty;
    }

    /// <summary>
    /// 为[action]占位符生成路由模板
    /// </summary>
    public static string GetRouteTemplateForAction(Microsoft.CodeAnalysis.ISymbol methodSymbol, string httpMethod, string methodName)
    {
        // 首先检查是否有明确的路由模板
        foreach (var attr in methodSymbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.Name;
            if (attrName == $"Http{httpMethod}Attribute" ||
                attrName == $"{httpMethod}Attribute")
            {
                if (attr.ConstructorArguments.Length > 0 &&
                    attr.ConstructorArguments[0].Value is string template)
                {
                    return template;
                }
                // 如果找到HTTP特性但没有模板，继续用方法名推断
                break;
            }
        }

        // 对于[action]模式，我们需要从方法名中移除HTTP前缀，然后转换为kebab-case
        string[] prefixes = httpMethod switch
        {
            "Get" => new[] { "Get" },
            "Post" => new[] { "Post", "Create", "Add" },
            "Put" => new[] { "Put", "Update" },
            "Delete" => new[] { "Delete", "Remove" },
            "Patch" => new[] { "Patch" },
            "Head" => new[] { "Head" },
            "Options" => new[] { "Options" },
            _ => Array.Empty<string>()
        };

        // 如果方法名以HTTP前缀开头，移除前缀后转换为kebab-case
        if (prefixes.Length > 0)
        {
            foreach (var prefix in prefixes)
            {
                if (methodName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    var actionPart = methodName.Substring(prefix.Length);
                    // 如果移除前缀后还有内容，转换为kebab-case
                    if (!string.IsNullOrEmpty(actionPart))
                    {
                        return StringExtensions.ConvertToKebabCase(actionPart);
                    }
                    // 如果移除前缀后没有内容，使用原方法名转换
                    break;
                }
            }
        }

        // 如果没有匹配的前缀，直接转换整个方法名
        return StringExtensions.ConvertToKebabCase(methodName);
    }

    public static string? GetFilterType(Microsoft.CodeAnalysis.ISymbol methodSymbol, string httpMethod)
    {
        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == $"{httpMethod}Attribute")
            {
                foreach (var namedArg in attr.NamedArguments)
                {
                    if (namedArg.Key == "FilterType" &&
                        namedArg.Value.Value is Microsoft.CodeAnalysis.INamedTypeSymbol filterTypeSymbol)
                    {
                        return filterTypeSymbol.ToDisplayString();
                    }
                }
            }
        }
        return null;
    }
}