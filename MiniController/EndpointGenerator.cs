using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using MiniController.Constants;
using MiniController.Extensions;
using MiniController.Helpers;
using MiniController.Models;
using MiniController.Generators;

namespace MiniController;

[Generator(LanguageNames.CSharp)]
public class EndpointGenerator : IIncrementalGenerator
{
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

        // 为每个端点组生成单独的扩展类
        context.RegisterImplementationSourceOutput(
            endpointGroupProvider,
            (spc, endpointGroup) => SourceCodeGenerator.GenerateEndpointRegistration(spc, endpointGroup!)
        );

        // 生成统一的MiniController注册扩展和DI注册
        context.RegisterImplementationSourceOutput(
            endpointGroupsCollection,
            (spc, endpointGroups) =>
            {
                var groups = endpointGroups.Where(e => e != null).ToList();
                SourceCodeGenerator.GenerateMiniControllerRegistration(spc, groups.Select(e => (e!.Namespace, e.ClassName)).ToList());
                SourceCodeGenerator.GenerateDependencyInjectionRegistration(spc, groups!);
            }
        );
    }

    private static bool IsEndpointGroupClass(SyntaxNode node)
    {
        // 对于类声明语法节点，我们总是返回true，让transform阶段进行语义检查
        // 这是因为继承的attribute检测需要语义模型，无法在语法层面完成
        return node is ClassDeclarationSyntax;
    }

    private EndpointGroupClass? GetEndpointGroupClass(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var model = context.SemanticModel;
        var classSymbol = model.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;

        if (classSymbol == null)
        {
            return null;
        }

        // 检查类是否具有MiniControllerAttribute（包括继承的）
        if (!HasMiniControllerAttribute(classSymbol))
        {
            return null;
        }

        var controllerMetadata = ExtractControllerMetadata(classSymbol, classDecl);
        if (string.IsNullOrEmpty(controllerMetadata.RoutePrefix))
            return null;

        var endpointMethods = CollectEndpointMethods(classDecl, model, controllerMetadata);

        if (endpointMethods.Count == 0)
            return null;

        // 检查类是否为静态类
        var isStatic = classDecl.Modifiers.Any(m => m.ValueText == "static");

        return new EndpointGroupClass
        {
            Namespace = classSymbol.ContainingNamespace.ToString(),
            ClassName = classDecl.Identifier.ValueText,
            RoutePrefix = controllerMetadata.RoutePrefix,
            FilterType = controllerMetadata.FilterType,
            Authorize = controllerMetadata.Authorize,
            EndpointMethods = endpointMethods,
            IsStatic = isStatic
        };
    }

    /// <summary>
    /// 检查类是否具有MiniControllerAttribute（包括继承的）
    /// </summary>
    private static bool HasMiniControllerAttribute(INamedTypeSymbol classSymbol)
    {
        // 检查当前类及其所有基类是否有MiniControllerAttribute
        var currentType = classSymbol;
        while (currentType != null)
        {
            foreach (var attr in currentType.GetAttributes())
            {
                var attrName = attr.AttributeClass?.Name;
                if (attrName == HttpConstants.MiniControllerAttributeName)
                {
                    return true;
                }
            }
            currentType = currentType.BaseType;
        }
        return false;
    }

    private ControllerMetadata ExtractControllerMetadata(ISymbol classSymbol, ClassDeclarationSyntax classDecl)
    {
        var metadata = new ControllerMetadata();

        // 检查当前类及其所有基类的属性
        var currentType = classSymbol as INamedTypeSymbol;
        while (currentType != null)
        {
            foreach (var attr in currentType.GetAttributes())
            {
                var attrName = attr.AttributeClass?.Name;
                if (attrName == null) continue;

                switch (attrName)
                {
                    case HttpConstants.MiniControllerAttributeName:
                        // 只有当RoutePrefix还未设置时才设置（优先使用派生类的设置）
                        if (string.IsNullOrEmpty(metadata.RoutePrefix))
                        {
                            metadata.RoutePrefix = ExtractRoutePrefix(attr, classDecl, classSymbol);
                        }
                        // 只有当FilterType还未设置时才设置（优先使用派生类的设置）
                        if (metadata.FilterType == null)
                        {
                            metadata.FilterType = AttributeHelper.ExtractControllerFilterType(attr);
                        }
                        break;
                    case HttpConstants.AuthorizeAttributeName:
                        // 只有当Authorize还未设置时才设置（优先使用派生类的设置）
                        if (metadata.Authorize == null)
                        {
                            metadata.Authorize = AttributeHelper.ExtractAuthorizeMetadata(attr);
                        }
                        break;
                    case HttpConstants.AllowAnonymousAttributeName:
                        // 只有当Authorize还未设置时才设置（优先使用派生类的设置）
                        if (metadata.Authorize == null)
                        {
                            metadata.Authorize = new AuthorizeMetadata { AllowAnonymous = true };
                        }
                        break;
                    case HttpConstants.ApiExplorerSettingsAttributeName:
                        // 只有当ApiExplorerSettings还未设置时才设置（优先使用派生类的设置）
                        if (metadata.ApiExplorerSettings == null)
                        {
                            metadata.ApiExplorerSettings = AttributeHelper.ExtractApiExplorerSettings(attr);
                        }
                        break;
                }
            }
            currentType = currentType.BaseType;
        }

        return metadata;
    }

    private string ExtractRoutePrefix(AttributeData attr, ClassDeclarationSyntax classDecl, ISymbol classSymbol)
    {
        string? routeTemplate = null;

        if (attr.ConstructorArguments.Length > 0 &&
            attr.ConstructorArguments[0].Value is string prefix)
        {
            routeTemplate = prefix;
        }

        var className = classDecl.Identifier.ValueText;

        return StringExtensions.GetOrAddRouteFromClassName(className, classSymbol, routeTemplate);
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
        var responseTypes = AttributeHelper.ExtractResponseTypes(methodSymbol);
        var methodApiExplorerSettings = ExtractMethodApiExplorerSettings(methodSymbol);

        var effectiveAuthorize = AttributeHelper.MergeAuthorizeMetadata(controllerMetadata.Authorize, methodAuthorize);
        var effectiveApiExplorerSettings = AttributeHelper.MergeApiExplorerSettings(
            controllerMetadata.ApiExplorerSettings,
            methodApiExplorerSettings
        );

        // 处理路由模板，支持 [action] 占位符
        var routeTemplate = GetEffectiveRouteTemplate(
            controllerMetadata.RoutePrefix, 
            httpMethodInfo.Value.Template,
            methodDecl.Identifier.ValueText,
            httpMethodInfo.Value.HttpMethod,
            methodSymbol);

        // 提取方法参数信息
        var parameters = ExtractMethodParameters(methodSymbol);
        
        // 检查是否为静态方法
        var isStatic = methodDecl.Modifiers.Any(m => m.ValueText == "static");

        return new EndpointMethod
        {
            Name = methodDecl.Identifier.ValueText,
            HttpMethod = httpMethodInfo.Value.HttpMethod,
            RouteTemplate = routeTemplate,
            FilterType = HttpMethodHelper.GetFilterType(methodSymbol, httpMethodInfo.Value.HttpMethod),
            Authorize = effectiveAuthorize,
            ResponseTypes = responseTypes,
            ApiExplorerSettings = effectiveApiExplorerSettings,
            Parameters = parameters,
            ReturnType = ExtractReturnType(methodSymbol),
            IsStatic = isStatic
        };
    }

    /// <summary>
    /// 提取方法参数信息
    /// </summary>
    private List<MethodParameterInfo> ExtractMethodParameters(ISymbol methodSymbol)
    {
        var parameters = new List<MethodParameterInfo>();
        
        if (methodSymbol is IMethodSymbol method)
        {
            foreach (var param in method.Parameters)
            {
                var paramInfo = new MethodParameterInfo
                {
                    Name = param.Name,
                    Type = param.Type.ToDisplayString()
                };

                // 检查参数特性
                foreach (var attr in param.GetAttributes())
                {
                    var attrName = attr.AttributeClass?.Name;
                    switch (attrName)
                    {
                        case "FromServicesAttribute":
                            paramInfo.IsFromServices = true;
                            break;
                        case "FromRouteAttribute":
                            paramInfo.IsFromRoute = true;
                            break;
                        case "FromQueryAttribute":
                            paramInfo.IsFromQuery = true;
                            break;
                        case "FromBodyAttribute":
                            paramInfo.IsFromBody = true;
                            break;
                        case "FromHeaderAttribute":
                            paramInfo.IsFromHeader = true;
                            break;
                        case "FromFormAttribute":
                            paramInfo.IsFromForm = true;
                            break;
                    }
                }

                parameters.Add(paramInfo);
            }
        }

        return parameters;
    }

    /// <summary>
    /// 提取方法返回类型
    /// </summary>
    private string ExtractReturnType(ISymbol methodSymbol)
    {
        if (methodSymbol is IMethodSymbol method)
        {
            return method.ReturnType.ToDisplayString();
        }
        return "IResult";
    }

    /// <summary>
    /// 获取有效的路由模板，处理控制器级别的 [action] 占位符
    /// </summary>
    private string GetEffectiveRouteTemplate(string controllerRoutePrefix, string methodRouteTemplate, string methodName, string httpMethod, ISymbol methodSymbol)
    {
        // 如果控制器路由前缀包含 [action] 占位符，方法级别的路由模板应该只包含额外的参数
        if (!string.IsNullOrEmpty(controllerRoutePrefix) && controllerRoutePrefix.Contains("[action]"))
        {
            // 对于包含 [action] 的控制器路由，只返回明确指定的HTTP属性路由模板
            // [action]的解析由RouteTemplateResolver.ResolveActionTemplate处理
            foreach (var attr in methodSymbol.GetAttributes())
            {
                var attributeName = attr.AttributeClass?.Name;
                if (attributeName != null && AttributeHelper.IsHttpMethodAttribute(attributeName))
                {
                    if (attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is string templateValue)
                    {
                        return templateValue;
                    }
                    // 如果HTTP属性没有指定路由模板，对于[action]模式返回空字符串
                    return string.Empty;
                }
            }
            // 如果没有HTTP属性，返回空字符串
            return string.Empty;
        }
        
        // 检查是否有明确指定的HTTP属性路由模板
        foreach (var attr in methodSymbol.GetAttributes())
        {
            var attributeName = attr.AttributeClass?.Name;
            if (attributeName != null && AttributeHelper.IsHttpMethodAttribute(attributeName))
            {
                if (attr.ConstructorArguments.Length > 0 &&
                    attr.ConstructorArguments[0].Value is string templateValue)
                {
                    return templateValue;
                }
                // 如果HTTP属性没有指定路由模板，且控制器路由是自定义的完整路径（不包含占位符），返回空字符串
                if (!string.IsNullOrEmpty(controllerRoutePrefix) && 
                    !controllerRoutePrefix.Contains("[controller]") && 
                    !controllerRoutePrefix.Contains("[area]"))
                {
                    return string.Empty;
                }
            }
        }
        
        // 如果控制器路由前缀不包含 [action]，则使用原有逻辑
        return HttpMethodHelper.GetRouteTemplate(methodSymbol, httpMethod);
    }

    private static ApiExplorerSettingsMetadata? ExtractMethodApiExplorerSettings(ISymbol methodSymbol)
    {
        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == HttpConstants.ApiExplorerSettingsAttributeName)
            {
                return AttributeHelper.ExtractApiExplorerSettings(attr);
            }
        }
        return null;
    }

    private (string HttpMethod, string Template)? GetHttpMethodInfo(ISymbol methodSymbol)
    {
        foreach (var attr in methodSymbol.GetAttributes())
        {
            var attributeName = attr.AttributeClass?.Name;
            if (attributeName != null && AttributeHelper.IsHttpMethodAttribute(attributeName))
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

        return HttpMethodHelper.InferHttpMethodFromName(methodSymbol.Name);
    }

    private static AuthorizeMetadata? ExtractMethodAuthorizeMetadata(ISymbol methodSymbol)
    {
        foreach (var attr in methodSymbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.Name;
            switch (attrName)
            {
                case HttpConstants.AuthorizeAttributeName:
                    return AttributeHelper.ExtractAuthorizeMetadata(attr);
                case HttpConstants.AllowAnonymousAttributeName:
                    return new AuthorizeMetadata { AllowAnonymous = true };
            }
        }
        return null;
    }
}