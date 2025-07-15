using Microsoft.CodeAnalysis;
using System;
using System.Text;
using MiniController.Extensions;

namespace MiniController.Helpers;

/// <summary>
/// ·��ģ���������֧�� [area]��[controller]��[action] ռλ��
/// </summary>
public static class RouteTemplateResolver
{
    /// <summary>
    /// ���������������·��ģ��
    /// </summary>
    /// <param name="template">·��ģ�壬�� "/api/[area]/[controller]"</param>
    /// <param name="classSymbol">�����</param>
    /// <param name="className">����</param>
    /// <returns>�������·��</returns>
    public static string ResolveControllerTemplate(string template, ISymbol classSymbol, string className)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        var result = new StringBuilder(template);

        // ���� [area] ռλ��
        var areaName = ExtractAreaName(classSymbol);
        if (!string.IsNullOrEmpty(areaName))
        {
            result.Replace("[area]", areaName!.ToLowerInvariant());
        }
        else
        {
            // �Ƴ����� [area] ��·����
            result = RemovePathSegmentWithPlaceholder(result, "[area]");
        }

        // ���� [controller] ռλ��
        var controllerName = ExtractControllerName(className);
        result.Replace("[controller]", controllerName);

        return NormalizePath(result.ToString());
    }

    /// <summary>
    /// �������������·��ģ�壨��Ҫ���� [action] ռλ����
    /// </summary>
    /// <param name="template">·��ģ��</param>
    /// <param name="methodName">������</param>
    /// <param name="httpMethod">HTTP����</param>
    /// <returns>�������·��</returns>
    public static string ResolveActionTemplate(string template, string methodName, string httpMethod)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        var result = new StringBuilder(template);

        // ���� [action] ռλ��
        var actionName = ExtractActionName(methodName, httpMethod);
        result.Replace("[action]", actionName);

        return NormalizePath(result.ToString());
    }

    /// <summary>
    /// �����������ȡArea����
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
    /// ����������ȡ���������ƣ��Ƴ�������׺
    /// </summary>
    private static string ExtractControllerName(string className)
    {
        var name = className;
        
        // �Ƴ�������׺�������ִ�Сд��
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
    /// �ӷ���������ȡ�������ƣ��Ƴ�HTTP����ǰ׺
    /// </summary>
    private static string ExtractActionName(string methodName, string httpMethod)
    {
        var name = methodName;

        // �Ƴ�HTTP����ǰ׺
        string[] prefixes = { httpMethod, "Get", "Post", "Put", "Delete", "Patch", "Head", "Options", "Create", "Add", "Update", "Remove" };
        
        foreach (var prefix in prefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(prefix.Length);
                break;
            }
        }

        // ����Ƴ�ǰ׺��Ϊ�գ���ʹ��ԭʼ������
        if (string.IsNullOrEmpty(name))
        {
            name = methodName;
        }

        return StringExtensions.ConvertToKebabCase(name);
    }

    /// <summary>
    /// �Ƴ�����ָ��ռλ����·����
    /// </summary>
    private static StringBuilder RemovePathSegmentWithPlaceholder(StringBuilder path, string placeholder)
    {
        var pathStr = path.ToString();
        var placeholderIndex = pathStr.IndexOf(placeholder, StringComparison.Ordinal);
        
        if (placeholderIndex == -1)
            return path;

        // �ҵ�����ռλ����·���εĿ�ʼ�ͽ���λ��
        var segmentStart = placeholderIndex;
        var segmentEnd = placeholderIndex + placeholder.Length;

        // ��ǰ����·���ο�ʼ������� '/' ���ַ�����ʼ��
        while (segmentStart > 0 && pathStr[segmentStart - 1] != '/')
        {
            segmentStart--;
        }

        // ������·���ν���������� '/' ���ַ���������
        while (segmentEnd < pathStr.Length && pathStr[segmentEnd] != '/')
        {
            segmentEnd++;
        }

        // ���·���κ����� '/', Ҳһ���Ƴ�
        if (segmentEnd < pathStr.Length && pathStr[segmentEnd] == '/')
        {
            segmentEnd++;
        }
        // ���·����ǰ���� '/' ��ǰ�滹�����ݣ�����ǰ��� '/'
        else if (segmentStart > 0 && pathStr[segmentStart - 1] == '/')
        {
            segmentStart--;
        }

        path.Remove(segmentStart, segmentEnd - segmentStart);
        return path;
    }

    /// <summary>
    /// �淶��·�����Ƴ������б�ܣ�ȷ���� '/' ��ͷ
    /// </summary>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        // �Ƴ������б��
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

        // ȷ���� '/' ��ͷ������Ϊ�գ�
        if (!string.IsNullOrEmpty(result) && !result.StartsWith("/"))
        {
            result = "/" + result;
        }

        // �Ƴ�ĩβ�� '/'�������Ǹ�·����
        if (result.Length > 1 && result.EndsWith("/"))
        {
            result = result.Substring(0, result.Length - 1);
        }

        return result;
    }
}