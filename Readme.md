# MiniController

MiniController 是一个基于 Roslyn Source Generator 的 .NET 8/Standard 2.0 端点自动注册工具。它通过自定义特性（如 `MiniControllerAttribute`）自动生成分组端点注册扩展方法，简化 API 路由、授权、过滤器和响应类型的声明与维护。

## 特性

- 自动识别带有 `MiniControllerAttribute` 的类，生成分组路由注册代码。
- 支持分组和方法级别的授权（`AuthorizeAttribute`、`AllowAnonymousAttribute`）。
- 支持分组和方法级别的 API Explorer 设置（`ApiExplorerSettingsAttribute`）。
- 支持响应类型声明（`ProducesResponseTypeAttribute`）。
- 支持端点过滤器（`FilterType`）。
- 自动生成 `MapMiniController` 扩展方法，一键注册所有 MiniController。

## 快速开始

1. **安装依赖**

   项目依赖于 .NET 8 或 .NET Standard 2.0，确保你的项目环境满足要求。

2. **定义 MiniController**
``` csharp
MiniController("/api/demo", Name = "DemoGroup")] 
public static class DemoController
 { 
    [HttpGet("hello")] 
    [ProducesResponseType(typeof(string), 200)] 
    public static IResult Hello() => Results.Ok("Hello World"); 
}
```


3. **自动生成扩展方法**

编译后，Source Generator 会自动生成如下扩展方法：

- `DemoControllerExtensions.MapDemoController`
- `MiniControllerExtensions.MapMiniController`

4. **在应用启动时注册所有端点**
``` csharp
app.MapMiniController();
```

## 主要依赖

- `Microsoft.CodeAnalysis`
- `Microsoft.AspNetCore.Builder`
- `Microsoft.AspNetCore.Http`

## 进阶用法

- 支持分组和方法级别的授权、过滤器、API Explorer 设置。
- 支持多种 HTTP 方法（GET、POST、PUT、DELETE、PATCH、HEAD、OPTIONS）。
- 支持自定义响应类型和内容类型。

## 贡献与反馈

如有建议或问题，欢迎提交 Issue 或 PR。

---

**自动生成声明** 
本项目部分代码由 Source Generator 自动生成，请勿手动修改生成文件。
