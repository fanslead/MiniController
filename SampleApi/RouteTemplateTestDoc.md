# MiniController 路由模板功能测试文档

## 功能概述

MiniController 现在支持强大的路由模板语法，类似于 ASP.NET Core MVC 的路由模板：

- `[area]` - 从 MVC 的 [Area] 特性标签中获取
- `[controller]` - 从类名中提取，自动移除 Service、Controller、Endpoint、Endpoints 后缀
- `[action]` - 从方法名中提取，自动移除 HTTP 动词前缀

## 测试用例说明

### 1. 带有 Area 的用户管理端点
**文件**: `Areas/Admin/Controllers/UserEndpoints.cs`
**路由模板**: `/api/[area]/[controller]`
**预期结果**:
- GET `/api/admin/user` - GetUsers()
- GET `/api/admin/user/{id}` - GetUser(id)
- POST `/api/admin/user` - CreateUser()
- PUT `/api/admin/user/{id}` - UpdateUser(id)
- DELETE `/api/admin/user/{id}` - DeleteUser(id)

### 2. 产品服务端点（包含 action 模板）
**文件**: `Controllers/ProductService.cs`
**路由模板**: `/api/v1/[controller]/[action]`
**预期结果**:
- GET `/api/v1/product/list` - GetList()
- GET `/api/v1/product/details` - GetDetails(id)
- POST `/api/v1/product/create` - PostCreate()
- PUT `/api/v1/product/update` - PutUpdate(id)
- DELETE `/api/v1/product/remove` - DeleteRemove(id)
- GET `/api/v1/product/search-by-category` - GetSearchByCategory(category)
- POST `/api/v1/product/batch-import` - PostBatchImport()

### 3. 订单控制器（基础 controller 模板）
**文件**: `Controllers/OrderController.cs`
**路由模板**: `/api/[controller]`
**预期结果**:
- GET `/api/order` - GetOrders()
- GET `/api/order/{id}` - GetOrder(id)
- POST `/api/order` - CreateOrder()
- GET `/api/order/search` - SearchOrders(keyword)

### 4. 分类端点（area 不存在时的处理）
**文件**: `Services/CategoryEndpoint.cs`
**路由模板**: `/api/[area]/[controller]`
**预期结果**:
- GET `/api/category` - GetCategories() (area 被自动忽略)
- POST `/api/category` - CreateCategory()

### 5. 分析端点（完整模板）
**文件**: `Areas/Report/Services/AnalyticsEndpoints.cs`
**路由模板**: `/api/v2/[area]/[controller]/[action]`
**预期结果**:
- GET `/api/v2/report/analytics/sales-report` - GetSalesReport()
- GET `/api/v2/report/analytics/user-activity` - GetUserActivity()
- POST `/api/v2/report/analytics/generate-report` - PostGenerateReport()
- GET `/api/v2/report/analytics/daily-revenue-analysis` - GetDailyRevenueAnalysis()

### 6. 默认和自定义路由测试
**文件**: `Controllers/DefaultAndCustomRouteTests.cs`
**测试内容**:
- **NotificationEndpoints**: 默认路由 `/api/notification`
- **MessageController**: 自定义路由 `/custom/path/messages`

### 7. 后缀移除测试
**文件**: `Controllers/SuffixRemovalTests.cs`
**测试内容**:
- **AccountService** → `/api/account` (Service 后缀移除)
- **PaymentController** → `/api/payment` (Controller 后缀移除)
- **FileEndpoint** → `/api/file` (Endpoint 后缀移除)
- **DataEndpoints** → `/api/data` (Endpoints 后缀移除)

## 运行测试

启动 SampleApi 项目并访问 Swagger UI 查看生成的所有端点：

```bash
cd SampleApi
dotnet run
```

然后访问 `https://localhost:5001/swagger` 查看所有生成的端点。

## 技术实现要点

1. **模板解析器**: `RouteTemplateResolver` 类负责解析路由模板中的占位符
2. **后缀处理**: 自动移除常见的类名后缀（Service、Controller、Endpoint、Endpoints）
3. **命名转换**: 自动将 PascalCase 转换为 kebab-case
4. **Area 支持**: 完全支持 MVC 的 [Area] 特性
5. **路径规范化**: 自动处理多余的斜杠和路径格式

## 验证方法

1. 检查生成的源代码文件（在 `obj/Debug/net8.0/generated` 目录下）
2. 运行应用并查看 Swagger 文档
3. 使用 HTTP 客户端测试各个端点
4. 验证路由是否按预期映射