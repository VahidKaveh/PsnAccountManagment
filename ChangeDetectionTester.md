# Change Detection Testing and Debugging Guide

## مشکلات تشخیص داده شده:

### 1. Hash Generation Issues:
- تابع `NormalizeMessageText` در `ChangeDetectionService` فقط whitespace normalize می‌کرد
- حالا toLowerCase و Unicode normalization اضافه شده

### 2. ProcessingService Flow:
- قبلاً change ها فقط pending می‌شدند اما process نمی‌شدند
- حالا همه change ها به طور خودکار process می‌شوند
- Logging بهبود یافته برای debugging

### 3. Configuration:
- Settings اضافه شده در `appsettings.json`
- Default values برای auto processing

## تست کردن Change Detection:

### 1. بررسی Log ها:
```bash
# در Admin Panel logs دنبال این پیام‌ها باشید:
[Information] Change detection result for message {MessageId}: HasChanged={HasChanged}
[Information] Content change detected for message {MessageId} from channel {ChannelId}
[Information] Detected changes in account {ExternalId}: {ChangeType}, Total changes: {Count}
[Information] IMPORTANT: Sold status changed for {ExternalId}: {OldStatus} -> {NewStatus}
```

### 2. بررسی Database:
```sql
-- چک کردن RawMessage ها با تغییرات
SELECT 
    Id, ChannelId, ExternalMessageId, 
    IsChange, ChangeDetails, Status, 
    ContentHash, CreatedAt
FROM RawMessages 
WHERE IsChange = 1 
ORDER BY CreatedAt DESC;

-- چک کردن AdminNotification ها
SELECT 
    Id, Type, Title, Message, 
    Priority, IsRead, CreatedAt
FROM AdminNotifications 
WHERE Type = 'AccountChanged'
ORDER BY CreatedAt DESC;
```

### 3. Manual Test:
1. یک پیغام از کانال را scrape کنید
2. همان پیغام را با تغییر جزئی دوباره scrape کنید
3. بررسی کنید که:
   - `IsChange = true` در RawMessage
   - `ChangeDetails` پر شده باشد
   - AdminNotification ایجاد شده باشد
   - Account به‌روزرسانی شده باشد

## Debug Commands:

### برای تست Hash Generation:
```csharp
var service = new ChangeDetectionService(repo, logger);
var hash1 = service.GenerateContentHash("Test Message 1");
var hash2 = service.GenerateContentHash("Test Message 2");
var hash3 = service.GenerateContentHash("test message 1"); // lowercase
// hash1 should equal hash3 (case insensitive)
// hash1 should NOT equal hash2
```

### Configuration بررسی:
```csharp
// در هر service که از IConfiguration استفاده می‌کند:
_logger.LogInformation("AutoProcessChanges: {Value}", 
    _configuration["ChangeDetectionSettings:AutoProcessChanges"]);
_logger.LogInformation("NotifyAdminOnChanges: {Value}", 
    _configuration["ChangeDetectionSettings:NotifyAdminOnChanges"]);
```

## احتمالی مشکلات:

### 1. اگر هنوز Change Detection کار نمی‌کند:
- چک کنید که `ITelegramClient` message ها را به درستی scrape می‌کند
- مطمئن شوید که `ProcessRawMessageAsync` فراخوانی می‌شود
- Log level را به `Debug` تغییر دهید

### 2. اگر Notification ها نمایش داده نمی‌شوند:
- جدول `AdminNotifications` وجود دارد؟
- Admin Panel صفحه notifications دارد؟
- SignalR برای real-time updates فعال است؟

### 3. اگر Hash های یکسان تولید می‌شوند:
- متن پیغام‌ها واقعاً متفاوت هستند؟
- Unicode characters یا invisible characters وجود دارند؟

## Next Steps:
1. برنامه را rebuild و restart کنید
2. Log level را به Information یا Debug تنظیم کنید
3. یک تغییر کوچک در کانال ایجاد کنید
4. Log ها را بررسی کنید
5. Database را چک کنید

اگر باز هم مشکل داشتید، محتوای log ها را ارسال کنید.
