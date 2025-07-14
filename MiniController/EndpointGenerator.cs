using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Text;

namespace MiniController;

[Generator]
public class EndpointGenerator : IIncrementalGenerator
{
    private readonly static List<(string nameSpace, string className)> ClassInfoList = [];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    { 
        Debugger.Launch(); // 启用调试器
        // 1. 筛选出带有MiniControllerAttribute的类
        var endpointGroupClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => IsEndpointGroupClass(s),
                transform: (ctx, _) => GetEndpointGroupClass(ctx))
            .Where(c => c is not null)!;

        // 2. 生成代码
        context.RegisterSourceOutput(endpointGroupClasses, (spc, endpointGroup) =>
        {
            GenerateEndpointRegistration(spc, endpointGroup!);
        });

        context.RegisterSourceOutput(context.CompilationProvider, (spc, _) =>
        {
            GenerateMiniControllerRegistration(spc, ClassInfoList.Distinct().ToList());
        });
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
        var classSymbol = model.GetDeclaredSymbol(classDecl)!;

        // 获取MiniControllerAttribute的RoutePrefix
        var routePrefix = string.Empty;
        string? groupName = null;
        string? groupFilterType = null;
        AuthorizeMetadata? groupAuthorize = null;
        ApiExplorerSettingsMetadata? groupApiExplorerSettings = null;

        foreach (var attr in classSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "MiniControllerAttribute")
            {
                if (attr.ConstructorArguments.Length > 0 &&
                    attr.ConstructorArguments[0].Value is string prefix)
                {
                    routePrefix = prefix;
                }
                else
                {
                    // 如果没有提供RoutePrefix，则使用类名作为默认前缀
                    routePrefix = $"/api/{classDecl.Identifier.ValueText.ToLowerInvariant()
                        .Replace("service", "")
                        .Replace("controller", "")
                        .Replace("endpoint", "")}";
                }

                // 获取可选参数
                foreach (var namedArg in attr.NamedArguments)
                {
                    if (namedArg.Key == "Name" && namedArg.Value.Value is string name)
                        groupName = name;

                    if (namedArg.Key == "FilterType" &&
                        namedArg.Value.Value is INamedTypeSymbol filterTypeSymbol)
                    {
                        groupFilterType = filterTypeSymbol.ToDisplayString();
                    }
                }

                break;
            }

            // 检查类上的[Authorize]特性
            if (attr.AttributeClass?.Name == "AuthorizeAttribute")
            {
                groupAuthorize = ExtractAuthorizeMetadata(attr);
            }

            // 检查类上的[AllowAnonymous]特性
            if (attr.AttributeClass?.Name == "AllowAnonymousAttribute")
            {
                groupAuthorize =new AuthorizeMetadata
                {
                    AllowAnonymous = true
                };
            }

            // 检查类上的[ApiExplorerSettings]特性
            if (attr.AttributeClass?.Name == "ApiExplorerSettingsAttribute")
            {
                groupApiExplorerSettings = ExtractApiExplorerSettings(attr);
            }
        }

        if (string.IsNullOrEmpty(routePrefix))
            return null;

        // 收集类中的端点方法
        var endpointMethods = new List<EndpointMethod>();
        foreach (var member in classDecl.Members)
        {
            if (member is MethodDeclarationSyntax methodDecl &&
                methodDecl.Modifiers.Any(m => m.ValueText == "public"))
            {
                var methodSymbol = model.GetDeclaredSymbol(methodDecl)!;
                var methodAuthorize = ExtractMethodAuthorizeMetadata(methodSymbol);
                var responseTypes = ExtractResponseTypes(methodSymbol);
                var methodApiExplorerSettings = ExtractMethodApiExplorerSettings(methodSymbol);
                var httpMethodInfo = GetHttpMethodInfo(methodSymbol);

                // 合并类和方法级的授权（方法级覆盖类级）
                var effectiveAuthorize = MergeAuthorizeMetadata(groupAuthorize, methodAuthorize);

                // 合并类和方法级的ApiExplorerSettings（方法级覆盖类级）
                var effectiveApiExplorerSettings = MergeApiExplorerSettings(
                    groupApiExplorerSettings,
                    methodApiExplorerSettings
                );

                if (httpMethodInfo != null)
                {
                    endpointMethods.Add(new EndpointMethod
                    {
                        Name = methodDecl.Identifier.ValueText,
                        HttpMethod = httpMethodInfo.Value.HttpMethod,
                        RouteTemplate = GetRouteTemplate(methodSymbol, httpMethodInfo.Value.HttpMethod),
                        FilterType = GetFilterType(methodSymbol, httpMethodInfo.Value.HttpMethod),
                        Authorize = effectiveAuthorize,
                        ResponseTypes = responseTypes,
                        ApiExplorerSettings = effectiveApiExplorerSettings
                    });
                }
            }
        }

        return new EndpointGroupClass
        {
            Namespace = classSymbol.ContainingNamespace.ToString(),
            ClassName = classDecl.Identifier.ValueText,
            RoutePrefix = routePrefix,
            Name = groupName,
            FilterType = groupFilterType,
            Authorize = groupAuthorize,
            EndpointMethods = endpointMethods
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
        int? order = null;

        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg.Key == "IgnoreApi" && namedArg.Value.Value is bool ignore)
                ignoreApi = ignore;

            if (namedArg.Key == "GroupName" && namedArg.Value.Value is string name)
                groupName = name;

            if (namedArg.Key == "Order" && namedArg.Value.Value is int ord)
                order = ord;
        }

        if (ignoreApi == null && groupName == null && order == null)
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

        return null;
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
            if (attr.AttributeClass?.Name == "AuthorizeAttribute")
            {
                return ExtractAuthorizeMetadata(attr);
            }
            if (attr.AttributeClass?.Name == "AllowAnonymousAttribute")
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