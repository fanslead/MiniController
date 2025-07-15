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

        var combinedProvider = endpointGroupProvider.Combine(endpointGroupsCollection);

        context.RegisterImplementationSourceOutput(
            endpointGroupProvider,
            (spc, endpointGroup) => SourceCodeGenerator.GenerateEndpointRegistration(spc, endpointGroup!)
        );

        context.RegisterImplementationSourceOutput(
            combinedProvider,
            (spc, tuple) => SourceCodeGenerator.GenerateMiniControllerRegistration(spc, tuple.Right.Select(e => (e.Namespace, e.ClassName)).ToList())
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
                case HttpConstants.MiniControllerAttributeName:
                    metadata.RoutePrefix = ExtractRoutePrefix(attr, classDecl);
                    metadata.GroupName = AttributeHelper.ExtractGroupName(attr);
                    metadata.FilterType = AttributeHelper.ExtractControllerFilterType(attr);
                    break;
                case HttpConstants.AuthorizeAttributeName:
                    metadata.Authorize = AttributeHelper.ExtractAuthorizeMetadata(attr);
                    break;
                case HttpConstants.AllowAnonymousAttributeName:
                    metadata.Authorize = new AuthorizeMetadata { AllowAnonymous = true };
                    break;
                case HttpConstants.ApiExplorerSettingsAttributeName:
                    metadata.ApiExplorerSettings = AttributeHelper.ExtractApiExplorerSettings(attr);
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
        return StringExtensions.GetOrAddRouteFromClassName(className);
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

        return new EndpointMethod
        {
            Name = methodDecl.Identifier.ValueText,
            HttpMethod = httpMethodInfo.Value.HttpMethod,
            RouteTemplate = HttpMethodHelper.GetRouteTemplate(methodSymbol, httpMethodInfo.Value.HttpMethod),
            FilterType = HttpMethodHelper.GetFilterType(methodSymbol, httpMethodInfo.Value.HttpMethod),
            Authorize = effectiveAuthorize,
            ResponseTypes = responseTypes,
            ApiExplorerSettings = effectiveApiExplorerSettings
        };
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