using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Concurrent;
using System.Text;

namespace MiniController;

[Generator(LanguageNames.CSharp)]
public class EndpointGenerator : IIncrementalGenerator
{
    // 使用静态只读集合提高性能
    private static readonly HashSet<string> HttpAttributeNames = new(StringComparer.Ordinal)
    {
        "HttpGetAttribute", "HttpPostAttribute", "HttpPutAttribute",
        "HttpDeleteAttribute", "HttpPatchAttribute", "HttpHeadAttribute", "HttpOptionsAttribute"
    };

    private static readonly string[] HttpMethodPrefixes =
    {
        "Get", "Post", "Put", "Delete", "Patch", "Head", "Options"
    };

    // 优化缓存键的生成，避免字符串拼接
    private static readonly ConcurrentDictionary<string, string> RouteNameCache = new();
    private static readonly ConcurrentDictionary<(string methodName, string prefixes), string> KebabCaseCache = new();

    private const string MiniControllerAttributeName = "MiniControllerAttribute";
    private const string AuthorizeAttributeName = "AuthorizeAttribute";
    private const string AllowAnonymousAttributeName = "AllowAnonymousAttribute";
    private const string ApiExplorerSettingsAttributeName = "ApiExplorerSettingsAttribute";
    private const string ProducesResponseTypeAttributeName = "ProducesResponseTypeAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        //Debugger.Launch(); // 启用调试器
#endif
        var endpointGroupProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => IsEndpointGroupClass(s),
                transform: (ctx, _) => GetEndpointGroupClass(ctx))
            .WithTrackingName("EndpointGroupProvider")
            .Where(c => c is not null);

        var endpointGroupsCollection = endpointGroupProvider.Collect();

        var combinedProvider = endpointGroupProvider.Combine(endpointGroupsCollection);

        context.RegisterImplementationSourceOutput(
            endpointGroupProvider,
            (spc, endpointGroup) => GenerateEndpointRegistration(spc, endpointGroup!)
        );

        context.RegisterImplementationSourceOutput(
            combinedProvider,
            (spc, tuple) => GenerateMiniControllerRegistration(spc, tuple.Right.Select(e => (e.Namespace, e.ClassName)).ToList())
        );
    }

    private static bool IsEndpointGroupClass(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDecl)
            return false;

        foreach (var attrList in classDecl.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var attrName = attr.Name.ToString().AsSpan();
                if (attrName.Equals("MiniController".AsSpan(), StringComparison.Ordinal) ||
                    attrName.Equals("MiniControllerAttribute".AsSpan(), StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private EndpointGroupClass? GetEndpointGroupClass(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var model = context.SemanticModel;
        var classSymbol = model.GetDeclaredSymbol(classDecl);

        if (classSymbol == null)
        {
            return null;
        }

        var controllerMetadata = ExtractControllerMetadata(classSymbol, classDecl);
        if (string.IsNullOrEmpty(controllerMetadata.RoutePrefix))
            return null;

        var endpointMethods = CollectEndpointMethods(classDecl, model, controllerMetadata);

        if (endpointMethods.Count == 0)
            return null;

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

    private ControllerMetadata ExtractControllerMetadata(ISymbol classSymbol, ClassDeclarationSyntax classDecl)
    {
        var metadata = new ControllerMetadata();

        foreach (var attr in classSymbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.Name;
            if (attrName == null) continue;

            switch (attrName)
            {
                case MiniControllerAttributeName:
                    metadata.RoutePrefix = ExtractRoutePrefix(attr, classDecl);
                    metadata.GroupName = ExtractGroupName(attr);
                    metadata.FilterType = ExtractControllerFilterType(attr);
                    break;
                case AuthorizeAttributeName:
                    metadata.Authorize = ExtractAuthorizeMetadata(attr);
                    break;
                case AllowAnonymousAttributeName:
                    metadata.Authorize = new AuthorizeMetadata { AllowAnonymous = true };
                    break;
                case ApiExplorerSettingsAttributeName:
                    metadata.ApiExplorerSettings = ExtractApiExplorerSettings(attr);
                    break;
            }
        }

        return metadata;
    }

    private string ExtractRoutePrefix(AttributeData attr, ClassDeclarationSyntax classDecl)
    {
        if (attr.ConstructorArguments.Length > 0 &&
            attr.ConstructorArguments[0].Value is string prefix)
        {
            return prefix;
        }

        var className = classDecl.Identifier.ValueText;
        return RouteNameCache.GetOrAdd(className, static name =>
        {
            var processedName = name.ToLowerInvariant()
                .Replace("service", "")
                .Replace("controller", "")
                .Replace("endpoint", "");
            return $"/api/{processedName}";
        });
    }

    private static string? ExtractGroupName(AttributeData attr)
    {
        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg.Key == "Name" && namedArg.Value.Value is string name)
                return name;
        }

        return null;
    }

    private static string? ExtractControllerFilterType(AttributeData attr)
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

    private List<EndpointMethod> CollectEndpointMethods(
        ClassDeclarationSyntax classDecl,
        SemanticModel model,
        ControllerMetadata controllerMetadata)
    {
        var endpointMethods = new List<EndpointMethod>();

        var publicMethods = classDecl.Members
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Modifiers.Any(mod => mod.ValueText == "public"));

        foreach (var methodDecl in publicMethods)
        {
            var methodSymbol = model.GetDeclaredSymbol(methodDecl);
            if (methodSymbol == null) continue;

            var endpointMethod = ProcessEndpointMethod(methodSymbol, methodDecl, controllerMetadata);
            if (endpointMethod != null)
            {
                endpointMethods.Add(endpointMethod);
            }
        }

        return endpointMethods;
    }

    private EndpointMethod? ProcessEndpointMethod(
        ISymbol methodSymbol,
        MethodDeclarationSyntax methodDecl,
        ControllerMetadata controllerMetadata)
    {
        var httpMethodInfo = GetHttpMethodInfo(methodSymbol);
        if (httpMethodInfo == null)
            return null;

        var methodAuthorize = ExtractMethodAuthorizeMetadata(methodSymbol);
        var responseTypes = ExtractResponseTypes(methodSymbol);
        var methodApiExplorerSettings = ExtractMethodApiExplorerSettings(methodSymbol);

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

    private static ApiExplorerSettingsMetadata? ExtractMethodApiExplorerSettings(ISymbol methodSymbol)
    {
        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == ApiExplorerSettingsAttributeName)
            {
                return ExtractApiExplorerSettings(attr);
            }
        }
        return null;
    }

    private static ApiExplorerSettingsMetadata? ExtractApiExplorerSettings(AttributeData attr)
    {
        bool? ignoreApi = null;
        string? groupName = null;

        foreach (var namedArg in attr.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "IgnoreApi" when namedArg.Value.Value is bool ignore:
                    ignoreApi = ignore;
                    break;
                case "GroupName" when namedArg.Value.Value is string name:
                    groupName = name;
                    break;
            }
        }

        if (ignoreApi == null && groupName == null)
            return null;

        return new ApiExplorerSettingsMetadata
        {
            IgnoreApi = ignoreApi,
            GroupName = groupName
        };
    }

    private static ApiExplorerSettingsMetadata? MergeApiExplorerSettings(
        ApiExplorerSettingsMetadata? groupSettings,
        ApiExplorerSettingsMetadata? methodSettings)
    {
        if (methodSettings == null) return groupSettings;
        if (groupSettings == null) return methodSettings;

        return new ApiExplorerSettingsMetadata
        {
            IgnoreApi = methodSettings.IgnoreApi ?? groupSettings.IgnoreApi,
            GroupName = methodSettings.GroupName ?? groupSettings.GroupName,
        };
    }

    private (string HttpMethod, string Template)? GetHttpMethodInfo(ISymbol methodSymbol)
    {
        foreach (var attr in methodSymbol.GetAttributes())
        {
            var attributeName = attr.AttributeClass?.Name;
            if (attributeName != null && IsHttpMethodAttribute(attributeName))
            {
                var httpMethod = attributeName.Replace("Attribute", "").Replace("Http", "");
                var template = string.Empty;

                if (attr.ConstructorArguments.Length > 0 &&
                    attr.ConstructorArguments[0].Value is string templateValue)
                {
                    template = templateValue;
                }

                return (httpMethod, template);
            }
        }

        return InferHttpMethodFromName(methodSymbol.Name);
    }

    private static bool IsHttpMethodAttribute(string attributeName)
    {
        return HttpAttributeNames.Contains(attributeName);
    }

    private (string HttpMethod, string Template)? InferHttpMethodFromName(string methodName)
    {
        if (string.IsNullOrEmpty(methodName))
            return null;

        var span = methodName.AsSpan();

        foreach (var prefix in HttpMethodPrefixes)
        {
            if (span.StartsWith(prefix.AsSpan(), StringComparison.Ordinal))
            {
                var template = InferRouteFromMethodName(methodName, prefix);
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
            var template = InferRouteFromMethodName(methodName, new[] { "Post", createPrefix, addPrefix });
            return ("Post", template);
        }

        if (span.StartsWith(updatePrefix.AsSpan(), StringComparison.Ordinal))
        {
            var template = InferRouteFromMethodName(methodName, new[] { "Put", updatePrefix });
            return ("Put", template);
        }

        if (span.StartsWith(removePrefix.AsSpan(), StringComparison.Ordinal))
        {
            var template = InferRouteFromMethodName(methodName, new[] { "Delete", removePrefix });
            return ("Delete", template);
        }

        return null;
    }

    private string InferRouteFromMethodName(string methodName, string[] prefixes)
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

    private string InferRouteFromMethodName(string methodName, string prefix)
    {
        return InferRouteFromMethodName(methodName, new[] { prefix });
    }

    private static string ConvertToKebabCase(string input)
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

        // 预估 StringBuilder 容量以提高性能
        var estimatedLength = input.Length + (input.Length / 3); // 约增加 33% 用于连字符
        var result = new StringBuilder(estimatedLength);

        // 将首字符转换为小写
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

    private string GetRouteTemplate(ISymbol methodSymbol, string httpMethod)
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
            return InferRouteFromMethodName(methodName, prefixes);
        }

        return string.Empty;
    }

    private static string? GetFilterType(ISymbol methodSymbol, string httpMethod)
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

    private static AuthorizeMetadata? ExtractMethodAuthorizeMetadata(ISymbol methodSymbol)
    {
        foreach (var attr in methodSymbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.Name;
            switch (attrName)
            {
                case AuthorizeAttributeName:
                    return ExtractAuthorizeMetadata(attr);
                case AllowAnonymousAttributeName:
                    return new AuthorizeMetadata { AllowAnonymous = true };
            }
        }
        return null;
    }

    private static AuthorizeMetadata? ExtractAuthorizeMetadata(AttributeData attr)
    {
        string? policy = null;
        string? roles = null;
        string? authenticationSchemes = null;

        if (attr.ConstructorArguments.Length > 0 &&
            attr.ConstructorArguments[0].Value is string policyValue)
        {
            policy = policyValue;
        }

        foreach (var namedArg in attr.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "Policy" when namedArg.Value.Value is string p:
                    policy = p;
                    break;
                case "Roles" when namedArg.Value.Value is string r:
                    roles = r;
                    break;
                case "AuthenticationSchemes" when namedArg.Value.Value is string s:
                    authenticationSchemes = s;
                    break;
            }
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

    private static AuthorizeMetadata? MergeAuthorizeMetadata(AuthorizeMetadata? group, AuthorizeMetadata? method)
    {
        if (method == null) return group;
        if (group == null) return method;

        return new AuthorizeMetadata
        {
            Policy = method.Policy ?? group.Policy,
            Roles = method.Roles ?? group.Roles,
            AuthenticationSchemes = method.AuthenticationSchemes ?? group.AuthenticationSchemes,
            AllowAnonymous = method.AllowAnonymous || group.AllowAnonymous
        };
    }

    private static List<ResponseTypeMetadata> ExtractResponseTypes(ISymbol methodSymbol)
    {
        var responseTypes = new List<ResponseTypeMetadata>();

        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == ProducesResponseTypeAttributeName)
            {
                var statusCode = 200;
                string? typeName = null;
                string? contentType = null;

                if (attr.ConstructorArguments.Length > 0 &&
                    attr.ConstructorArguments[0].Value is int code)
                {
                    statusCode = code;
                }

                if (attr.ConstructorArguments.Length > 1 &&
                    attr.ConstructorArguments[1].Value is INamedTypeSymbol typeSymbol)
                {
                    typeName = typeSymbol.ToDisplayString();
                }

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
        ValidateEndpoints(context, endpointGroup);

        var methodCount = endpointGroup.EndpointMethods.Count;
        var estimatedCapacity =
            500 +
            (methodCount * 200) +
            (endpointGroup.RoutePrefix?.Length ?? 0) * methodCount +
            100;

        var source = new StringBuilder(estimatedCapacity);

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

        foreach (var method in endpointGroup.EndpointMethods)
        {
            BuildMethodRegistration(source, method, endpointGroup.ClassName);
        }

        source.AppendLine();
        source.AppendLine("            return builder;");
        source.AppendLine("        }");
        source.AppendLine("    }");
        source.AppendLine("}");

        context.AddSource($"{endpointGroup.ClassName}Extensions.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
    }

    private static void BuildMethodRegistration(StringBuilder source, EndpointMethod method, string className)
    {
        if (method == null || string.IsNullOrEmpty(method.Name))
        {
            return; // 跳过无效的方法
        }

        source.Append($"            group.Map{method.HttpMethod}(\"{method.RouteTemplate}\", {className}.{method.Name})");

        if (method.Authorize != null)
        {
            var authorizeCall = BuildAuthorizeCall(method.Authorize);
            if (!string.IsNullOrEmpty(authorizeCall))
            {
                source.Append(authorizeCall);
            }
        }

        if (method.ApiExplorerSettings != null)
        {
            var apiExplorerCall = BuildApiExplorerSettingsCall(method.ApiExplorerSettings);
            if (!string.IsNullOrEmpty(apiExplorerCall))
            {
                source.Append(apiExplorerCall);
            }
        }
        else
        {
            source.Append(".WithOpenApi()");
        }

        // 添加响应类型
        foreach (var responseType in method.ResponseTypes ?? Enumerable.Empty<ResponseTypeMetadata>())
        {
            AppendProducesCall(source, responseType);
        }

        // 添加过滤器
        if (!string.IsNullOrEmpty(method.FilterType))
        {
            source.Append($".AddEndpointFilter<{method.FilterType}>()");
        }

        source.AppendLine(";");
    }

    private static void AppendProducesCall(StringBuilder source, ResponseTypeMetadata responseType)
    {
        if (responseType == null) return;

        if (!string.IsNullOrEmpty(responseType.TypeName))
        {
            source.Append($".Produces<{responseType.TypeName}>({responseType.StatusCode}");
            if (!string.IsNullOrEmpty(responseType.ContentType))
            {
                source.Append($", \"{responseType.ContentType}\"");
            }
            source.Append(')');
        }
        else
        {
            source.Append($".Produces({responseType.StatusCode}");
            if (!string.IsNullOrEmpty(responseType.ContentType))
            {
                source.Append($", \"{responseType.ContentType}\"");
            }
            source.Append(')');
        }
    }

    private void GenerateMiniControllerRegistration(SourceProductionContext context, List<(string nameSpace, string className)> ClassInfoList)
    {
        var estimatedCapacity = 300 + (ClassInfoList.Count * 50);
        var Extension = new StringBuilder(estimatedCapacity);

        Extension.AppendLine("// <auto-generated>");
        Extension.AppendLine("// 由EndpointGenerator自动生成，请勿修改");
        Extension.AppendLine("// </auto-generated>");
        Extension.AppendLine();
        Extension.AppendLine("using Microsoft.AspNetCore.Builder;");
        Extension.AppendLine("using Microsoft.AspNetCore.Http;");

        var uniqueNamespaces = ClassInfoList.Select(c => c.nameSpace).Distinct();
        foreach (var ns in uniqueNamespaces)
        {
            Extension.AppendLine($"using {ns};");
        }

        Extension.AppendLine();
        Extension.AppendLine("namespace Microsoft.AspNetCore.Builder");
        Extension.AppendLine("{");
        Extension.AppendLine("    public static class MiniControllerExtensions");
        Extension.AppendLine("    {");
        Extension.AppendLine("        public static IEndpointRouteBuilder MapMiniController(this IEndpointRouteBuilder builder)");
        Extension.AppendLine("        {");

        foreach (var classInfo in ClassInfoList)
        {
            Extension.AppendLine($"            builder.Map{classInfo.className}();");
        }

        Extension.AppendLine("            return builder;");
        Extension.AppendLine("        }");
        Extension.AppendLine("    }");
        Extension.AppendLine("}");

        context.AddSource("MiniControllerExtensions.g.cs", SourceText.From(Extension.ToString(), Encoding.UTF8));
    }

    private static string BuildAuthorizeCall(AuthorizeMetadata metadata)
    {
        if (metadata?.AllowAnonymous == true)
        {
            return ".AllowAnonymous()";
        }

        if (metadata == null)
            return string.Empty;

        var arguments = new List<string>(3);

        if (!string.IsNullOrEmpty(metadata.Policy))
            arguments.Add($"\"{metadata.Policy}\"");

        if (!string.IsNullOrEmpty(metadata.Roles))
            arguments.Add($"Roles: \"{metadata.Roles}\"");

        if (!string.IsNullOrEmpty(metadata.AuthenticationSchemes))
            arguments.Add($"AuthenticationSchemes: \"{metadata.AuthenticationSchemes}\"");

        if (arguments.Count == 0)
            return ".RequireAuthorization()";

        return $".RequireAuthorization({string.Join(", ", arguments)})";
    }

    private static string BuildApiExplorerSettingsCall(ApiExplorerSettingsMetadata settings)
    {
        if (settings == null) return string.Empty;

        var result = new StringBuilder(64);

        if (settings.IgnoreApi == true)
        {
            result.Append(".ExcludeFromDescription()");
        }
        else
        {
            result.Append(".WithOpenApi()");
        }

        if (!string.IsNullOrEmpty(settings.GroupName))
        {
            result.Append($".WithTags(\"{settings.GroupName}\")");
        }

        return result.ToString();
    }

    private static void ValidateEndpoints(SourceProductionContext context, EndpointGroupClass endpointGroup)
    {
        if (endpointGroup?.EndpointMethods == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                CreateDiagnosticDescriptor("MC002", "无效的端点组", "端点组或其方法列表为空", DiagnosticSeverity.Error),
                Location.None));
            return;
        }

        var routeMap = new Dictionary<string, List<string>>(endpointGroup.EndpointMethods.Count);

        foreach (var method in endpointGroup.EndpointMethods)
        {
            if (method == null) continue;

            var key = $"{method.HttpMethod}:{method.RouteTemplate}";
            if (!routeMap.TryGetValue(key, out var methods))
            {
                routeMap[key] = new List<string> { method.Name };
            }
            else
            {
                methods.Add(method.Name);
                context.ReportDiagnostic(Diagnostic.Create(
                    CreateDiagnosticDescriptor("MC001", "路由冲突",
                        $"控制器 {endpointGroup.ClassName} 中的方法 {string.Join(", ", methods)} 具有相同的路由 {key}",
                        DiagnosticSeverity.Warning),
                    Location.None));
            }
        }
    }

    private static DiagnosticDescriptor CreateDiagnosticDescriptor(string id, string title, string messageFormat, DiagnosticSeverity severity)
    {
        return new DiagnosticDescriptor(
            id,
            title,
            messageFormat,
            "MiniController",
            severity,
            isEnabledByDefault: true,
            description: $"MiniController 源码生成器诊断：{title}"
        );
    }
}

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

    public List<ResponseTypeMetadata> ResponseTypes { get; set; } = new();

    public ApiExplorerSettingsMetadata? ApiExplorerSettings { get; set; }
}

public class AuthorizeMetadata
{
    public string? Policy { get; set; }

    public string? Roles { get; set; }

    public string? AuthenticationSchemes { get; set; }

    public bool AllowAnonymous { get; set; } = false;
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

public class ControllerMetadata
{
    public string RoutePrefix { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public string? FilterType { get; set; }
    public AuthorizeMetadata? Authorize { get; set; }
    public ApiExplorerSettingsMetadata? ApiExplorerSettings { get; set; }
}