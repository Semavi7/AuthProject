# 🔐 AuthProject

ASP.NET Core 10 üzerine inşa edilmiş, production-ready, tam kapsamlı bir **kimlik doğrulama ve yetkilendirme API'si**. JWT tabanlı oturum yönetimi, OTP doğrulama, şifre sıfırlama, çoklu cihaz oturumu ve Google OAuth2 desteği sunar.

---

## 📋 İçindekiler

- [Özellikler](#özellikler)
- [Teknoloji Stack'i](#teknoloji-stacki)
- [Proje Mimarisi](#proje-mimarisi)
- [Klasör Yapısı](#klasör-yapısı)
- [Veritabanı Şeması](#veritabanı-şeması)
- [API Endpoint'leri](#api-endpointleri)
- [Kimlik Doğrulama Akışları](#kimlik-doğrulama-akışları)
- [Kurulum ve Çalıştırma](#kurulum-ve-çalıştırma)
- [Konfigürasyon](#konfigürasyon)
- [User Secrets (Geliştirme Ortamı)](#user-secrets-geliştirme-ortamı)
- [Docker ile Çalıştırma](#docker-ile-çalıştırma)
- [Güvenlik Detayları](#güvenlik-detayları)
- [Event Sistemi (MediatR)](#event-sistemi-mediatr)
- [Validasyon Katmanı](#validasyon-katmanı)

---

## Özellikler

| Özellik | Açıklama |
|---|---|
| 📧 Email / 📱 SMS ile Kayıt & Giriş | Kullanıcılar email veya telefon numarası ile kayıt/giriş yapabilir |
| 🔑 JWT Access Token | 15 dakika ömürlü, HttpOnly cookie ile taşınan erişim tokeni |
| 🔄 Refresh Token Rotasyonu | 7 günlük, BCrypt ile hashlenmiş, tek kullanımlık yenileme tokeni |
| ✅ OTP Hesap Doğrulama | Kayıt sonrası 6 haneli OTP kodu ile hesap aktivasyonu (5 dk geçerli) |
| 🔒 Şifre Sıfırlama | OTP tabanlı iki aşamalı şifre sıfırlama akışı (15 dk geçerli) |
| 📤 OTP Yeniden Gönderme | Süresi dolmuş/kullanılmış OTP'ler iptal edilerek yeni kod gönderilir |
| 🌐 Google OAuth2 | Google hesabıyla tek tıkla sosyal giriş |
| 💻 Çoklu Cihaz Oturumu | Her cihaz için ayrı oturum kaydı, aktif oturumları listeleme |
| 🚪 Oturum Kapatma | Session bazlı tekil çıkış |
| 🛡️ FluentValidation | Action filter ile otomatik request doğrulama |
| 🐘 PostgreSQL | Npgsql üzerinden Entity Framework Core ile tam entegrasyon |
| 📖 Scalar API Docs | Geliştirme ortamında interaktif API dokümantasyonu |

---

## Teknoloji Stack'i

| Katman | Teknoloji / Kütüphane | Versiyon |
|---|---|---|
| **Framework** | ASP.NET Core | 10.0 |
| **Dil** | C# | 14.0 |
| **ORM** | Entity Framework Core + Npgsql | 10.0.x |
| **Kimlik Yönetimi** | ASP.NET Core Identity | 10.0.x |
| **JWT** | Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.x |
| **OAuth2** | Microsoft.AspNetCore.Authentication.Google | 10.0.x |
| **Şifre Hashleme** | BCrypt.Net-Next | 4.1.0 |
| **Event Bus** | MediatR | 14.0.0 |
| **Validasyon** | FluentValidation | 12.1.1 |
| **SMS Gönderimi** | Twilio | 7.14.3 |
| **Email Gönderimi** | System.Net.Mail (SMTP) | Built-in |
| **API Docs** | Scalar + Microsoft.AspNetCore.OpenApi | 2.13.x |
| **Veritabanı** | PostgreSQL | - |

---

## Proje Mimarisi

Proje, **katmanlı mimari (Layered Architecture)** prensibiyle tasarlanmıştır:

```
┌─────────────────────────────────────────┐
│            Controllers (API)            │  ← HTTP isteklerini karşılar
├─────────────────────────────────────────┤
│         Filters / Validators            │  ← Request doğrulama & filtreler
├─────────────────────────────────────────┤
│            Services (İş Mantığı)        │  ← Tüm iş kuralları burada
├─────────────────────────────────────────┤
│         Events (MediatR / CQRS)         │  ← Asenkron olaylar (OTP gönderim)
├─────────────────────────────────────────┤
│    Db (EF Core) + Entities + Enums      │  ← Veri erişim katmanı
└─────────────────────────────────────────┘
```

### Mimari Kararlar

- **MediatR Event/Handler** pattern'i ile OTP gönderim mantığı servis katmanından ayrıştırılmıştır. `AuthService` sadece event yayımlar; kim/nasıl gönderdiği `SendEmailOtpHandler` ve `SendSmsOtpHandler` sınıflarına delege edilir.
- **Repository pattern kullanılmamış**; EF Core `DbContext` doğrudan servis katmanında kullanılır (küçük ölçekli proje için pragmatik karar).
- **HttpOnly Cookie** stratejisi; frontend'e token göndermek yerine tarayıcının cookie'sine yazılır, XSS saldırılarına karşı koruma sağlar.

---

## Klasör Yapısı

```
AuthProject/
│
├── Controllers/
│   └── AuthController.cs          # Tüm auth endpoint'leri
│
├── Services/
│   ├── AuthService/
│   │   └── AuthService.cs         # Ana iş mantığı servisi
│   ├── EmailService/
│   │   ├── IEmailService.cs       # Email servis arayüzü
│   │   └── SmtpEmailService.cs    # Gmail SMTP implementasyonu
│   └── SmsSevice/
│       ├── ISmsService.cs         # SMS servis arayüzü
│       └── SmsService.cs          # Twilio SMS implementasyonu
│
├── Events/
│   ├── SendEmailOtpEvent.cs       # Email OTP yayım olayı
│   ├── SendEmailOtpHandler.cs     # Email OTP gönderim handler'ı
│   ├── SendSmsOtpEvent.cs         # SMS OTP yayım olayı
│   └── SendSmsOtpHandler.cs       # SMS OTP gönderim handler'ı
│
├── Entites/
│   ├── User.cs                    # Kullanıcı entity (IdentityUser'dan türetilmiş)
│   ├── UserSession.cs             # Cihaz oturum kayıtları
│   ├── Verify.cs                  # OTP doğrulama kayıtları
│   ├── ForgetPassword.cs          # Şifre sıfırlama kayıtları
│   ├── Socialite.cs               # OAuth sosyal giriş kayıtları
│   └── UserAddress.cs             # Kullanıcı adres bilgileri
│
├── Dtos/
│   ├── RegisterLoginDto.cs        # Kayıt/giriş isteği
│   ├── RegisterLoginResponseDto.cs# Kayıt/giriş yanıtı
│   ├── VerifyAccountDto.cs        # Hesap doğrulama isteği
│   ├── ResendOtpDto.cs            # OTP yeniden gönderme isteği
│   ├── ForgetPasswordRequestDto.cs# Şifre sıfırlama başlatma
│   └── ResetPasswordDto.cs        # Yeni şifre belirleme
│
├── Enums/
│   ├── UserStatus.cs              # Active, Inactive, Peding, Suspended, Blocked
│   ├── VerifyChannel.cs           # Email, Sms
│   ├── VerifyType.cs              # VerifyAccount, ForgetPassword
│   ├── VerifyStatus.cs            # Pedding, Complated
│   ├── DeviceType.cs              # Web, Mobile, vb.
│   ├── SocialiteType.cs           # Google, vb.
│   └── AddressType.cs             # Adres türleri
│
├── Validators/
│   ├── RegisterLoginDtoValidator.cs
│   ├── VerifyAccountDtoValidator.cs
│   ├── ResendOtpDtoValidator.cs
│   ├── ForgetPasswordRequestDtoValidator.cs
│   └── ResetPasswordDtoValidator.cs
│
├── Filters/
│   └── FluentValidationFilter.cs  # Global validasyon action filter'ı
│
├── Db/
│   └── ApplicationDbContext.cs    # EF Core DbContext
│
├── Migrations/                    # EF Core migration dosyaları
│
├── Program.cs                     # Uygulama başlangıç noktası & DI konfigürasyonu
├── appsettings.json               # Uygulama konfigürasyonu (hassas bilgiler hariç)
└── AuthProject.csproj             # Proje dosyası
```

---

## Veritabanı Şeması

```
┌──────────────────┐       ┌──────────────────┐
│      users       │       │   user_sessions  │
├──────────────────┤       ├──────────────────┤
│ Id (Guid) PK     │──┐    │ Id (Guid) PK     │
│ FirstName        │  │    │ UserId (Guid) FK ├──→ users.Id
│ LastName         │  │    │ RefreshTokenHash │
│ Email            │  │    │ DeviceId         │
│ PhoneNumber      │  │    │ DeviceType       │
│ Status           │  │    │ DeviceName       │
│ PhoneVerifyId    │  │    │ IpAdress         │
│ EmailVerifyId    │  │    │ UserAgent        │
│ CreatedAt        │  │    │ LastActiveAt     │
│ UpdateAt         │  │    │ ExpireAt         │
│ DeletedAt        │  │    │ RevokeAt         │
└──────────────────┘  │    │ CreatedAt        │
                      │    └──────────────────┘
                      │
                      │    ┌──────────────────┐
                      │    │    verifies      │
                      │    ├──────────────────┤
                      ├───→│ Id (Guid) PK     │
                      │    │ Channel (enum)   │
                      │    │ Type (enum)      │
                      │    │ UserId (Guid) FK │
                      │    │ Code (6 hane)    │
                      │    │ Status (enum)    │
                      │    │ AttemptCount     │
                      │    │ IpAdress         │
                      │    │ UserAgent        │
                      │    │ ExpiredAt        │
                      │    └──────────────────┘
                      │
                      │    ┌──────────────────┐
                      │    │ forget_passwords │
                      │    ├──────────────────┤
                      ├───→│ Id (Guid) PK     │
                      │    │ UserId (Guid) FK │
                      │    │ VerifyId (Guid)  │
                      │    │ ExpireAt         │
                      │    │ IsUsedAt         │
                      │    └──────────────────┘
                      │
                      │    ┌──────────────────┐
                      │    │   socialites     │
                      │    ├──────────────────┤
                      └───→│ Id (Guid) PK     │
                           │ Type (enum)      │
                           │ RefId            │
                           │ Email            │
                           │ UserId (Guid) FK │
                           │ Data (JSON)      │
                           └──────────────────┘
```

> **Not:** `verifies.Channel` alanı OTP'nin email ya da SMS kanalıyla gönderildiğini gösterir.  
> `verifies.AttemptCount` maks. 5 yanlış denemede kodu otomatik geçersiz kılar.

---

## API Endpoint'leri

**Base URL:** `http://localhost:{port}/api/auth`

### Genel Bakış

| Method | Endpoint | Auth | Açıklama |
|--------|----------|------|----------|
| `POST` | `/register` | Hayır | Yeni kullanıcı kaydı |
| `POST` | `/login` | Hayır | Email/telefon ile giriş |
| `POST` | `/verify-account` | Hayır | OTP ile hesap doğrulama |
| `POST` | `/resend-verification-otp` | Hayır | Yeni OTP kodu gönder |
| `POST` | `/forget-password` | Hayır | Şifre sıfırlama başlat |
| `POST` | `/reset-password` | Hayır | Yeni şifre belirle |
| `POST` | `/refresh` | Hayır | Access token yenile |
| `POST` | `/logout` | **Evet** | Oturumu kapat |
| `GET` | `/sessions` | **Evet** | Aktif oturumları listele |
| `GET` | `/google` | Hayır | Google OAuth başlat |
| `GET` | `/google/redirect` | Hayır | Google OAuth callback |

---

### 📝 Endpoint Detayları

#### `POST /register`
Yeni kullanıcı oluşturur ve OTP kodu gönderir. Hesap `Pending` durumunda başlar.

**Request Body:**
```json
{
  "email": "kullanici@example.com",
  "phone": null,
  "password": "Sifre123",
  "deviceType": 0,
  "deviceName": "Chrome Browser",
  "deviceId": "unique-device-id-123"
}
```
> `email` veya `phone`'dan en az biri zorunludur.

**Başarılı Yanıt `200`:**
```json
{
  "message": "Kayıt başarılı. Lütfen gönderilen kod ile hesabınızı doğrulayın.",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "kullanici@example.com",
    "phoneNumber": null,
    "firstName": null,
    "lastName": null,
    "status": 2
  }
}
```

---

#### `POST /login`
Kimlik bilgilerini doğrular ve `Authentication` + `RefreshToken` cookie'lerini tarayıcıya yazar.

**Request Body:**
```json
{
  "email": "kullanici@example.com",
  "phone": null,
  "password": "Sifre123",
  "deviceType": 0,
  "deviceName": "Chrome Browser",
  "deviceId": "unique-device-id-123"
}
```

**Başarılı Yanıt `200`:** *(Cookie'ler otomatik set edilir)*
```json
{
  "message": "Giriş başarılı.",
  "user": { ... }
}
```

| Cookie | HttpOnly | Secure | SameSite | Ömür |
|--------|----------|--------|----------|------|
| `Authentication` | ✅ | Prod'da ✅ | Strict | 15 dakika |
| `RefreshToken` | ✅ | Prod'da ✅ | Strict | 7 gün |

---

#### `POST /verify-account`
Kayıt sonrası gelen OTP kodu ile hesabı aktif eder.

**Request Body:**
```json
{
  "email": "kullanici@example.com",
  "phone": null,
  "code": "847291"
}
```

**Başarılı Yanıt `200`:**
```json
{
  "message": "Hesabınız başarıyla doğrulandı. Artık giriş yapabilirsiniz."
}
```

> ⚠️ 5 yanlış denemeden sonra kod otomatik geçersiz olur ve yeni kod talep edilmesi gerekir.

---

#### `POST /resend-verification-otp`
Mevcut bekleyen OTP'leri iptal ederek yeni bir kod gönderir.

**Request Body:**
```json
{
  "email": "kullanici@example.com",
  "phone": null
}
```

---

#### `POST /forget-password`
OTP kodu üretir, kullanıcıya gönderir ve ileriki adım için `verify_id` döner.

**Request Body:**
```json
{
  "email": "kullanici@example.com",
  "phone": null
}
```

**Başarılı Yanıt `200`:**
```json
{
  "message": "Şifre sıfırlama kodu gönderildi. Lütfen kodu kullanarak şifrenizi sıfırlayın.",
  "verify_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

---

#### `POST /reset-password`
`verify_id` ve OTP kodu ile şifreyi sıfırlar.

**Request Body:**
```json
{
  "verifyId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "code": "391847",
  "newPassword": "YeniSifre456"
}
```

> ⚠️ Kod 15 dakika geçerlidir ve maksimum 5 deneme hakkı vardır.

---

#### `POST /refresh`
`RefreshToken` cookie'sinden yeni bir Access Token üretir.  
Cookie otomatik okunur, body gönderilmesine gerek yoktur.

---

#### `POST /logout`
Mevcut oturumu veritabanında `RevokeAt` ile işaretler, cookie'leri siler.  
**Header:** `Authorization: Bearer <token>` (veya `Authentication` cookie)

---

#### `GET /sessions`
Giriş yapmış kullanıcının aktif tüm oturumlarını döner.  
**Header:** `Authorization: Bearer <token>` (veya `Authentication` cookie)

**Başarılı Yanıt `200`:**
```json
[
  {
    "id": "...",
    "deviceId": "unique-device-id-123",
    "deviceName": "Chrome Browser",
    "lastActiveAt": "2025-01-15T10:30:00Z",
    "expireAt": "2025-01-22T10:30:00Z",
    "ipAdress": "192.168.1.1",
    "createdAt": "2025-01-15T10:30:00Z"
  }
]
```

---

#### `GET /google`
Tarayıcıyı Google OAuth2 sayfasına yönlendirir.

#### `GET /google/redirect`
Google'dan dönen callback'i işler, kullanıcıyı oluşturur/bulur ve `http://localhost:3000/dashboard` adresine yönlendirir.

---

## Kimlik Doğrulama Akışları

### 1️⃣ Standart Kayıt & Giriş Akışı

```
Kullanıcı           API                  DB               Email/SMS
   │                 │                    │                    │
   │──POST /register─→│                   │                    │
   │                 │──Kullanıcı oluştur─→│                   │
   │                 │──OTP kaydet────────→│                   │
   │                 │──Event yayımla──────────────────────────→│
   │←──200 (pending)─│                    │                    │
   │                 │                    │                    │
   │──POST /verify───→│                   │                    │
   │                 │──OTP doğrula───────→│                   │
   │                 │──Status=Active─────→│                   │
   │←──200 (active)──│                    │                    │
   │                 │                    │                    │
   │──POST /login────→│                   │                    │
   │                 │──Oturum kaydet─────→│                   │
   │←──200 + Cookies─│                    │                    │
```

### 2️⃣ Token Yenileme Akışı

```
Kullanıcı           API                  DB
   │                 │                    │
   │──POST /refresh──→│  (Cookie okur)    │
   │                 │──Session bul───────→│
   │                 │──BCrypt verify─────→│
   │                 │──Yeni token üret    │
   │                 │──Hash güncelle─────→│
   │←──200 + Cookies─│                    │
```

### 3️⃣ Şifre Sıfırlama Akışı

```
Kullanıcı           API                   Email/SMS
   │                 │                        │
   │──POST /forget───→│                       │
   │                 │──OTP oluştur & gönder──→│
   │←──{verify_id}───│                        │
   │                 │                        │
   │──POST /reset────→│  {verifyId, code, newPassword}
   │                 │──OTP doğrula           │
   │                 │──Şifreyi güncelle      │
   │←──200───────────│                        │
```

### 4️⃣ Google OAuth Akışı

```
Kullanıcı (Tarayıcı)    API              Google              DB
       │                 │                  │                  │
       │──GET /google────→│                 │                  │
       │←──302 Redirect──│──────────────────→│                │
       │                                    │                  │
       │──Google ile giriş yap──────────────│                 │
       │←──Callback (code)──────────────────│                 │
       │                 │                  │                  │
       │──GET /redirect──→│                 │                  │
       │                 │──Kullanıcı bul/oluştur─────────────→│
       │                 │──Oturum kaydet─────────────────────→│
       │←──302 + Cookies→│                 │                   │
       │   (dashboard)   │                 │                   │
```

---

## Kurulum ve Çalıştırma

### Gereksinimler

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL](https://www.postgresql.org/download/) (v14+)
- Twilio hesabı (SMS için)
- Gmail hesabı + Uygulama Şifresi (Email için)
- Google Cloud Console projesi (OAuth2 için)

### 1. Depoyu Klonlayın

```bash
git clone <repo-url>
cd AuthProject
```

### 2. User Secrets Ayarlayın

Hassas bilgileri `appsettings.json`'a **koymayın**. Bunun yerine User Secrets kullanın:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=AuthDb;Username=postgres;Password=YOUR_PASSWORD"
dotnet user-secrets set "Jwt:Secret" "en-az-32-karakterlik-gizli-anahtar-buraya"
dotnet user-secrets set "Jwt:RefreshSecret" "en-az-32-karakterlik-refresh-gizli-anahtar"
dotnet user-secrets set "Smtp:Password" "gmail-uygulama-sifresi"
dotnet user-secrets set "Smtp:Username" "senin.mailin@gmail.com"
dotnet user-secrets set "Smtp:From" "senin.mailin@gmail.com"
dotnet user-secrets set "SmsProvider:TiwilioAccoundSid" "twilio-account-sid"
dotnet user-secrets set "SmsProvider:TiwilioAuthToken" "twilio-auth-token"
dotnet user-secrets set "SmsProvider:TwilioPhoneNumber" "+1234567890"
dotnet user-secrets set "Authentication:Google:ClientId" "google-client-id"
dotnet user-secrets set "Authentication:Google:ClientSecret" "google-client-secret"
```

### 3. Veritabanını Oluşturun

```bash
dotnet ef database update
```

### 4. Uygulamayı Çalıştırın

```bash
dotnet run
```

Uygulama başladıktan sonra Scalar API dokümantasyonuna erişin:

```
https://localhost:{port}/scalar
```

---

## Konfigürasyon

`appsettings.json` dosyası şablon olarak kullanılır. **Gerçek değerleri User Secrets veya environment variable ile sağlayın.**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=AuthDb;Username=postgres;Password=YOUR_PG_PASSWORD"
  },
  "Jwt": {
    "Secret": "EN_AZ_32_KARAKTER_GIZLI_ANAHTAR",
    "RefreshSecret": "EN_AZ_32_KARAKTER_REFRESH_ANAHTARI",
    "Issuer": "http://localhost:5000",
    "Audience": "http://localhost:5000"
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "Username": "senin.mailin@gmail.com",
    "Password": "GMAIL_UYGULAMA_SIFRESI",
    "From": "senin.mailin@gmail.com"
  },
  "SmsProvider": {
    "TiwilioAccoundSid": "TWILIO_ACCOUNT_SID",
    "TiwilioAuthToken": "TWILIO_AUTH_TOKEN",
    "TwilioPhoneNumber": "+1234567890"
  },
  "Authentication": {
    "Google": {
      "ClientId": "GOOGLE_CLIENT_ID",
      "ClientSecret": "GOOGLE_CLIENT_SECRET"
    }
  }
}
```

### Gmail Uygulama Şifresi Oluşturma

1. [Google Hesap Güvenliği](https://myaccount.google.com/security) sayfasına gidin
2. **2 Adımlı Doğrulama**'yı etkinleştirin
3. **Uygulama Şifreleri** bölümüne gidin
4. "Mail" için yeni şifre oluşturun
5. Oluşturulan 16 karakterlik şifreyi `Smtp:Password` olarak kullanın

### Google OAuth2 Kurulumu

1. [Google Cloud Console](https://console.cloud.google.com/) açın
2. Yeni proje oluşturun
3. **APIs & Services > Credentials** bölümüne gidin
4. **Create Credentials > OAuth 2.0 Client ID** seçin
5. Authorized Redirect URI olarak ekleyin: `https://localhost:{port}/signin-google`
6. `ClientId` ve `ClientSecret` değerlerini kopyalayın

### Twilio Kurulumu

1. [Twilio Console](https://console.twilio.com/) açın
2. **Account SID** ve **Auth Token**'ı kopyalayın
3. Twilio Phone Number edinin (Trial hesapta ücretsiz numara verilir)

---

## User Secrets (Geliştirme Ortamı)

Proje `UserSecretsId` ile yapılandırılmıştır:

```
5cdbe615-3c92-42ac-bc20-b834d0d5e50a
```

Secrets dosyası şu konumda saklanır:
- **Windows:** `%APPDATA%\Microsoft\UserSecrets\5cdbe615-3c92-42ac-bc20-b834d0d5e50a\secrets.json`
- **Linux/macOS:** `~/.microsoft/usersecrets/5cdbe615-3c92-42ac-bc20-b834d0d5e50a/secrets.json`

---

## Docker ile Çalıştırma

Proje, Linux container desteğiyle Docker'a hazır şekilde yapılandırılmıştır.

### docker-compose ile Çalıştırma

```yaml
# docker-compose.yml örneği
version: '3.8'
services:
  api:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=db;Database=AuthDb;Username=postgres;Password=postgres
      - Jwt__Secret=production-jwt-secret-min-32-chars
      - Jwt__Issuer=https://yourdomain.com
      - Jwt__Audience=https://yourdomain.com
    depends_on:
      - db

  db:
    image: postgres:16
    environment:
      POSTGRES_DB: AuthDb
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    volumes:
      - pg_data:/var/lib/postgresql/data

volumes:
  pg_data:
```

```bash
docker-compose up --build
```

---

## Güvenlik Detayları

### Token Güvenliği

| Özellik | Detay |
|---|---|
| Access Token Ömrü | 15 dakika (`ClockSkew = Zero` ile tam süre) |
| Refresh Token Ömrü | 7 gün |
| Token Taşıma | HttpOnly Cookie (XSS koruması) |
| Cookie SameSite | `Strict` (CSRF koruması) |
| Refresh Token Saklama | Veritabanında BCrypt hash olarak |
| Refresh Token Rotasyonu | Her yenilemede yeni token, eski geçersiz |

### OTP Güvenliği

| Özellik | Detay |
|---|---|
| Kod Uzunluğu | 6 hane |
| Üretim Yöntemi | `RandomNumberGenerator.GetInt32` (kriptografik) |
| Hesap Doğrulama Süresi | 5 dakika |
| Şifre Sıfırlama Süresi | 15 dakika |
| Maks. Yanlış Deneme | 5 (aşılınca kod geçersiz) |

### Şifre Güvenliği

- ASP.NET Core Identity ile yönetilir
- En az 6 karakter, en az 1 rakam zorunlu
- BCrypt ile hashli refresh token karşılaştırması

### Oturum Güvenliği

- Her cihaz için benzersiz `DeviceId` + `SessionId`
- Oturum iptalinde `RevokeAt` timestamp ile işaretleme
- Süresi dolmuş oturumlar `ExpireAt` kontrolüyle reddedilir

---

## Event Sistemi (MediatR)

OTP gönderimi MediatR `INotification` / `INotificationHandler` pattern'i ile gerçekleştirilir. Bu yapı sayesinde:
- `AuthService` sadece event yayımlar, gönderim detayından habersizdir.
- Email/SMS implementasyonları birbirinden bağımsız değiştirilebilir.
- İleride `Push Notification` gibi yeni kanallar kolayca eklenebilir.

```
AuthService
    └── _mediator.Publish(new SendEmailOtpEvent(email, code))
            └── SendEmailOtpHandler.Handle()
                    └── IEmailService.SendEmailAsync()

    └── _mediator.Publish(new SendSmsOtpEvent(phone, code))
            └── SendSmsOtpHandler.Handle()
                    └── ISmsService.SendSmsAsync()
```

---

## Validasyon Katmanı

`FluentValidationFilter`, bir `IAsyncActionFilter` olarak tüm controller action'larından **önce** çalışır ve ilgili `IValidator<T>` sınıfını DI container'dan çözümleyerek doğrulama yapar.

**Hata formatı `400 Bad Request`:**
```json
{
  "message": "Validasyon hatası",
  "errors": [
    {
      "field": "Password",
      "error": "Şifre en az 6 karakter olmalıdır."
    },
    {
      "field": "",
      "error": "Kayıt olmak veya giriş yapmak için Email veya Telefon numarası girmelisiniz."
    }
  ]
}
```

### Mevcut Validator'lar

| Validator | Kural |
|---|---|
| `RegisterLoginDtoValidator` | Şifre min. 6 karakter; email veya telefon zorunlu |
| `VerifyAccountDtoValidator` | OTP kodu ve email/telefon alanı zorunlu |
| `ResendOtpDtoValidator` | Email veya telefon zorunlu |
| `ForgetPasswordRequestDtoValidator` | Email veya telefon zorunlu |
| `ResetPasswordDtoValidator` | VerifyId, kod ve yeni şifre zorunlu |

---

## 📄 Lisans

Bu proje [MIT Lisansı](LICENSE) kapsamındadır.
