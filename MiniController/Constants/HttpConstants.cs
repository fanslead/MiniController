using System;
using System.Collections.Generic;

namespace MiniController.Constants;

public static class HttpConstants
{
    // 使用静态只读集合提高性能
    public static readonly HashSet<string> HttpAttributeNames = new(StringComparer.Ordinal)
    {
        "HttpGetAttribute", "HttpPostAttribute", "HttpPutAttribute",
        "HttpDeleteAttribute", "HttpPatchAttribute", "HttpHeadAttribute", "HttpOptionsAttribute"
    };

    public static readonly string[] HttpMethodPrefixes =
    {
        "Get", "Post", "Put", "Delete", "Patch", "Head", "Options"
    };

    public const string MiniControllerAttributeName = "MiniControllerAttribute";
    public const string AuthorizeAttributeName = "AuthorizeAttribute";
    public const string AllowAnonymousAttributeName = "AllowAnonymousAttribute";
    public const string ApiExplorerSettingsAttributeName = "ApiExplorerSettingsAttribute";
    public const string ProducesResponseTypeAttributeName = "ProducesResponseTypeAttribute";
}