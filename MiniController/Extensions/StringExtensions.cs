using Microsoft.CodeAnalysis;
using MiniController.Helpers;
using System.Collections.Concurrent;
using System.Text;

namespace MiniController.Extensions;

public static class StringExtensions
{
    // �Ż������Ա����ظ����ɣ������ַ���ƴ��
    private static readonly ConcurrentDictionary<string, string> RouteNameCache = new();
    private static readonly ConcurrentDictionary<(string methodName, string prefixes), string> KebabCaseCache = new();

    /// <summary>
    /// ��������ȡ�����·�ɣ�֧��ģ���﷨����
    /// </summary>
    /// <param name="className">����</param>
    /// <param name="classSymbol">����ţ���ѡ������ģ�������</param>
    /// <param name="routeTemplate">·��ģ�壨��ѡ��</param>
    /// <returns>�������·��</returns>
    public static string GetOrAddRouteFromClassName(string className, ISymbol? classSymbol = null, string? routeTemplate = null)
    {
        // ����ṩ��·��ģ���Ұ���ռλ������ʹ��ģ�����
        if (!string.IsNullOrEmpty(routeTemplate) && classSymbol != null && ContainsTemplatePlaceholders(routeTemplate!))
        {
            return RouteTemplateResolver.ResolveControllerTemplate(routeTemplate!, classSymbol, className);
        }

        // ����ṩ����ȷ��·��ģ���Ҳ�����ռλ����ֱ��ʹ��
        if (!string.IsNullOrEmpty(routeTemplate))
        {
            return routeTemplate!.StartsWith("/") ? routeTemplate : "/" + routeTemplate;
        }

        // ʹ��Ĭ���߼�
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
    /// ���·��ģ���Ƿ����ռλ��
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

        // �Ƴ� "Async" ��׺��������ڣ�
        const string asyncSuffix = "Async";
        if (input.EndsWith(asyncSuffix, StringComparison.Ordinal))
        {
            input = input.Substring(0, input.Length - asyncSuffix.Length);
        }

        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Ԥ�� StringBuilder �����Լ����ط���
        var estimatedLength = input.Length + (input.Length / 3); // Լ���� 33% �����ַ�
        var result = new StringBuilder(estimatedLength);

        // ��һ���ַ�ת��ΪСд
        result.Append(char.ToLowerInvariant(input[0]));

        // ����ʣ���ַ�
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