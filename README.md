# OutboxPatterns & Event-Driven Architecture

Bu proje, **Outbox Design Pattern** (Outbox Tasarım Deseni) ve **Event-Driven Architecture** (Olay Güdümlü Mimari) kullanılarak .NET 9 üzerinde geliştirilmiş mikroservis tabanlı bir örnek uygulamadır. 

Proje, güvenilir mesajlaşma (reliable messaging) sağlamak amacıyla bir Web API servisi ve bu servisin asenkron olarak fırlattığı olayları (events) dinleyen bir Notification (Bildirim) Worker servisinden oluşmaktadır.

---

## 🏗️ Proje Yapısı

Proje temel olarak iki ana uygulamadan oluşmaktadır:

### 1. `OutboxPatterns` (Yayıncı - Publisher Web API)
Kullanıcı oluşturma işlemlerini yöneten ana API servisidir. Klasör yapısı Clean Architecture prensiplerine yakın bir yaklaşımla (uygulama ve altyapı katmanlarının ayrılması) kurgulanmıştır:
- **Application:** Uygulama iş kuralları (Use-Case/Command), DTO (`CreateUserRequest`, `CreateUserResponse`), Minimal API uç noktaları (`Endpoint.cs`) ve Fluent Validation (`CreateUserRequestValidator`) kurallarını barındırır.
- **Domain:** Veritabanı varlıkları (`Userss`, `OutboxTable`) ve Olay Kontratı (`UserCreatedEvent`, `Event.cs`) tanımlı.
- **Infrastructure:** Veritabanı bağlamı (`OutboxDbContext`), Migrations (Taşımalar) ve arkaplanda çalışan `OutboxProcessor` (Outbox'ı periyodik okuyup message broker'a event fırlatan background service) içerir.

### 2. `Notification` (Tüketici - Consumer Worker Service)
Olay güdümlü mimaride tüketici (consumer) rolünü üstlenir. Konsol uygulaması / Worker Service olarak kurgulanmıştır.
- Ana görevi `UserCreatedEvent` mesajlarını dinlemektir.
- `UserCreatedEventConsumer.cs` aracılığıyla, yeni bir kullanıcı oluşturulduğunda Service Bus'tan ilgili mesajı çeker ve simüle edilmiş bir bildirim logu düşer.

---

## 🚀 Kullanılan Teknolojiler ve Paketler

- **.NET 9.0:** Projenin çekirdek framework'ü.
- **Entity Framework Core 9 (EF Core SQL Server):** ORM aracı olarak kullanılmış, Code-First yaklaşımı ile veritabanı tabloları yönetilmektedir.
- **MassTransit (v8.5):** Dağıtık sistemlerde mesajlaşmayı soyutlayan ve yöneten framework. (Producer/Consumer yapıları için)
- **MassTransit.Azure.ServiceBus.Core:** Mesaj kuyruğu/broker altyapısı olarak **Azure Service Bus** kullanılmaktadır.
- **FluentValidation:** API isteklerindeki verilerin (DTO) validasyonu (doğrulaması) için kullanılmıştır.
- **Swagger / OpenAPI:** API dökümantasyonu ve test arayüzü sağlamaktadır.
- **Minimal APIs:** API uç noktaları (`/Users`) ASP.NET Core Minimal API kullanılarak sade ve performanslı bir şekilde ayağa kaldırılmıştır.

---

## 📐 Mimari ve Tasarım Desenleri (Design Patterns)

### 1. Transactional Outbox Pattern
Mikroservis mimarilerinde en büyük sorunlardan biri *Dual-Write* (Çift Yazma) problemidir: Sistemin, veritabanına veri kaydettikten hemen sonra diğer servislere mesaj (event) gönderirken çökmesi durumunda oluşacak veri tutarsızlığı.

Bu projede sorun şu şekilde çözülmüştür:
1. **Aynı Transaction (İşlem):** Yeni kullanıcı kaydı (`Userss`) ile yayınlanacak mesaj (`OutboxTable`) veritabanında **aynı transaction** içerisinde kaydedilir (`CreateUser.cs` - `BeginTransactionAsync`). Biri başarısız olursa diğeri de eklenmez (Rollback).
2. **Arkaplan İşleyicisi (Background Processor):** `OutboxProcessor` adında bir `BackgroundService`, periyodik olarak (10 saniyede bir) `OutboxTable`'a bakar.
3. **Mesajın Yayınlanması:** İşlenmemiş (ProcessedOn == null) mesajları bulur, MassTransit üzerinden Azure Service Bus'a iletir (`publishEndpoint.Publish`) ve başarıyla gönderildiğinde durumu günceller.

### 2. Event-Driven Architecture (Olay Güdümlü Mimari)
Sistemler arası doğrudan HTTP çağrısı (Tight Coupling - Sıkı Bağ) yerine, olaylar aracılığıyla haberleşme tercih edilmiştir:
- `OutboxPatterns` API'si "Kullanıcı Oluşturuldu" (UserCreated) olayını duyurur (Publish).
- `Notification` servisi "Kim oluşturdu, bana ne?" demez, sadece Azure Service Bus üzerindeki o olaya abone olur (Subscribe/Consume). Bu sayede servislerin birbirinin erişilebilir (Uptime) durumundan haberdar olmasına gerek kalmaz (Loose Coupling).

### 3. Dependency Injection (Bağımlılık Enjeksiyonu)
Her iki serviste de .NET'in built-in IoC Container'ı kullanılarak loglama, veritabanı bağlamları (`DbContext`), servis arayüzleri (`IUserService`), doğrulayıcılar (`IValidator`) ve MassTransit bileşenleri merkezi olarak inject edilmiştir.

### 4. Background Service (Worker Service)
`IHostedService` ve `BackgroundService` sınıflarından türetilen bileşenler (örn: `OutboxProcessor`) uygulamanın yaşam döngüsüyle beraber çalışarak arka planda asenkron görevleri yürütür.

---

## 🛠️ Nasıl Çalıştırılır?

1. Her iki projede de Service Bus (Azure Service Bus) için ConnectionString değerlerini (`appsettings.json` veya benzeri yerlerde) girdiğinizden emin olun.
2. `OutboxPatterns` projesinin EF Core bağlantı dizesinde uygun bir SQL Server adresi tanımlayın ve aşağıdaki komutları kullanarak veritabanını ayağa kaldırın:
   ```bash
   dotnet ef database update
   ```
3. API ve Notification projelerini ayağa kaldırarak uç noktalardan `/Users` POST isteği atıp uçtan uca Outbox ve Event mekanizmasını test edebilirsiniz.
