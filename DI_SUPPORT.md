# MiniController 依赖注入支持文档

## ?? 新特性：依赖注入支持

MiniController 现在支持依赖注入，所有非静态的 MiniController 类将自动注册到DI容器中，**生命周期固定为 Transient**。

## ?? 重要说明

**MiniController 的依赖注入生命周期必须且仅能是 Transient**，这是为了：

1. **避免状态共享问题**：确保每次请求都创建新的控制器实例
2. **保证线程安全**：每个请求都有独立的控制器实例
3. **简化开发模型**：开发者无需考虑实例状态管理问题

## ?? 快速开始

### 1. 定义支持依赖注入的 MiniController

```csharp
using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace MyApi.Controllers;

// 非静态类自动支持依赖注入，生命周期固定为Transient
[MiniController("/api/[controller]")]
public class UserController
{
    private readonly ILogger<UserController> _logger;
    private readonly IUserService _userService;

    // 支持构造函数注入
    public UserController(ILogger<UserController> logger, IUserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IResult> GetUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        _logger.LogInformation("获取到 {Count} 个用户", users.Count);
        return Results.Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<IResult> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return Results.NotFound();
            
        _logger.LogInformation("获取用户 {UserId}: {UserName}", id, user.Name);
        return Results.Ok(user);
    }

    [HttpPost]
    public async Task<IResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = await _userService.CreateUserAsync(request.Name, request.Email);
        _logger.LogInformation("创建用户: {UserName}", user.Name);
        return Results.Created($"/api/user/{user.Id}", user);
    }
}
```

### 2. 在应用启动时注册服务

```csharp
var builder = WebApplication.CreateBuilder(args);

// 添加必要的服务
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 注册业务服务
builder.Services.AddScoped<IUserService, UserService>();

// ?? 自动注册所有MiniController到DI容器（Transient生命周期）
builder.Services.AddMiniControllers();

var app = builder.Build();

// ?? 一行代码注册所有MiniController端点
app.MapMiniController();

app.Run();
```

## ?? 特性对比

| 特性 | 静态类 MiniController | 实例类 MiniController |
|------|---------------------|---------------------|
| **依赖注入** | ? 不支持 | ? 支持 |
| **构造函数注入** | ? 不支持 | ? 支持 |
| **生命周期** | N/A | ?? **固定 Transient** |
| **状态管理** | 静态状态 | 每次请求新实例 |
| **性能** | 略好（无实例化开销） | 轻微开销（实例化） |
| **线程安全** | 需要手动处理 | 自动保证 |

## ?? 生成的代码示例

### DI 注册扩展方法
```csharp
// 自动生成的文件：MiniControllerServiceCollectionExtensions.g.cs
public static class MiniControllerServiceCollectionExtensions
{
    public static IServiceCollection AddMiniControllers(this IServiceCollection services)
    {
        services.AddTransient<UserController>();
        services.AddTransient<ProductController>();
        services.AddTransient<OrderController>();
        // ... 其他非静态 MiniController
        return services;
    }
}
```

### 端点注册扩展方法
```csharp
// 自动生成的文件：UserControllerExtensions.g.cs
public static class UserControllerExtensions
{
    public static IEndpointRouteBuilder MapUserController(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/user");
        
        // 实例方法调用，支持依赖注入
        group.MapGet("", ([FromServices] UserController controller) => 
            controller.GetUsers()).WithOpenApi();
            
        group.MapGet("{id:int}", ([FromServices] UserController controller, int id) => 
            controller.GetUser(id)).WithOpenApi();
            
        group.MapPost("", ([FromServices] UserController controller, [FromBody] CreateUserRequest request) => 
            controller.CreateUser(request)).WithOpenApi();
        
        return builder;
    }
}
```

## ?? 最佳实践

### ? 推荐做法

1. **使用实例类进行复杂业务逻辑**
```csharp
[MiniController("/api/orders")]
public class OrderController
{
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(
        IOrderService orderService,
        IPaymentService paymentService,
        ILogger<OrderController> logger)
    {
        _orderService = orderService;
        _paymentService = paymentService;
        _logger = logger;
    }
    
    // 复杂业务逻辑，依赖多个服务
    [HttpPost]
    public async Task<IResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var order = await _orderService.CreateAsync(request);
        await _paymentService.ProcessPaymentAsync(order.Id, request.PaymentInfo);
        _logger.LogInformation("订单 {OrderId} 创建成功", order.Id);
        return Results.Created($"/api/orders/{order.Id}", order);
    }
}
```

2. **静态类用于简单功能**
```csharp
[MiniController("/api/health")]
public static class HealthController
{
    [HttpGet]
    public static IResult GetHealth()
    {
        return Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }
}
```

### ? 避免的做法

```csharp
// ? 不要在实例控制器中维护跨请求的状态
[MiniController("/api/counter")]
public class CounterController
{
    private static int _counter = 0; // ? 静态状态
    private int _instanceCounter = 0; // ? 实例状态（每次请求重置）
    
    [HttpGet]
    public IResult GetCounter()
    {
        _instanceCounter++; // ? 只在当前请求中有效
        return Results.Ok(new { InstanceCounter = _instanceCounter });
    }
}
```

## ?? 生命周期验证示例

```csharp
[MiniController("/api/instance")]
public class InstanceController
{
    private readonly Guid _instanceId = Guid.NewGuid();
    private readonly DateTime _createdAt = DateTime.UtcNow;
    private int _callCount = 0;

    [HttpGet]
    public IResult GetInstance()
    {
        _callCount++;
        return Results.Ok(new 
        { 
            InstanceId = _instanceId,
            CreatedAt = _createdAt,
            CallCount = _callCount,
            Note = "每次请求都会看到新的InstanceId，证明使用了Transient生命周期"
        });
    }
}
```

调用这个端点多次，您会看到每次请求都有不同的 `InstanceId` 和 `CreatedAt`，证明每次都创建了新的控制器实例。

## ?? 迁移指南

### 从静态类迁移到实例类

**之前（静态类）：**
```csharp
[MiniController("/api/users")]
public static class UserController
{
    [HttpGet]
    public static IResult GetUsers()
    {
        // 无法使用依赖注入
        var logger = LoggerFactory.CreateLogger<UserController>();
        logger.LogInformation("获取用户列表");
        return Results.Ok("用户列表");
    }
}
```

**之后（实例类）：**
```csharp
[MiniController("/api/users")]
public class UserController
{
    private readonly ILogger<UserController> _logger;

    public UserController(ILogger<UserController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IResult GetUsers()
    {
        _logger.LogInformation("获取用户列表");
        return Results.Ok("用户列表");
    }
}
```

**所需更改：**
1. 移除 `static` 关键字
2. 添加构造函数和依赖注入
3. 移除方法的 `static` 关键字
4. 调用 `builder.Services.AddMiniControllers()`

## ?? 性能考虑

- **实例化开销**：Transient 生命周期会在每次请求时创建新实例，但开销通常很小
- **内存使用**：每个请求的控制器实例在请求结束后会被垃圾回收
- **依赖解析**：DI 容器会解析构造函数中的所有依赖项

对于高性能场景，如果控制器逻辑简单且无需依赖注入，继续使用静态类是更好的选择。

---

**总结**：MiniController 现在同时支持静态类（高性能，无依赖注入）和实例类（支持依赖注入，固定 Transient 生命周期），让您根据实际需求选择最合适的方式。