using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace MiniController;

[Generator]
public class EndpointGenerator : IIncrementalGenerator
{
    private readonly static ConcurrentBag<(string nameSpace, string className)> ClassInfoList = new();

    private const string MiniControllerAttributeName = "MiniControllerAttribute";
    private const string AuthorizeAttributeName = "AuthorizeAttribute";
    private const string AllowAnonymousAttributeName = "AllowAnonymousAttribute";
    private const string ApiExplorerSettingsAttributeName = "ApiExplorerSettingsAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
       // Debugger.Launch(); // 启用调试器
#endif
        // 1. 筛选出带有MiniControllerAttribute的类
        var endpointGroupProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => IsEndpointGroupClass(s),
                transform: (ctx, _) => GetEndpointGroupClass(ctx))
            .Where(c => c is not null);

        // 使用SelectMany将所有控制器信息合并为一个集合
        var endpointGroupsCollection = endpointGroupProvider.Collect();

        // 使用CompilationProvider和SelectMany创建源代码生成管道
        var combinedProvider = context.CompilationProvider.Combine(endpointGroupsCollection);

        // 2. 生成代码：为每个控制器生成扩展方法
        context.RegisterImplementationSourceOutput(
            endpointGroupProvider,
            (spc, endpointGroup) => GenerateEndpointRegistration(spc, endpointGroup!)
        );

        // 3. 生成聚合注册方法
        context.RegisterImplementationSourceOutput(
            combinedProvider,
            (spc, tuple) => GenerateMiniControllerRegistration(spc, [.. tuple.Right.Select(e => (e.Namespace, e.ClassName))])
        );
    }

    private bool IsEndpointGroupClass(SyntaxNode node)
        => node is ClassDeclarationSyntax classDecl &&
           classDecl.AttributeLists.Any(attrList =>
               attrList.Attributes.Any(attr =>
                   attr.Name.ToString() == "MiniController"));

    private EndpointGroupClass? GetEndpointGroupClass(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var model = context.SemanticModel;
        var classSymbol = model.GetDeclaredSymbol(classDecl);

        if (classSymbol == null)
        {
            // 无法获取类符号，返回 null
            return null;
        }

        // 提取控制器级别的元数据
        var controllerMetadata = ExtractControllerMetadata(classSymbol, classDecl);
        if (string.IsNullOrEmpty(controllerMetadata.RoutePrefix))
            return null;

        // 收集类中的端点方法
        var endpointMethods = CollectEndpointMethods(classDecl, model, controllerMetadata);

        // 创建并返回 EndpointGroupClass
        return new EndpointGroupClass
        {
            Namespace = classSymbol.ContainingNamespace.ToString(),
            ClassName = classDecl.Identifier.ValueText,
            RoutePrefix = controllerMetadata.RoutePrefix,
            Name = controllerMetadata.GroupName,
            FilterType = controllerMetadata.FilterType,
            Authorize = controllerMetadata.Authorize,
            EndpointMethods = endpointMethods
        };
    }

    /// <summary>
    /// 提取控制器级别的元数据
    /// </summary>
    private ControllerMetadata ExtractControllerMetadata(ISymbol classSymbol, ClassDeclarationSyntax classDecl)
    {
        var metadata = new ControllerMetadata();

        foreach (var attr in classSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == MiniControllerAttributeName)
            {
                metadata.RoutePrefix = ExtractRoutePrefix(attr, classDecl);
                metadata.GroupName = ExtractGroupName(attr);
                metadata.FilterType = ExtractControllerFilterType(attr);
            }
            else if (attr.AttributeClass?.Name == AuthorizeAttributeName)
            {
                metadata.Authorize = ExtractAuthorizeMetadata(attr);
            }
            else if (attr.AttributeClass?.Name == AllowAnonymousAttributeName)
            {
                metadata.Authorize = new AuthorizeMetadata { AllowAnonymous = true };
            }
            else if (attr.AttributeClass?.Name == ApiExplorerSettingsAttributeName)
            {
                metadata.ApiExplorerSettings = ExtractApiExplorerSettings(attr);
            }
        }

        return metadata;
    }

    /// <summary>
    /// 提取路由前缀
    /// </summary>
    private string ExtractRoutePrefix(AttributeData attr, ClassDeclarationSyntax classDecl)
    {
        // 从构造函数参数中获取路由前缀
        if (attr.ConstructorArguments.Length > 0 &&
            attr.ConstructorArguments[0].Value is string prefix)
        {
            return prefix;
        }

        // 如果没有提供前缀，则根据类名生成默认前缀
        return $"/api/{classDecl.Identifier.ValueText.ToLowerInvariant()
            .Replace("service", "")
            .Replace("controller", "")
            .Replace("endpoint", "")}";
    }

    /// <summary>
    /// 提取组名称
    /// </summary>
    private string? ExtractGroupName(AttributeData attr)
    {
        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg.Key == "Name" && namedArg.Value.Value is string name)
                return name;
        }

        return null;
    }

    /// <summary>
    /// 提取控制器级过滤器类型
    /// </summary>
    private string? ExtractControllerFilterType(AttributeData attr)
    {
        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg.Key == "FilterType" &&
                namedArg.Value.Value is INamedTypeSymbol filterTypeSymbol)
            {
                return filterTypeSymbol.ToDisplayString();
            }
        }

        return null;
    }

    /// <summary>
    /// 收集类中的端点方法
    /// </summary>
    private List<EndpointMethod> CollectEndpointMethods(
        ClassDeclarationSyntax classDecl,
        SemanticModel model,
        ControllerMetadata controllerMetadata)
    {
        var endpointMethods = new List<EndpointMethod>();

        foreach (var member in classDecl.Members)
        {
            if (member is MethodDeclarationSyntax methodDecl &&
                methodDecl.Modifiers.Any(m => m.ValueText == "public"))
            {
                var methodSymbol = model.GetDeclaredSymbol(methodDecl);
                if (methodSymbol == null) continue;

                var endpointMethod = ProcessEndpointMethod(methodSymbol, methodDecl, controllerMetadata);
                if (endpointMethod != null)
                {
                    endpointMethods.Add(endpointMethod);
                }
            }
        }

        return endpointMethods;
    }

    /// <summary>
    /// 处理单个端点方法
    /// </summary>
    private EndpointMethod? ProcessEndpointMethod(
        ISymbol methodSymbol,
        MethodDeclarationSyntax methodDecl,
        ControllerMetadata controllerMetadata)
    {
        // 提取方法级元数据
        var methodAuthorize = ExtractMethodAuthorizeMetadata(methodSymbol);
        var responseTypes = ExtractResponseTypes(methodSymbol);
        var methodApiExplorerSettings = ExtractMethodApiExplorerSettings(methodSymbol);
        var httpMethodInfo = GetHttpMethodInfo(methodSymbol);

        if (httpMethodInfo == null)
            return null;

        // 合并类和方法级的授权和API浏览器设置
        var effectiveAuthorize = MergeAuthorizeMetadata(controllerMetadata.Authorize, methodAuthorize);
        var effectiveApiExplorerSettings = MergeApiExplorerSettings(
            controllerMetadata.ApiExplorerSettings,
            methodApiExplorerSettings
        );

        return new EndpointMethod
        {
            Name = methodDecl.Identifier.ValueText,
            HttpMethod = httpMethodInfo.Value.HttpMethod,
            RouteTemplate = GetRouteTemplate(methodSymbol, httpMethodInfo.Value.HttpMethod),
            FilterType = GetFilterType(methodSymbol, httpMethodInfo.Value.HttpMethod),
            Authorize = effectiveAuthorize,
            ResponseTypes = responseTypes,
            ApiExplorerSettings = effectiveApiExplorerSettings
        };
    }

    private ApiExplorerSettingsMetadata? ExtractMethodApiExplorerSettings(ISymbol methodSymbol)
    {
        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "ApiExplorerSettingsAttribute")
            {
                return ExtractApiExplorerSettings(attr);
            }
        }
        return null;
    }

    private ApiExplorerSettingsMetadata? ExtractApiExplorerSettings(AttributeData attr)
    {
        bool? ignoreApi = null;
        string? groupName = null;

        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg.Key == "IgnoreApi" && namedArg.Value.Value is bool ignore)
                ignoreApi = ignore;

            if (namedArg.Key == "GroupName" && namedArg.Value.Value is string name)
                groupName = name;
        }

        if (ignoreApi == null && groupName == null)
            return null;

        return new ApiExplorerSettingsMetadata
        {
            IgnoreApi = ignoreApi,
            GroupName = groupName
        };
    }

    private ApiExplorerSettingsMetadata? MergeApiExplorerSettings(
        ApiExplorerSettingsMetadata? groupSettings,
        ApiExplorerSettingsMetadata? methodSettings)
    {
        if (methodSettings == null) return groupSettings;
        if (groupSettings == null) return methodSettings;

        // 方法级覆盖类级
        return new ApiExplorerSettingsMetadata
        {
            IgnoreApi = methodSettings.IgnoreApi ?? groupSettings.IgnoreApi,
            GroupName = methodSettings.GroupName ?? groupSettings.GroupName,
        };
    }

    private (string HttpMethod, string Template)? GetHttpMethodInfo(ISymbol methodSymbol)
    {
        // 支持的HTTP方法特性列表
        var httpAttributes = new[]
        {
                "HttpGetAttribute", "HttpPostAttribute", "HttpPutAttribute",
                "HttpDeleteAttribute", "HttpPatchAttribute", "HttpHeadAttribute", "HttpOptionsAttribute"
            };

        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (httpAttributes.Contains(attr.AttributeClass?.Name))
            {
                var httpMethod = attr.AttributeClass!.Name.Replace("Attribute", "").Replace("Http", "");
                var template = string.Empty;

                // 获取路由模板（构造函数第一个参数）
                if (attr.ConstructorArguments.Length > 0 &&
                    attr.ConstructorArguments[0].Value is string templateValue)
                {
                    template = templateValue;
                }

                return (httpMethod, template);
            }
        }

        // 如果没有 HTTP 方法特性，则根据方法名称推断
        var methodName = methodSymbol.Name;

        // 根据方法名称前缀推断 HTTP 方法
        if (methodName.StartsWith("Get"))
        {
            return ("Get", InferRouteFromMethodName(methodName, "Get"));
        }
        else if (methodName.StartsWith("Post") || methodName.StartsWith("Create") || methodName.StartsWith("Add"))
        {
            return ("Post", InferRouteFromMethodName(methodName, ["Post", "Create", "Add"]));
        }
        else if (methodName.StartsWith("Put") || methodName.StartsWith("Update"))
        {
            return ("Put", InferRouteFromMethodName(methodName, ["Put", "Update"]));
        }
        else if (methodName.StartsWith("Delete") || methodName.StartsWith("Remove"))
        {
            return ("Delete", InferRouteFromMethodName(methodName, ["Delete", "Remove"]));
        }
        else if (methodName.StartsWith("Patch"))
        {
            return ("Patch", InferRouteFromMethodName(methodName, "Patch"));
        }
        else if (methodName.StartsWith("Head"))
        {
            return ("Head", InferRouteFromMethodName(methodName, "Head"));
        }
        else if (methodName.StartsWith("Options"))
        {
            return ("Options", InferRouteFromMethodName(methodName, "Options"));
        }

        // 如果方法名称不符合任何模式，则返回 null
        return null;
    }

    /// <summary>
    /// 根据方法名称推断路由模板
    /// </summary>
    private string InferRouteFromMethodName(string methodName, string prefix)
    {
        return InferRouteFromMethodName(methodName, new[] { prefix });
    }

    /// <summary>
    /// 根据方法名称推断路由模板，支持多个前缀
    /// </summary>
    private string InferRouteFromMethodName(string methodName, string[] prefixes)
    {
        string routeName = methodName;

        // 移除方法名称前缀
        foreach (var prefix in prefixes)
        {
            if (routeName.StartsWith(prefix))
            {
                routeName = routeName.Substring(prefix.Length);
                break;
            }
        }

        // 如果移除前缀后为空，则返回空路由
        if (string.IsNullOrEmpty(routeName))
        {
            return string.Empty;
        }

        // 确保第一个字符小写
        routeName = char.ToLowerInvariant(routeName[0]) + routeName.Substring(1);

        // 转换为 kebab-case 格式
        var kebabCase = string.Empty;
        for (int i = 0; i < routeName.Length; i++)
        {
            var c = routeName[i];
            if (i > 0 && char.IsUpper(c))
            {
                kebabCase += "-" + char.ToLowerInvariant(c);
            }
            else
            {
                kebabCase += char.ToLowerInvariant(c);
            }
        }

        // 移除末尾的"Async"（如果有）
        if (kebabCase.EndsWith("-async"))
        {
            kebabCase = kebabCase.Substring(0, kebabCase.Length - 6);
        }

        return kebabCase;
    }

    private string GetRouteTemplate(ISymbol methodSymbol, string httpMethod)
    {
        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == $"Http{httpMethod}Attribute" &&
                attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is string template)
            {
                return template;
            }
        }
        return string.Empty;
    }

    private string? GetFilterType(ISymbol methodSymbol, string httpMethod)
    {
        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == $"{httpMethod}Attribute")
            {
                foreach (var namedArg in attr.NamedArguments)
                {
                    if (namedArg.Key == "FilterType" &&
                        namedArg.Value.Value is INamedTypeSymbol filterTypeSymbol)
                    {
                        return filterTypeSymbol.ToDisplayString();
                    }
                }
            }
        }
        return null;
    }


    private AuthorizeMetadata? ExtractMethodAuthorizeMetadata(ISymbol methodSymbol)
    {
        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == AuthorizeAttributeName)
            {
                return ExtractAuthorizeMetadata(attr);
            }
            if (attr.AttributeClass?.Name == AllowAnonymousAttributeName)
            {
                return new AuthorizeMetadata
                {
                    AllowAnonymous = true
                };
            }
        }
        return null;
    }

    private AuthorizeMetadata? ExtractAuthorizeMetadata(AttributeData attr)
    {
        string? policy = null;
        string? roles = null;
        string? authenticationSchemes = null;

        // 检查构造函数参数
        if (attr.ConstructorArguments.Length > 0 &&
            attr.ConstructorArguments[0].Value is string policyValue)
        {
            policy = policyValue;
        }

        // 检查命名参数
        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg.Key == "Policy" && namedArg.Value.Value is string p)
                policy = p;

            if (namedArg.Key == "Roles" && namedArg.Value.Value is string r)
                roles = r;

            if (namedArg.Key == "AuthenticationSchemes" && namedArg.Value.Value is string s)
                authenticationSchemes = s;
        }

        if (policy == null && roles == null && authenticationSchemes == null)
            return new AuthorizeMetadata();

        return new AuthorizeMetadata
        {
            Policy = policy,
            Roles = roles,
            AuthenticationSchemes = authenticationSchemes
        };
    }

    private AuthorizeMetadata? MergeAuthorizeMetadata(AuthorizeMetadata? group, AuthorizeMetadata? method)
    {
        if (method == null) return group;
        if (group == null) return method;

        // 方法级覆盖类级
        return new AuthorizeMetadata
        {
            Policy = method.Policy ?? group.Policy,
            Roles = method.Roles ?? group.Roles,
            AuthenticationSchemes = method.AuthenticationSchemes ?? group.AuthenticationSchemes
        };
    }


    private List<ResponseTypeMetadata> ExtractResponseTypes(ISymbol methodSymbol)
    {
        var responseTypes = new List<ResponseTypeMetadata>();

        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "ProducesResponseTypeAttribute")
            {
                var statusCode = 200; // 默认值
                string? typeName = null;
                string? contentType = null;

                // 获取StatusCode（构造函数第一个参数）
                if (attr.ConstructorArguments.Length > 0 &&
                    attr.ConstructorArguments[0].Value is int code)
                {
                    statusCode = code;
                }

                // 获取Type（构造函数第二个参数）
                if (attr.ConstructorArguments.Length > 1 &&
                    attr.ConstructorArguments[1].Value is INamedTypeSymbol typeSymbol)
                {
                    typeName = typeSymbol.ToDisplayString();
                }

                // 获取ContentType（命名参数）
                foreach (var namedArg in attr.NamedArguments)
                {
                    if (namedArg.Key == "ContentType" && namedArg.Value.Value is string ct)
                    {
                        contentType = ct;
                    }
                }

                responseTypes.Add(new ResponseTypeMetadata
                {
                    StatusCode = statusCode,
                    TypeName = typeName,
                    ContentType = contentType
                });
            }
        }

        return responseTypes;
    }

    private void GenerateEndpointRegistration(SourceProductionContext context, EndpointGroupClass endpointGroup)
    {
        var source = new StringBuilder();
        source.AppendLine("// <auto-generated>");
        source.AppendLine("// 由EndpointGenerator自动生成，请勿修改");
        source.AppendLine("// </auto-generated>");
        source.AppendLine();
        source.AppendLine("using Microsoft.AspNetCore.Builder;");
        source.AppendLine("using Microsoft.AspNetCore.Http;");
        source.AppendLine();
        source.AppendLine($"namespace {endpointGroup.Namespace}");
        source.AppendLine("{");
        source.AppendLine($"    public static class {endpointGroup.ClassName}Extensions");
        source.AppendLine("    {");
        source.AppendLine($"        public static IEndpointRouteBuilder Map{endpointGroup.ClassName}(this IEndpointRouteBuilder builder)");
        source.AppendLine("        {");
        source.AppendLine($"            var group = builder.MapGroup(\"{endpointGroup.RoutePrefix}\");");

        // 应用组级配置
        if (!string.IsNullOrEmpty(endpointGroup.Name))
        {
            source.AppendLine($"            group.WithName(\"{endpointGroup.Name}\");");
        }

        if (endpointGroup.Authorize != null)
        {
            var authorizeCall = BuildAuthorizeCall(endpointGroup.Authorize);
            source.AppendLine($"            group{authorizeCall};");
        }

        if (!string.IsNullOrEmpty(endpointGroup.FilterType))
        {
            source.AppendLine($"            group.AddEndpointFilter<{endpointGroup.FilterType}>();");
        }

        source.AppendLine();

        // 生成端点注册代码
        foreach (var method in endpointGroup.EndpointMethods)
        {
            var httpMethod = method.HttpMethod;
            var routeTemplate = string.IsNullOrEmpty(method.RouteTemplate)
                ? string.Empty
                : $", \"{method.RouteTemplate}\"";

            var endpointBuilder = $"            group.Map{httpMethod}(\"{method.RouteTemplate}\", {endpointGroup.ClassName}.{method.Name})";


            // 添加授权配置
            if (method.Authorize != null)
            {
                var authorizeCall = BuildAuthorizeCall(method.Authorize);
                endpointBuilder += authorizeCall;
            }

            // 添加ApiExplorerSettings配置
            if (method.ApiExplorerSettings != null)
            {
                var apiExplorerCall = BuildApiExplorerSettingsCall(method.ApiExplorerSettings);
                if (!string.IsNullOrEmpty(apiExplorerCall))
                {
                    endpointBuilder += apiExplorerCall;
                }
            }
            else
            {
                endpointBuilder += ".WithOpenApi()";
            }

            // 添加响应类型配置
            foreach (var responseType in method.ResponseTypes)
            {
                if (responseType.TypeName != null)
                {
                    endpointBuilder += $".Produces<{responseType.TypeName}>({responseType.StatusCode}";
                    if (!string.IsNullOrEmpty(responseType.ContentType))
                    {
                        endpointBuilder += $", \"{responseType.ContentType}\"";
                    }
                    endpointBuilder += ")";
                }
                else
                {
                    endpointBuilder += $".Produces({responseType.StatusCode}";
                    if (!string.IsNullOrEmpty(responseType.ContentType))
                    {
                        endpointBuilder += $", \"{responseType.ContentType}\"";
                    }
                    endpointBuilder += ")";
                }
            }

            if (!string.IsNullOrEmpty(method.FilterType))
            {
                endpointBuilder += $".AddEndpointFilter<{method.FilterType}>()";
            }

            endpointBuilder += ";";
            source.AppendLine(endpointBuilder);
        }

        source.AppendLine();
        source.AppendLine("            return builder;");
        source.AppendLine("        }");
        source.AppendLine("    }");
        source.AppendLine("}");

        context.AddSource($"{endpointGroup.ClassName}Extensions.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
        ClassInfoList.Add((endpointGroup.Namespace, endpointGroup.ClassName));
    }


    private void GenerateMiniControllerRegistration(SourceProductionContext context, List<(string nameSpace, string className)> ClassInfoList)
    {
        var Extension = new StringBuilder();
        Extension.AppendLine("// <auto-generated>");
        Extension.AppendLine("// 由EndpointGenerator自动生成，请勿修改");
        Extension.AppendLine("// </auto-generated>");
        Extension.AppendLine();
        Extension.AppendLine("using Microsoft.AspNetCore.Builder;");
        Extension.AppendLine("using Microsoft.AspNetCore.Http;");
        foreach (var classInfo in ClassInfoList)
        {
            Extension.AppendLine($"using {classInfo.nameSpace};");
        }
        Extension.AppendLine();
        Extension.AppendLine($"namespace Microsoft.AspNetCore.Builder");
        Extension.AppendLine("{");
        Extension.AppendLine($"    public static class MiniControllerExtensions");
        Extension.AppendLine("    {");
        Extension.AppendLine($"        public static IEndpointRouteBuilder MapMiniController(this IEndpointRouteBuilder builder)");
        Extension.AppendLine("        {");
        foreach (var classInfo in ClassInfoList)
        {
            Extension.AppendLine($"           builder.Map{classInfo.className}();");
        }
        Extension.AppendLine("            return builder;");
        Extension.AppendLine("        }");
        Extension.AppendLine("    }");
        Extension.AppendLine("}");

        context.AddSource($"MiniControllerExtensions.g.cs", SourceText.From(Extension.ToString(), Encoding.UTF8));
    }
    private string BuildAuthorizeCall(AuthorizeMetadata metadata)
    {
        if (metadata == null) return string.Empty;

        var arguments = new List<string>();

        if (!string.IsNullOrEmpty(metadata.Policy))
            arguments.Add($"\"{metadata.Policy}\"");

        if (!string.IsNullOrEmpty(metadata.Roles))
            arguments.Add($"Roles: \"{metadata.Roles}\"");

        if (!string.IsNullOrEmpty(metadata.AuthenticationSchemes))
            arguments.Add($"AuthenticationSchemes: \"{metadata.AuthenticationSchemes}\"");

        if (arguments.Count == 0)
            return $".RequireAuthorization()";

        return $".RequireAuthorization({string.Join(", ", arguments)})";
    }

    private string BuildApiExplorerSettingsCall(ApiExplorerSettingsMetadata settings)
    {
        if (settings == null) return string.Empty;

        var builder = new StringBuilder();

        if (settings.IgnoreApi == true)
        {
            builder.Append(".ExcludeFromDescription()");
        }
        else
        {
            builder.Append(".WithOpenApi()");
        }

        if (!string.IsNullOrEmpty(settings.GroupName))
        {
            builder.Append($".WithTags(\"{settings.GroupName}\")");
        }

        return builder.ToString();
    }
}
// === 辅助模型类 ===

