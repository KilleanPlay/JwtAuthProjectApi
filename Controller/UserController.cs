using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly TokenService _tokenService;

    public UserController(AppDbContext dbContext, TokenService tokenService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    [Authorize]
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var currentUser = HttpContext.User;
        var roleClaim = currentUser.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        if (!Enum.TryParse<UserRole>(roleClaim, out var userRole))
            return Forbid();

        if (userRole == UserRole.Admin || userRole == UserRole.Manager)
        {
            var allUsers = await _dbContext.Users
                .Select(u => new { u.Id, u.Username, u.Role, u.Email, u.Phone })
                .ToListAsync();
            return Ok(allUsers);
        }
        else if (userRole == UserRole.Chief || userRole == UserRole.Staff)
        {
            var staffUsers = await _dbContext.Users
                .Where(u => u.Role == UserRole.Staff)
                .Select(u => new { u.Id, u.Username, u.Role, u.Email, u.Phone })
                .ToListAsync();
            return Ok(staffUsers);
        }

        return Forbid();
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
            return NotFound("Kullanıcı bulunamadı");

        return Ok(new { user.Id, user.Username, user.Role, user.Email, user.Phone });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.Password == request.Password);

        if (user == null)
            return Unauthorized("Geçersiz kullanıcı adı veya şifre");

        // ⬇️ E-posta ve telefon claim'lerini de token'a ekliyoruz
        var token = _tokenService.GenerateToken(
            user.Username,
            user.Role.ToString(),
            user.Email,
            user.Phone
        );

        return Ok(new { token });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("create")]
    public async Task<IActionResult> CreateUser([FromBody] UserCreateRequest newUser)
    {
        if (await _dbContext.Users.AnyAsync(u => u.Username == newUser.Username))
            return BadRequest("Bu kullanıcı adı zaten mevcut");

        var user = new User
        {
            Username = newUser.Username,
            Password = newUser.Password,
            Role = newUser.Role,
            Email = newUser.Email,   // ⬅️ opsiyonel
            Phone = newUser.Phone    // ⬅️ opsiyonel
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new
        {
            user.Id,
            user.Username,
            user.Role,
            user.Email,
            user.Phone
        });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPut("update/{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateRequest updateUser)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
            return NotFound("Kullanıcı bulunamadı");

        user.Username = updateUser.Username ?? user.Username;
        user.Password = updateUser.Password ?? user.Password;
        user.Role = updateUser.Role;
        user.Email = updateUser.Email ?? user.Email;   // ⬅️ opsiyonel
        user.Phone = updateUser.Phone ?? user.Phone;   // ⬅️ opsiyonel

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
            return NotFound("Kullanıcı bulunamadı");

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserCreateRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    public string? Email { get; set; }   // opsiyonel
    public string? Phone { get; set; }   // opsiyonel
}

public class UserUpdateRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public UserRole Role { get; set; }

    public string? Email { get; set; }   // opsiyonel
    public string? Phone { get; set; }   // opsiyonel
}
