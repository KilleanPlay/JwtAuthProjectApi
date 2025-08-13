JWT Auth Project API
1. Proje Açıklaması
Bu proje, ASP.NET Core ve Entity Framework Core kullanılarak geliştirilmiş bir JWT tabanlı kimlik doğrulama ve kullanıcı yönetim API’sidir.
API, kullanıcı girişi, rol bazlı yetkilendirme, CRUD işlemleri ve JWT token üretimi gibi özellikler sunar.

2. Temel Özellikler
JWT ile kimlik doğrulama

Rol bazlı yetkilendirme

Admin, Manager: Tüm kullanıcıları görüntüleme, ekleme, güncelleme ve silme yetkisine sahip

Chief, Staff: Yalnızca Staff kullanıcılarını görüntüleyebilir

Kullanıcı modeli

Username (zorunlu, benzersiz)

Password (zorunlu)

Role (Admin, Chief, Manager, Staff)

Opsiyonel: Email, Phone

JWT Payload içerikleri

Role, Email, Phone bilgileri claim olarak eklenir

EF Core + SQL Server desteği

3. Gereksinimler
.NET SDK 8/9

SQL Server (LocalDB, Express veya Developer sürümü)

dotnet-ef CLI aracı (veritabanı migration işlemleri için)

4. Kurulum Adımları
Bağımlılıkların Kurulması
bash
Kopyala
Düzenle
dotnet tool install --global dotnet-ef
appsettings.json Düzenleme
Connection string ve JWT ayarlarını kendi ortamınıza göre güncelleyin:

json
Kopyala
Düzenle
"ConnectionStrings": {
    "DefaultConnection": "Server=DESKTOP-PLE8UBQ;Database=JwtAuthDb;Trusted_Connection=True;TrustServerCertificate=True;"
},
"Jwt": {
    "Key": "gizli_anahtar_burada_olacak_12345678",
    "Issuer": "JwtAuthApi",
    "Audience": "JwtAuthClient",
    "ExpireMinutes": 60
}
Veritabanı Migration ve Güncelleme
bash
Kopyala
Düzenle
dotnet ef migrations add InitialCreate --context AppDbContext
dotnet ef database update --context AppDbContext
5. Çalıştırma
bash
Kopyala
Düzenle
dotnet run
Konsol çıktısı:

nginx
Kopyala
Düzenle
Now listening on: http://localhost:5000
Now listening on: https://localhost:5001
Geliştirme sertifikasını güvenilir yapmak için:

bash
Kopyala
Düzenle
dotnet dev-certs https --trust
6. Önemli Endpoint’ler
Kimlik Doğrulama
POST /login → Kullanıcı giriş yapar ve JWT token alır

Kullanıcı Yönetimi
GET /User/users → Rolüne göre kullanıcı listesi

GET /User/{id} → Tek bir kullanıcı bilgisi

POST /User/create → Yeni kullanıcı ekler (Admin, Manager)

PUT /User/update/{id} → Kullanıcı günceller (Admin, Manager)

DELETE /User/delete/{id} → Kullanıcı siler (Admin, Manager)

7. JWT Payload Örneği
json
Kopyala
Düzenle
{
    "name": "admin",
    "role": "Admin",
    "email": "ornek@gmail.com",
    "phone": "05553332211",
    "iss": "JwtAuthApi",
    "aud": "JwtAuthClient",
    "exp": 1754990789
}
8. Geliştirme Notları
[Authorize(Roles = "Admin,Manager")] gibi attribute’lar ClaimTypes.Role üzerinden çalışır.

Token süresi Jwt:ExpireMinutes ile belirlenir.

Kişisel veriler zorunlu olmadıkça token içinde taşınmamalıdır.

CORS politikası React için localhost:3000 portuna izin verir.
