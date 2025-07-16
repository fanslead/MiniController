using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using MiniController.Models;
using MiniController.Constants;

namespace MiniController.Helpers;

public static class AttributeHelper
{
    public static string? ExtractControllerFilterType(AttributeData attr)
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

    public static AuthorizeMetadata? ExtractAuthorizeMetadata(AttributeData attr)
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

    public static ApiExplorerSettingsMetadata? ExtractApiExplorerSettings(AttributeData attr)
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

    public static List<ResponseTypeMetadata> ExtractResponseTypes(ISymbol methodSymbol)
    {
        var responseTypes = new List<ResponseTypeMetadata>();

        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == HttpConstants.ProducesResponseTypeAttributeName)
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

    public static AuthorizeMetadata? MergeAuthorizeMetadata(AuthorizeMetadata? group, AuthorizeMetadata? method)
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

    public static ApiExplorerSettingsMetadata? MergeApiExplorerSettings(
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

    public static bool IsHttpMethodAttribute(string attributeName)
    {
        return HttpConstants.HttpAttributeNames.Contains(attributeName);
    }
}