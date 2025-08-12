JWT Auth API (ASP.NET Core + EF Core)
Kullanıcı yönetimi (login, rol-bazlı listeleme, CRUD) ve JWT ile kimlik doğrulama sağlayan ASP.NET Core Web API.

Özellikler
JWT ile kimlik doğrulama

Rol bazlı yetkilendirme

Admin, Manager: tüm kullanıcıları görebilir, ekle/güncelle/sil yapabilir

Chief, Staff: yalnızca Staff kullanıcılarını görebilir

Kullanıcı modeli: Username, Password, Role, opsiyonel Email, Phone

JWT payload’da role/email/phone claim’leri

EF Core + SQL Server, Username unique index

Gereksinimler
.NET SDK 8/9

SQL Server (LocalDB/Express/Developer)

(Geliştirme için) dotnet-ef araçları:

bash
Kopyala
Düzenle
dotnet tool install --global dotnet-ef
Hızlı Başlangıç
appsettings.json → connection string ve JWT ayarlarını doldur:

json
Kopyala
Düzenle
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=DESKTOP-PLE8UBQ;Database=JwtAuthDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "gizli_anahtar_burada_olacak_12345678",
    "Issuer": "JwtAuthApi",
    "Audience": "JwtAuthClient",
    "ExpireMinutes": 60
  },
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "AllowedHosts": "*"
}
Veritabanı şeması (ilk kurulum / model güncellemesi):

bash
Kopyala
Düzenle
# Migration oluştur (tek DbContext: AppDbContext)
dotnet ef migrations add InitialCreate --context AppDbContext
# Veritabanını güncelle
dotnet ef database update --context AppDbContext
Çalıştırma

bash
Kopyala
Düzenle
dotnet run
Konsolda:

nginx
Kopyala
Düzenle
Now listening on: http://localhost:5000
Now listening on: https://localhost:5001
Geliştirme sertifikasını güvenilir yapmak için (ilk sefer):

bash
Kopyala
Düzenle
dotnet dev-certs https --trust
Mimarî Özeti
Varlık (Entity)
User.cs

csharp
Kopyala
Düzenle
[Index(nameof(Username), IsUnique = true)]
public class User
{
    public int Id { get; set; }
    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    [Required, MaxLength(100)]
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }               // 0=Admin, 1=Chief, 2=Manager, 3=Staff
    [EmailAddress, MaxLength(254)] public string? Email { get; set; }
    [Phone, MaxLength(20)]        public string? Phone { get; set; }
}
UserRole.cs

csharp
Kopyala
Düzenle
public enum UserRole { Admin = 0, Chief = 1, Manager = 2, Staff = 3 }
DbContext
AppDbContext.cs

csharp
Kopyala
Düzenle
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}
    public DbSet<User> Users { get; set; }
}
JWT Üretimi
TokenService.cs

csharp
Kopyala
Düzenle
public string GenerateToken(string username, string role, string? email = null, string? phone = null)
{
    var claims = new List<Claim> {
        new Claim(ClaimTypes.Name, username),
        new Claim(ClaimTypes.Role, role),
        new Claim(JwtRegisteredClaimNames.Sub, username),
        new Claim("role", role) // kısa anahtar
    };
    if (!string.IsNullOrWhiteSpace(email)) {
        claims.Add(new Claim(ClaimTypes.Email, email));
        claims.Add(new Claim("email", email));
    }
    if (!string.IsNullOrWhiteSpace(phone)) {
        claims.Add(new Claim(ClaimTypes.MobilePhone, phone));
        claims.Add(new Claim("phone", phone));
    }
    ...
    return new JwtSecurityTokenHandler().WriteToken(token);
}
Program.cs (çekirdek yapı)
Kestrel: HTTP 5000 / HTTPS 5001

CORS: React için 3000 portu

JWT ayarları

Minimal /login endpoint’i

Örnek (kısaltılmış):

csharp
Kopyala
Düzenle
builder.WebHost.ConfigureKestrel(o => {
    o.ListenLocalhost(5000);
    o.ListenLocalhost(5001, lo => lo.UseHttps());
});

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(opt => {
    opt.AddPolicy("AllowReact", p => p
        .WithOrigins("http://localhost:3000", "https://localhost:3000")
        .AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt => { /* TokenValidationParameters ... */ });

app.UseCors("AllowReact");
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/login", async (LoginModel login, TokenService tokenService, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == login.Username && u.Password == login.Password);
    if (user is null) return Results.Unauthorized();

    var token = tokenService.GenerateToken(user.Username, user.Role.ToString(), user.Email, user.Phone);
    return Results.Ok(new { token });
});
Endpoint’ler
Tüm korumalı endpoint’lerde Authorization: Bearer <token> başlığı gerekir.

Auth
POST /login → Anonim

json
Kopyala
Düzenle
// Request
{ "username": "admin", "password": "1234" }

// Response
{ "token": "eyJhbGciOi..." }
Users (Controller: UserController)
GET /User/users → Authorize

Admin/Manager ⇒ tüm kullanıcılar

Chief/Staff ⇒ yalnızca Staff kullanıcıları

GET /User/{id} → Authorize

POST /User/create → Authorize (Admin,Manager)

json
Kopyala
Düzenle
{ "username":"x", "password":"y", "role":3, "email":"a@b.com", "phone":"0555..." }
PUT /User/update/{id} → Authorize (Admin,Manager)

DELETE /User/delete/{id} → Authorize (Admin,Manager)

Yetkilendirme Matrisi
Endpoint	Admin	Manager	Chief	Staff
POST /login	✔	✔	✔	✔
GET /User/users	✔ (tümü)	✔ (tümü)	✔ (yalnız Staff)	✔ (yalnız Staff)
GET /User/{id}	✔	✔	✔	✔
POST /User/create	✔	✔	✖	✖
PUT /User/update/{id}	✔	✔	✖	✖
DELETE /User/delete/{id}	✔	✔	✖	✖

JWT Payload Örneği
json
Kopyala
Düzenle
{
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "admin",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Admin",
  "sub": "admin",
  "role": "Admin",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "ornek@gmail.com",
  "email": "ornek@gmail.com",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone": "05553332211",
  "phone": "05553332211",
  "iss": "JwtAuthApi",
  "aud": "JwtAuthClient",
  "exp": 1754990789
}
Geliştirme Notları
Roles: [Authorize(Roles = "Admin,Manager")] gibi attribute’lar ClaimTypes.Role’u baz alır (TokenService bu claim’i ekler).

Token süresi: Jwt:ExpireMinutes. Production’da kısa tutmanız önerilir.

Kişisel veri (PII) token’da: zorunlu değilse koymayın; HTTPS zorunludur.

CORS: Geliştirmede 3000 portuna izin verildi. Farklı front-end portu için AllowReact politikasını güncelleyin.

Sorun Giderme
Email/Phone jwt.io’da görünmüyor

/login veya /User/login token üretiminde user.Email/user.Phone geçirildi mi?

DB’de ilgili alanlar dolu mu?

Yeniden login oldun mu (yeni token)?

“More than one DbContext was found”

Sadece AppDbContext kalsın veya komutlarda --context AppDbContext kullan.

HTTPS uyarısı

dotnet dev-certs https --trust

Kestrel 5000/5001 portları açık mı?