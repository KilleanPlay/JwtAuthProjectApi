## Compatibility
Bu branch, aşağıdaki branch’lerle tam uyumludur:
- Health API: `compat/auth-proxy-aug2025` (https://localhost:7270)
- Frontend: `compat/auth-health-aug2025` (http://localhost:3000)

### Ports
- Auth API: https://localhost:5001 (HTTP:5000)
- Health API: https://localhost:7270
- Frontend: http://localhost:3000

### Önemli Uçlar
- Login: POST /login (veya /User/login)
- Users: GET /User/users
- CRUD: POST /User/users, PUT /User/users/{id}, DELETE /User/users/{id}
- Health Proxy: GET /admin/health/details-proxy (RBAC: Admin/Manager)
