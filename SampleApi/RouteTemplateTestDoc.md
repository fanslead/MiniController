# MiniController ·��ģ�幦�ܲ����ĵ�

## ���ܸ���

MiniController ����֧��ǿ���·��ģ���﷨�������� ASP.NET Core MVC ��·��ģ�壺

- `[area]` - �� MVC �� [Area] ���Ա�ǩ�л�ȡ
- `[controller]` - ����������ȡ���Զ��Ƴ� Service��Controller��Endpoint��Endpoints ��׺
- `[action]` - �ӷ���������ȡ���Զ��Ƴ� HTTP ����ǰ׺

## ��������˵��

### 1. ���� Area ���û�����˵�
**�ļ�**: `Areas/Admin/Controllers/UserEndpoints.cs`
**·��ģ��**: `/api/[area]/[controller]`
**Ԥ�ڽ��**:
- GET `/api/admin/user` - GetUsers()
- GET `/api/admin/user/{id}` - GetUser(id)
- POST `/api/admin/user` - CreateUser()
- PUT `/api/admin/user/{id}` - UpdateUser(id)
- DELETE `/api/admin/user/{id}` - DeleteUser(id)

### 2. ��Ʒ����˵㣨���� action ģ�壩
**�ļ�**: `Controllers/ProductService.cs`
**·��ģ��**: `/api/v1/[controller]/[action]`
**Ԥ�ڽ��**:
- GET `/api/v1/product/list` - GetList()
- GET `/api/v1/product/details` - GetDetails(id)
- POST `/api/v1/product/create` - PostCreate()
- PUT `/api/v1/product/update` - PutUpdate(id)
- DELETE `/api/v1/product/remove` - DeleteRemove(id)
- GET `/api/v1/product/search-by-category` - GetSearchByCategory(category)
- POST `/api/v1/product/batch-import` - PostBatchImport()

### 3. ���������������� controller ģ�壩
**�ļ�**: `Controllers/OrderController.cs`
**·��ģ��**: `/api/[controller]`
**Ԥ�ڽ��**:
- GET `/api/order` - GetOrders()
- GET `/api/order/{id}` - GetOrder(id)
- POST `/api/order` - CreateOrder()
- GET `/api/order/search` - SearchOrders(keyword)

### 4. ����˵㣨area ������ʱ�Ĵ���
**�ļ�**: `Services/CategoryEndpoint.cs`
**·��ģ��**: `/api/[area]/[controller]`
**Ԥ�ڽ��**:
- GET `/api/category` - GetCategories() (area ���Զ�����)
- POST `/api/category` - CreateCategory()

### 5. �����˵㣨����ģ�壩
**�ļ�**: `Areas/Report/Services/AnalyticsEndpoints.cs`
**·��ģ��**: `/api/v2/[area]/[controller]/[action]`
**Ԥ�ڽ��**:
- GET `/api/v2/report/analytics/sales-report` - GetSalesReport()
- GET `/api/v2/report/analytics/user-activity` - GetUserActivity()
- POST `/api/v2/report/analytics/generate-report` - PostGenerateReport()
- GET `/api/v2/report/analytics/daily-revenue-analysis` - GetDailyRevenueAnalysis()

### 6. Ĭ�Ϻ��Զ���·�ɲ���
**�ļ�**: `Controllers/DefaultAndCustomRouteTests.cs`
**��������**:
- **NotificationEndpoints**: Ĭ��·�� `/api/notification`
- **MessageController**: �Զ���·�� `/custom/path/messages`

### 7. ��׺�Ƴ�����
**�ļ�**: `Controllers/SuffixRemovalTests.cs`
**��������**:
- **AccountService** �� `/api/account` (Service ��׺�Ƴ�)
- **PaymentController** �� `/api/payment` (Controller ��׺�Ƴ�)
- **FileEndpoint** �� `/api/file` (Endpoint ��׺�Ƴ�)
- **DataEndpoints** �� `/api/data` (Endpoints ��׺�Ƴ�)

## ���в���

���� SampleApi ��Ŀ������ Swagger UI �鿴���ɵ����ж˵㣺

```bash
cd SampleApi
dotnet run
```

Ȼ����� `https://localhost:5001/swagger` �鿴�������ɵĶ˵㡣

## ����ʵ��Ҫ��

1. **ģ�������**: `RouteTemplateResolver` �ฺ�����·��ģ���е�ռλ��
2. **��׺����**: �Զ��Ƴ�������������׺��Service��Controller��Endpoint��Endpoints��
3. **����ת��**: �Զ��� PascalCase ת��Ϊ kebab-case
4. **Area ֧��**: ��ȫ֧�� MVC �� [Area] ����
5. **·���淶��**: �Զ���������б�ܺ�·����ʽ

## ��֤����

1. ������ɵ�Դ�����ļ����� `obj/Debug/net8.0/generated` Ŀ¼�£�
2. ����Ӧ�ò��鿴 Swagger �ĵ�
3. ʹ�� HTTP �ͻ��˲��Ը����˵�
4. ��֤·���Ƿ�Ԥ��ӳ��