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

        // 使用常量来避免重复的字符串创建
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