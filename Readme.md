# MiniController

MiniController 是一个基于 Roslyn Source Generator 的 .NET Standard 2.0 端点自动注册工具。它通过自定义特性（如 `MiniControllerAttribute`）自动生成分组端点注册扩展方法，简化 Minimal API 路由、授权、过滤器和响应类型的声明与维护。

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NuGet Version](https://img.shields.io/nuget/v/MiniController.svg)](https://www.nuget.org/packages/MiniController)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

## 🚀 特性

- **自动端点注册**: 自动识别带有 `MiniControllerAttribute` 的类，生成分组路由注册代码
- **强大的路由模板支持**: 支持 `[area]`、`[controller]`、`[action]` 占位符，完全兼容 ASP.NET Core MVC 路由模板
- **智能命名转换**: 自动将 PascalCase 转换为 kebab-case，符合 REST API 最佳实践
- **自动后缀移除**: 智能移除类名中的 Service、Controller、Endpoint、Endpoints 后缀
- **完整的授权支持**: 支持分组和方法级别的授权（`AuthorizeAttribute`、`AllowAnonymousAttribute`）
- **API Explorer 集成**: 支持分组和方法级别的 API Explorer 设置（`ApiExplorerSettingsAttribute`）
- **响应类型声明**: 支持响应类型声明（`ProducesResponseTypeAttribute`）
- **端点过滤器**: 支持自定义端点过滤器
- **HTTP 方法推断**: 自动从方法名推断 HTTP 方法（Get、Post、Put、Delete、Patch 等）
- **一键注册**: 自动生成 `MapMiniController` 扩展方法，一行代码注册所有端点

## 📦 安装

### 通过 NuGet 包管理器Install-Package MiniController.Attributes
```
Install-Package MiniController
```
### 通过 .NET CLIdotnet add package MiniController.Attributes
```
dotnet add package MiniController
```
### 手动添加到项目文件
``` xml
<ItemGroup>
  <PackageReference Include="MiniController.Attributes" Version="1.0.2" />
  <ProjectReference Include="MiniController" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```
### nuget引用
``` xml
<ItemGroup>
  <PackageReference Include="MiniController" Version="1.0.2"  PrivateAssets="all"/>
</ItemGroup>
```
## 🏃‍♂️ 快速开始

### 1. 定义一个简单的 MiniController
``` csharp
using MiniController.Attributes;
using Microsoft.AspNetCore.Mvc;

[MiniController("/api/demo")]
public static class DemoController
{
    [HttpGet("hello")]
    [ProducesResponseType(typeof(string), 200)]
    public static IResult Hello() => Results.Ok("Hello World");

    [HttpGet("user/{id}")]
    public static IResult GetUser(int id) => Results.Ok($"User {id}");

    [HttpPost("user")]
    public static IResult CreateUser() => Results.Created("/api/demo/user/1", new { Id = 1 });
}
```
### 2. 在应用启动时注册所有端点
``` csharp
var builder = WebApplication.CreateBuilder(args);

// 添加必要的服务
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 一行代码注册所有 MiniController 端点
app.MapMiniController();

app.Run();
```
### 3. 编译后自动生成扩展方法

Source Generator 会自动生成如下扩展方法：
- `DemoControllerExtensions.MapDemoController`
- `MiniControllerExtensions.MapMiniController`

## 🛠 高级用法

### 路由模板占位符

MiniController 支持强大的路由模板语法，兼容 ASP.NET Core MVC 路由模板：

#### 1. Area 支持
``` csharp
using Microsoft.AspNetCore.Mvc;

[Area("Admin")]
[MiniController("/api/[area]/[controller]")]
public class UserEndpoints
{
    [HttpGet]
    public static IResult GetUsers()
    {
        return Results.Ok("获取所有用户 - 路由: /api/admin/user");
    }

    [HttpGet("{id}")]
    public static IResult GetUser(int id)
    {
        return Results.Ok($"获取用户 {id} - 路由: /api/admin/user/{id}");
    }
}
```
#### 2. Controller 占位符
``` csharp
[MiniController("/api/[controller]")]
public class OrderController
{
    public static IResult GetOrders() // 自动推断为 GET /api/order
    {
        return Results.Ok("订单列表");
    }

    public static IResult GetOrder(int id) // 自动推断为 GET /api/order 并添加参数
    {
        return Results.Ok($"订单 {id}");
    }
}
```
#### 3. Action 占位符
``` csharp
[MiniController("/api/v1/[controller]/[action]")]
public class ProductService
{
    public static IResult GetList() // 路由: GET /api/v1/product/list
    {
        return Results.Ok("产品列表");
    }

    public static IResult PostCreate() // 路由: POST /api/v1/product/create
    {
        return Results.Ok("创建产品");
    }

    public static IResult GetSearchByCategory(string category) // 路由: GET /api/v1/product/search-by-category
    {
        return Results.Ok($"按分类搜索: {category}");
    }
}
```

### 智能命名规则

#### 自动后缀移除
类名中的常见后缀会被自动移除：
- `ProductService` → `product`
- `UserController` → `user`  
- `OrderEndpoint` → `order`
- `DataEndpoints` → `data`

#### PascalCase 转 kebab-case
方法名和类名会自动转换：
- `GetUserProfile` → `get-user-profile`
- `SearchByCategory` → `search-by-category`
- `BatchImport` → `batch-import`

#### HTTP 方法推断
方法名前缀会自动推断 HTTP 方法：
- `Get*` → GET
- `Post*` → POST  
- `Put*` → PUT
- `Delete*` → DELETE
- `Patch*` → PATCH
- `Create*` → POST
- `Update*` → PUT
- `Remove*` → DELETE

### 授权和安全
``` csharp
[MiniController("/api/secure")]
[Authorize] // 控制器级别授权
public class SecureController
{
    [HttpGet("public")]
    [AllowAnonymous] // 方法级别覆盖
    public static IResult GetPublicData()
    {
        return Results.Ok("公开数据");
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")] // 特定角色授权
    public static IResult GetAdminData()
    {
        return Results.Ok("管理员数据");
    }
}
```
### 响应类型和文档
``` csharp
[MiniController("/api/products")]
public class ProductController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<Product>), 200)]
    [ProducesResponseType(400)]
    public static IResult GetProducts()
    {
        return Results.Ok(new List<Product>());
    }

    [HttpPost]
    [ProducesResponseType(typeof(Product), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public static IResult CreateProduct(Product product)
    {
        return Results.Created($"/api/products/{product.Id}", product);
    }
}
```
### 端点过滤器
``` csharp
public class LoggingFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        Console.WriteLine($"请求: {context.HttpContext.Request.Path}");
        var result = await next(context);
        Console.WriteLine($"响应: {result}");
        return result;
    }
}

[MiniController("/api/logged", FilterType = typeof(LoggingFilter))]
public class LoggedController
{
    [HttpGet("test")]
    public static IResult Test() => Results.Ok("测试日志过滤器");
}
```
## 📋 完整示例
``` csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace MyApi.Controllers;

[Area("v1")]
[MiniController("/api/[area]/[controller]", Name = "UserManagement")]
[Authorize]
[ApiExplorerSettings(GroupName = "Users")]
public static class UserController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<User>), 200)]
    [ProducesResponseType(401)]
    public static IResult GetUsers()
    {
        // 路由: GET /api/v1/user
        return Results.Ok(new List<User>());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(User), 200)]
    [ProducesResponseType(404)]
    public static IResult GetUser(int id)
    {
        // 路由: GET /api/v1/user/{id}
        return Results.Ok(new User { Id = id });
    }

    [HttpPost]
    [ProducesResponseType(typeof(User), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public static IResult CreateUser(CreateUserRequest request)
    {
        // 路由: POST /api/v1/user
        var user = new User { Id = Random.Shared.Next(), Name = request.Name };
        return Results.Created($"/api/v1/user/{user.Id}", user);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(User), 200)]
    [ProducesResponseType(404)]
    public static IResult UpdateUser(int id, UpdateUserRequest request)
    {
        // 路由: PUT /api/v1/user/{id}
        return Results.Ok(new User { Id = id, Name = request.Name });
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public static IResult DeleteUser(int id)
    {
        // 路由: DELETE /api/v1/user/{id}
        return Results.NoContent();
    }

    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<User>), 200)]
    public static IResult SearchUsers(string keyword)
    {
        // 路由: GET /api/v1/user/search
        return Results.Ok(new List<User>());
    }
}

public record User(int Id, string Name);
public record CreateUserRequest(string Name);
public record UpdateUserRequest(string Name);
```
## 🔧 配置和自定义

### MiniControllerAttribute 属性

``` csharp
[MiniController(
    routePrefix: "/api/v1/custom",  // 自定义路由前缀
    Name = "CustomGroup",           // 端点组名称
    FilterType = typeof(MyFilter)   // 自定义过滤器
)]
```
### 支持的路由模板占位符

| 占位符 | 描述 | 示例 |
|--------|------|------|
| `[area]` | 从 `[Area]` 特性获取 | `Admin` → `admin` |
| `[controller]` | 从类名提取，移除后缀 | `UserController` → `user` |
| `[action]` | 从方法名提取，移除前缀 | `GetUserList` → `user-list` |

### 支持的 HTTP 方法特性

- `[HttpGet]`、`[HttpPost]`、`[HttpPut]`、`[HttpDelete]`
- `[HttpPatch]`、`[HttpHead]`、`[HttpOptions]`

## 🧪 测试和调试

### 查看生成的代码

启用编译器生成文件输出：

``` xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```
生成的文件将保存在 `obj/Debug/netX.X/generated` 目录下。

### 使用 Swagger 测试
``` csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 在开发环境启用 Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```
## 🎯 最佳实践

1. **保持类的简洁**: 每个 MiniController 专注于单一职责
2. **使用有意义的命名**: 类名和方法名要能清楚表达其功能
3. **合理使用路由模板**: 根据 API 设计选择合适的占位符组合
4. **添加响应类型**: 使用 `ProducesResponseType` 改善 API 文档
5. **适当的授权策略**: 在控制器和方法级别合理配置授权
6. **遵循 REST 约定**: 使用标准的 HTTP 方法和状态码

## 🔄 兼容性

- **.NET Standard 2.0+**: 兼容 .NET Framework 4.6.1+、.NET Core 2.0+、.NET 5+
- **ASP.NET Core 6.0+**: 需要 Minimal API 支持
- **Roslyn Source Generators**: 需要支持 Source Generator 的编译器

## 🐛 故障排除

### 常见问题

1. **端点未生成**: 检查类是否标记了 `[MiniController]` 特性
2. **路由冲突**: 确保路由模板不与现有路由冲突
3. **编译错误**: 检查生成的代码文件，确保没有语法错误
4. **Source Generator 未运行**: 确保项目文件正确配置了 Analyzer 引用

### 调试技巧

``` csharp
// 在 EndpointGenerator.cs 中启用调试器
#if DEBUG
    Debugger.Launch(); // 启用调试器
#endif

```
## 🤝 贡献与反馈

欢迎提交 Issue 和 Pull Request！

- **GitHub**: [https://github.com/fanslead/MiniController](https://github.com/fanslead/MiniController)
- **Issues**: [报告问题或建议](https://github.com/fanslead/MiniController/issues)
- **NuGet**: [MiniController](https://www.nuget.org/packages/MiniController)

## 📄 许可证

本项目采用 [MIT 许可证](https://opensource.org/licenses/MIT)。

---

**自动生成声明** 

本项目部分代码由 Roslyn Source Generator 自动生成，请勿手动修改生成文件。生成的代码会自动注册到 DI 容器中，无需手动配置。
