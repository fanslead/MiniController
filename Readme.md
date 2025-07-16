# MiniController

MiniController 是一个基于 Roslyn Source Generator 的 .NET Standard 2.0 端点自动注册工具。它通过自定义特性（如 `MiniControllerAttribute`）自动生成分组端点注册扩展方法，简化 Minimal API 路由、授权、过滤器和响应类型的声明与维护。

**传统Web API和Minimal API比较**
| **场景**                  | **传统Web API**                  | **Minimal API**                  |
|---------------------------|----------------------------------|----------------------------------|
| 大型复杂项目              | ✅ 更好的代码组织和可维护性       | ❌ 路由逻辑集中导致Program.cs膨胀 |
| 小型服务/快速原型         | ❌ 模板代码较多                   | ✅ 代码简洁，开发效率高           |
| 需要丰富的框架特性        | ✅ 完整支持过滤器、模型验证等     | ❌ 部分功能需要手动实现           |
| 微服务/无服务器应用       | ❌ 启动时间较长                   | ✅ 轻量级，启动快                 |

| **特性**               | **传统Web API**                     | **Minimal API**                     |
|------------------------|-------------------------------------|-------------------------------------|
| **路由定义**           | 基于控制器和属性路由                | 基于链式方法和lambda表达式          |
| **代码结构**           | 结构化，适合大型项目                | 扁平化，适合小型项目                |
| **灵活性**             | 依赖框架约定                        | 高度自定义                          |
| **学习曲线**           | 较高（需理解MVC模式）               | 较低（无需控制器概念）              |

本项目结合两者优势，拥有传统WebAPI的写法体验以及Minimal API的性能优势。

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
- **静态和实例类支持**: 支持静态类和实例类两种模式
- **自动依赖注入**: 自动生成 `AddMiniControllers` 扩展方法，支持依赖注入
- **智能参数绑定**: 支持 `[FromServices]`、`[FromRoute]`、`[FromQuery]` 等参数绑定特性
- **路由冲突检测**: 编译时检测路由冲突，提供诊断信息

## 📦 安装

### 通过 NuGet 包管理器
```
Install-Package MiniController
```
### 通过 .NET CLI
```
dotnet add package MiniController
```
### 手动添加到项目文件
``` xml
<ItemGroup>
  <PackageReference Include="MiniController" Version="1.0.3" PrivateAssets="all"/>
</ItemGroup>
```
## 🏃‍♂️ 快速开始

### 1. 定义一个简单的 MiniController（静态类）
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
### 2. 定义一个实例类 MiniController
``` csharp
using MiniController.Attributes;
using Microsoft.AspNetCore.Mvc;

[MiniController("/api/products")]
public class ProductController
{
    private readonly ILogger<ProductController> _logger;

    public ProductController(ILogger<ProductController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<Product>), 200)]
    public IResult GetProducts()
    {
        _logger.LogInformation("Getting all products");
        return Results.Ok(new List<Product>());
    }

    [HttpGet("{id:int}")]
    public IResult GetProduct([FromRoute] int id)
    {
        _logger.LogInformation("Getting product {Id}", id);
        return Results.Ok(new Product { Id = id, Name = "Sample Product" });
    }

    [HttpPost]
    public IResult CreateProduct([FromBody] Product product)
    {
        _logger.LogInformation("Creating product: {ProductName}", product.Name);
        return Results.Created($"/api/products/{product.Id}", product);
    }
}

public record Product(int Id, string Name);
```
### 3. 在应用启动时注册所有端点
``` csharp
var builder = WebApplication.CreateBuilder(args);
// 添加必要的服务
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 注册 MiniController 实例类（静态类无需注册）
builder.Services.AddMiniControllers();

var app = builder.Build();

// 一行代码注册所有 MiniController 端点
app.MapMiniController();

app.Run();
```
### 4. 编译后自动生成扩展方法

Source Generator 会自动生成如下扩展方法：
- `DemoControllerExtensions.MapDemoController`
- `ProductControllerExtensions.MapProductController` 
- `MiniControllerExtensions.MapMiniController`
- `MiniControllerServiceCollectionExtensions.AddMiniControllers`

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
### 智能参数绑定