public class EndpointGroupClass
{
    public string Namespace { get; set; } = string.Empty;

    public string ClassName { get; set; } = string.Empty;

    public string RoutePrefix { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string? FilterType { get; set; }

    public AuthorizeMetadata? Authorize { get; set; }

    public List<EndpointMethod> EndpointMethods { get; set; } = new();
}

public class EndpointMethod
{
    public string Name { get; set; } = string.Empty;

    public string HttpMethod { get; set; } = string.Empty;

    public string RouteTemplate { get; set; } = string.Empty;

    public string? FilterType { get; set; }

    public AuthorizeMetadata? Authorize { get; set; }

    public List<ResponseTypeMetadata> ResponseTypes { get; set; } = [];

    public ApiExplorerSettingsMetadata ApiExplorerSettings { get; set; }
}

public class AuthorizeMetadata
{
    public string? Policy { get; set; }

    public string? Roles { get; set; }

    public string? AuthenticationSchemes { get; set; }

    public bool AllowAnonymous { get; set; } = false; // 添加AllowAnonymous属性以支持匿名访问
}

public class ResponseTypeMetadata
{
    public int StatusCode { get; set; }
    public string? TypeName { get; set; }
    public string? ContentType { get; set; }
}

public class ApiExplorerSettingsMetadata
{
    public bool? IgnoreApi { get; set; }

    public string? GroupName { get; set; }
}

/// <summary>
/// 用于存储控制器级元数据的内部类
/// </summary>
public class ControllerMetadata
{
    public string RoutePrefix { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public string? FilterType { get; set; }
    public AuthorizeMetadata? Authorize { get; set; }
    public ApiExplorerSettingsMetadata? ApiExplorerSettings { get; set; }
}