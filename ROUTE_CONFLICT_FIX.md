# MiniController ·�ɳ�ͻ�޸�����

## ?? �������

### ���ֵ�����

1. **·�ɳ�ͻ����**
   - `AnalyticsEndpoints` �ж�� `Get*` ����������ͬ·��
   - `ProductService` �ж������Ҳ����·�ɳ�ͻ
   - ��Ҫԭ��`[action]` ռλ�������߼�����ȷ

2. **���ȱ��**
   - `GetEffectiveRouteTemplate` ������ `[action]` ģʽ��������
   - ·����֤�߼���׼ȷ���޷���ȷ���ʵ�ʵ�·�ɳ�ͻ
   - `[action]` ·�ɱ��ظ���ӣ������ظ���·����

## ?? �޸�����

### 1. �޸�·�������߼�

**�����Դ**��
- ���ڰ��� `[action]` ռλ���Ŀ�������`GetEffectiveRouteTemplate` �����᷵���ƶϵ�action·��
- Ȼ���� `GenerateDirectMethodRegistrations` �У����·�ɻᱻ׷�ӵ��Ѿ������� `[action]` ������·�ɺ���
- �������� `/api/v2/report/analytics/sales-report/sales-report` ���ظ�·��

**�޸�����**��
```csharp
// �޸�ǰ��Ϊ[action]ģʽ����action·��
var actionRoute = HttpMethodHelper.GetRouteTemplateForAction(methodSymbol, httpMethod, methodName);
return actionRoute;

// �޸���Ϊ[action]ģʽֻ������ȷָ����·��ģ�����ַ���
return string.Empty;
```

### 2. �Ľ�·����֤�߼�

**ԭ����**��
- ��֤�߼�ʹ�� `method.RouteTemplate` ���г�ͻ���
- ���� `[action]` ģʽ����ֻ�ǲ���·�ɣ���������·��

**�޸�����**��
```csharp
// ���� BuildFullRouteForValidation ����
private static string BuildFullRouteForValidation(EndpointGroupClass endpointGroup, EndpointMethod method)
{
    // ���������·�ɰ���[action]ռλ������Ҫ��������·��
    if (routePrefix.Contains("[action]"))
    {
        return RouteTemplateResolver.ResolveActionTemplate(routePrefix, method.Name, method.HttpMethod);
    }
    // �����߼�...
}
```

### 3. ��ǿ HTTP �����ƶ�

**��������**��
```csharp
public static string GetRouteTemplateForAction(ISymbol methodSymbol, string httpMethod, string methodName)
{
    // ר�Ŵ���[action]ռλ����·������
    // �ӷ��������Ƴ�HTTPǰ׺��ת��Ϊkebab-case
}
```

## ? �޸����

### �޸�ǰ�����ɴ���
```csharp
// ����·���ظ�
builder.MapGet("/api/v2/report/analytics/sales-report/sales-report", AnalyticsEndpoints.GetSalesReport)
builder.MapGet("/api/v2/report/analytics/user-activity/user-activity", AnalyticsEndpoints.GetUserActivity)
```

### �޸�������ɴ���
```csharp
// ��ȷ��·��Ψһ
builder.MapGet("/api/v2/report/analytics/sales-report", AnalyticsEndpoints.GetSalesReport)
builder.MapGet("/api/v2/report/analytics/user-activity", AnalyticsEndpoints.GetUserActivity)
builder.MapGet("/api/v2/report/analytics/daily-revenue-analysis", AnalyticsEndpoints.GetDailyRevenueAnalysis)
```

### ·��ӳ��ʾ��

| ������ | HTTP���� | ����·�� |
|--------|----------|----------|
| `GetSalesReport` | GET | `/api/v2/report/analytics/sales-report` |
| `GetUserActivity` | GET | `/api/v2/report/analytics/user-activity` |
| `PostGenerateReport` | POST | `/api/v2/report/analytics/generate-report` |
| `GetDailyRevenueAnalysis` | GET | `/api/v2/report/analytics/daily-revenue-analysis` |

## ?? ���ĸĽ�

### 1. ·��ռλ������
- ? ��ȷ���� `[area]`��`[controller]`��`[action]` ռλ��
- ? �����ظ���·����
- ? ֧����ȷָ����·��ģ�帲��

### 2. ��ͻ����Ż�
- ? ʹ������·�ɽ��г�ͻ���
- ? ׼ȷʶ����ʵ��·�ɳ�ͻ
- ? �ṩ������ľ�����Ϣ

### 3. ����ע��֧��
- ? �̶� Transient ��������
- ? �Զ����� DI ע����չ����
- ? ���ܲ���������

## ?? ��֤���

### �������
```
�� 3.9 �������� �ɹ������� 1 ����
```
- ? ��·�ɳ�ͻ����
- ? ֻ��һ���ɺ��ԵĿɿ����þ���

### ���ɵ�·��
����·�ɶ���Ψһ�ģ�û�г�ͻ��
- `/api/v2/report/analytics/sales-report`
- `/api/v2/report/analytics/user-activity`
- `/api/v2/report/analytics/generate-report`
- `/api/v2/report/analytics/daily-revenue-analysis`
- `/api/v2/report/analytics/weekly-report/weekly`
- `/api/v2/report/analytics/export-report/export/{format}`

## ?? ���ʵ������

### 1. ʹ�� [action] ռλ��
```csharp
[MiniController("/api/v1/[controller]/[action]")]
public class ProductService
{
    public static IResult GetList() { } // �� /api/v1/product/list
    public static IResult GetDetails(int id) { } // �� /api/v1/product/details
    public static IResult PostCreate() { } // �� /api/v1/product/create
}
```

### 2. ��ȷָ��·�ɱ����ͻ
```csharp
[HttpGet("featured")]  // ��ȷָ��·��
public static IResult GetFeaturedProducts() { }

[HttpPost("validate")]  // ��ȷָ��·��
public static IResult PostValidateProduct([FromBody] object data) { }
```

### 3. ������������
- ʹ��������HTTPǰ׺��`Get*`, `Post*`, `Put*`, `Delete*`
- �����ظ��ķ�����
- ʹ��������Ķ�������

---

**�ܽ�**���˴��޸����׽����·�ɳ�ͻ���⣬�Ľ���·�����ɺ���֤�߼���ʹ MiniController ���ӽ�׳�Ϳɿ���