MiniController 支持完整的参数绑定特性：
``` csharp
[MiniController("/api/advanced")]
public class AdvancedController
{
    private readonly ILogger<AdvancedController> _logger;

    public AdvancedController(ILogger<AdvancedController> logger)
    {
        _logger = logger;
    }

    [HttpGet("users/{id}")]
    public IResult GetUser(
        [FromRoute] int id,
        [FromQuery] string? name,
        [FromHeader(Name = "X-User-Agent")] string? userAgent,
        [FromServices] IUserService userService)
    {
        _logger.LogInformation("Getting user {Id} with name filter {Name}", id, name);
        return Results.Ok($"User {id}, Name: {name}, UserAgent: {userAgent}");
    }

    [HttpPost("users")]
    public IResult CreateUser(
        [FromBody] CreateUserRequest request,
        [FromServices] IUserService userService)
    {
        _logger.LogInformation("Creating user: {UserName}", request.Name);
        return Results.Created($"/api/advanced/users/{request.Id}", request);
    }
}

public record CreateUserRequest(int Id, string Name);
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
public class UserController
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<User>), 200)]
    [ProducesResponseType(401)]
    public async Task<IResult> GetUsers()
    {
        // 路由: GET /api/v1/user
        _logger.LogInformation("Getting all users");
        var users = await _userService.GetAllUsersAsync();
        return Results.Ok(users);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(User), 200)]
    [ProducesResponseType(404)]
    public async Task<IResult> GetUser([FromRoute] int id)
    {
        // 路由: GET /api/v1/user/{id}
        var user = await _userService.GetUserByIdAsync(id);
        return user != null ? Results.Ok(user) : Results.NotFound();
    }

    [HttpPost]
    [ProducesResponseType(typeof(User), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // 路由: POST /api/v1/user
        var user = await _userService.CreateUserAsync(request);
        return Results.Created($"/api/v1/user/{user.Id}", user);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(User), 200)]
    [ProducesResponseType(404)]
    public async Task<IResult> UpdateUser([FromRoute] int id, [FromBody] UpdateUserRequest request)
    {
        // 路由: PUT /api/v1/user/{id}
        var user = await _userService.UpdateUserAsync(id, request);
        return user != null ? Results.Ok(user) : Results.NotFound();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IResult> DeleteUser([FromRoute] int id)
    {
        // 路由: DELETE /api/v1/user/{id}
        var deleted = await _userService.DeleteUserAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<User>), 200)]
    public async Task<IResult> SearchUsers([FromQuery] string keyword)
    {
        // 路由: GET /api/v1/user/search
        var users = await _userService.SearchUsersAsync(keyword);
        return Results.Ok(users);
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

### 支持的参数绑定特性

- `[FromServices]` - 从依赖注入容器获取
- `[FromRoute]` - 从路由参数获取
- `[FromQuery]` - 从查询字符串获取
- `[FromBody]` - 从请求体获取
- `[FromHeader]` - 从请求头获取
- `[FromForm]` - 从表单获取

### 支持的 HTTP 方法特性

- `[HttpGet]`、`[HttpPost]`、`[HttpPut]`、`[HttpDelete]`
- `[HttpPatch]`、`[HttpHead]`、`[HttpOptions]`

## 🔄 静态类 vs 实例类

### 静态类模式
- **优点**: 性能更高，启动更快
- **缺点**: 仅支持方法级别依赖注入，不支持构造函数注入
- **适用场景**: 简单的 API、工具类、无状态操作
[MiniController("/api/simple")]
public static class SimpleController
{
    [HttpGet("ping")]
    public static IResult Ping() => Results.Ok("pong");
}
### 实例类模式
- **优点**: 支持完整依赖注入，更好的测试性，支持复杂业务逻辑
- **缺点**: 轻微的性能开销
- **适用场景**: 复杂的业务逻辑，需要依赖注入的场景
``` csharp
[MiniController("/api/complex")]
public class ComplexController
{
    private readonly IService _service;

    public ComplexController(IService service)
    {
        _service = service;
    }

    [HttpGet("data")]
    public async Task<IResult> GetData()
    {
        var data = await _service.GetDataAsync();
        return Results.Ok(data);
    }
}
```
## 🧪 测试和调试

### 查看生成的代码

启用编译器生成文件输出：
``` xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>生成的文件将保存在 `obj/Debug/netX.X/generated` 目录下。
``` 
### 使用 Swagger 测试
``` csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 注册 MiniController 服务
builder.Services.AddMiniControllers();

var app = builder.Build();

// 在开发环境启用 Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```
// 注册所有端点
``` csharp
app.MapMiniController();
```
## 🎯 最佳实践

1. **选择合适的类模式**: 简单操作使用静态类，复杂业务逻辑使用实例类
2. **合理使用依赖注入**: 实例类中充分利用 DI 容器管理依赖
3. **保持类的简洁**: 每个 MiniController 专注于单一职责
4. **使用有意义的命名**: 类名和方法名要能清楚表达其功能
5. **合理使用路由模板**: 根据 API 设计选择合适的占位符组合
6. **添加响应类型**: 使用 `ProducesResponseType` 改善 API 文档
7. **适当的授权策略**: 在控制器和方法级别合理配置授权
8. **遵循 REST 约定**: 使用标准的 HTTP 方法和状态码
9. **合理使用参数绑定**: 明确指定参数来源，提高可读性

## 🔄 兼容性

- **ASP.NET Core 6.0+**: 需要 Minimal API 支持
- **Roslyn Source Generators**: 需要支持 Source Generator 的编译器

## 🐛 故障排除

### 常见问题

1. **端点未生成**: 检查类是否标记了 `[MiniController]` 特性
2. **路由冲突**: 确保路由模板不与现有路由冲突（编译时会提供警告）
3. **实例类注入失败**: 确保调用了 `AddMiniControllers()` 注册服务
4. **编译错误**: 检查生成的代码文件，确保没有语法错误
5. **Source Generator 未运行**: 确保项目文件正确配置了 Analyzer 引用

### 调试技巧
// 在 EndpointGenerator.cs 中启用调试器
#if DEBUG
    Debugger.Launch(); // 启用调试器
#endif
### 诊断信息

MiniController 提供编译时诊断：
- **MC001**: 路由冲突警告
- **MC002**: 无效的端点错误

## 🤝 贡献与反馈

欢迎提交 Issue 和 Pull Request！

- **GitHub**: [https://github.com/fanslead/MiniController](https://github.com/fanslead/MiniController)
- **Issues**: [报告问题或建议](https://github.com/fanslead/MiniController/issues)
- **NuGet**: [MiniController](https://www.nuget.org/packages/MiniController)

## 📄 许可证

本项目采用 [MIT 许可证](https://opensource.org/licenses/MIT)。

---

