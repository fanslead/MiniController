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

        // ʹ�ó���������Լ���ַ�������
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
    /// Ϊ[action]ռλ������·��ģ��
    /// </summary>
    public static string GetRouteTemplateForAction(Microsoft.CodeAnalysis.ISymbol methodSymbol, string httpMethod, string methodName)
    {
        // ���ȼ���Ƿ�����ȷ��·��ģ��
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
                // ����ҵ�HTTP���Ե�û��ģ�壬�����÷������ƶ�
                break;
            }
        }

        // ����[action]ģʽ��������Ҫ�ӷ��������Ƴ�HTTPǰ׺��Ȼ��ת��Ϊkebab-case
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

        // �����������HTTPǰ׺��ͷ���Ƴ�ǰ׺��ת��Ϊkebab-case
        if (prefixes.Length > 0)
        {
            foreach (var prefix in prefixes)
            {
                if (methodName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    var actionPart = methodName.Substring(prefix.Length);
                    // ����Ƴ�ǰ׺�������ݣ�ת��Ϊkebab-case
                    if (!string.IsNullOrEmpty(actionPart))
                    {
                        return StringExtensions.ConvertToKebabCase(actionPart);
                    }
                    // ����Ƴ�ǰ׺��û�����ݣ�ʹ��ԭ������ת��
                    break;
                }
            }
        }

        // ���û��ƥ���ǰ׺��ֱ��ת������������
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