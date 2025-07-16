# MiniController ����ע��֧���ĵ�

## ?? �����ԣ�����ע��֧��

MiniController ����֧������ע�룬���зǾ�̬�� MiniController �ཫ�Զ�ע�ᵽDI�����У�**�������ڹ̶�Ϊ Transient**��

## ?? ��Ҫ˵��

**MiniController ������ע���������ڱ����ҽ����� Transient**������Ϊ�ˣ�

1. **����״̬��������**��ȷ��ÿ�����󶼴����µĿ�����ʵ��
2. **��֤�̰߳�ȫ**��ÿ�������ж����Ŀ�����ʵ��
3. **�򻯿���ģ��**�����������迼��ʵ��״̬��������

## ?? ���ٿ�ʼ

### 1. ����֧������ע��� MiniController

```csharp
using Microsoft.AspNetCore.Mvc;
using MiniController.Attributes;

namespace MyApi.Controllers;

// �Ǿ�̬���Զ�֧������ע�룬�������ڹ̶�ΪTransient
[MiniController("/api/[controller]")]
public class UserController
{
    private readonly ILogger<UserController> _logger;
    private readonly IUserService _userService;

    // ֧�ֹ��캯��ע��
    public UserController(ILogger<UserController> logger, IUserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IResult> GetUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        _logger.LogInformation("��ȡ�� {Count} ���û�", users.Count);
        return Results.Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<IResult> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return Results.NotFound();
            
        _logger.LogInformation("��ȡ�û� {UserId}: {UserName}", id, user.Name);
        return Results.Ok(user);
    }

    [HttpPost]
    public async Task<IResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = await _userService.CreateUserAsync(request.Name, request.Email);
        _logger.LogInformation("�����û�: {UserName}", user.Name);
        return Results.Created($"/api/user/{user.Id}", user);
    }
}
```

### 2. ��Ӧ������ʱע�����

```csharp
var builder = WebApplication.CreateBuilder(args);

// ��ӱ�Ҫ�ķ���
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ע��ҵ�����
builder.Services.AddScoped<IUserService, UserService>();

// ?? �Զ�ע������MiniController��DI������Transient�������ڣ�
builder.Services.AddMiniControllers();

var app = builder.Build();

// ?? һ�д���ע������MiniController�˵�
app.MapMiniController();

app.Run();
```

## ?? ���ԶԱ�

| ���� | ��̬�� MiniController | ʵ���� MiniController |
|------|---------------------|---------------------|
| **����ע��** | ? ��֧�� | ? ֧�� |
| **���캯��ע��** | ? ��֧�� | ? ֧�� |
| **��������** | N/A | ?? **�̶� Transient** |
| **״̬����** | ��̬״̬ | ÿ��������ʵ�� |
| **����** | �Ժã���ʵ���������� | ��΢������ʵ������ |
| **�̰߳�ȫ** | ��Ҫ�ֶ����� | �Զ���֤ |

## ?? ���ɵĴ���ʾ��

### DI ע����չ����
```csharp
// �Զ����ɵ��ļ���MiniControllerServiceCollectionExtensions.g.cs
public static class MiniControllerServiceCollectionExtensions
{
    public static IServiceCollection AddMiniControllers(this IServiceCollection services)
    {
        services.AddTransient<UserController>();
        services.AddTransient<ProductController>();
        services.AddTransient<OrderController>();
        // ... �����Ǿ�̬ MiniController
        return services;
    }
}
```

### �˵�ע����չ����
```csharp
// �Զ����ɵ��ļ���UserControllerExtensions.g.cs
public static class UserControllerExtensions
{
    public static IEndpointRouteBuilder MapUserController(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/user");
        
        // ʵ���������ã�֧������ע��
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

## ?? ���ʵ��

### ? �Ƽ�����

1. **ʹ��ʵ������и���ҵ���߼�**
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
    
    // ����ҵ���߼��������������
    [HttpPost]
    public async Task<IResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var order = await _orderService.CreateAsync(request);
        await _paymentService.ProcessPaymentAsync(order.Id, request.PaymentInfo);
        _logger.LogInformation("���� {OrderId} �����ɹ�", order.Id);
        return Results.Created($"/api/orders/{order.Id}", order);
    }
}
```

2. **��̬�����ڼ򵥹���**
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

### ? ���������

```csharp
// ? ��Ҫ��ʵ����������ά���������״̬
[MiniController("/api/counter")]
public class CounterController
{
    private static int _counter = 0; // ? ��̬״̬
    private int _instanceCounter = 0; // ? ʵ��״̬��ÿ���������ã�
    
    [HttpGet]
    public IResult GetCounter()
    {
        _instanceCounter++; // ? ֻ�ڵ�ǰ��������Ч
        return Results.Ok(new { InstanceCounter = _instanceCounter });
    }
}
```

## ?? ����������֤ʾ��

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
            Note = "ÿ�����󶼻ῴ���µ�InstanceId��֤��ʹ����Transient��������"
        });
    }
}
```

��������˵��Σ����ῴ��ÿ�������в�ͬ�� `InstanceId` �� `CreatedAt`��֤��ÿ�ζ��������µĿ�����ʵ����

## ?? Ǩ��ָ��

### �Ӿ�̬��Ǩ�Ƶ�ʵ����

**֮ǰ����̬�ࣩ��**
```csharp
[MiniController("/api/users")]
public static class UserController
{
    [HttpGet]
    public static IResult GetUsers()
    {
        // �޷�ʹ������ע��
        var logger = LoggerFactory.CreateLogger<UserController>();
        logger.LogInformation("��ȡ�û��б�");
        return Results.Ok("�û��б�");
    }
}
```

**֮��ʵ���ࣩ��**
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
        _logger.LogInformation("��ȡ�û��б�");
        return Results.Ok("�û��б�");
    }
}
```

**������ģ�**
1. �Ƴ� `static` �ؼ���
2. ��ӹ��캯��������ע��
3. �Ƴ������� `static` �ؼ���
4. ���� `builder.Services.AddMiniControllers()`

## ?? ���ܿ���

- **ʵ��������**��Transient �������ڻ���ÿ������ʱ������ʵ����������ͨ����С
- **�ڴ�ʹ��**��ÿ������Ŀ�����ʵ�������������ᱻ��������
- **��������**��DI ������������캯���е�����������

���ڸ����ܳ���������������߼�������������ע�룬����ʹ�þ�̬���Ǹ��õ�ѡ��

---

**�ܽ�**��MiniController ����ͬʱ֧�־�̬�ࣨ�����ܣ�������ע�룩��ʵ���֧ࣨ������ע�룬�̶� Transient �������ڣ�����������ʵ������ѡ������ʵķ�ʽ��