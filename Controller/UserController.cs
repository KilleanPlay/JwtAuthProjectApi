using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("[controller]")] // => /User/*
public class UserController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly TokenService _tokenService;

    public UserController(AppDbContext dbContext, TokenService tokenService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    // ─────────────────────  LIST  ─────────────────────
    [Authorize]
    [HttpGet("users")] // GET /User/users
    public async Task<IActionResult> GetAllUsers()
    {
        var currentUser = HttpContext.User;
        var roleClaim = currentUser.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        if (!Enum.TryParse<UserRole>(roleClaim, true, out var userRole))
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
    [HttpGet("{id:int}")] // GET /User/{id}
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
            return NotFound("Kullanıcı bulunamadı");

        return Ok(new { user.Id, user.Username, user.Role, user.Email, user.Phone });
    }

    // ─────────────────────  LOGIN  ─────────────────────
    [HttpPost("login")] // POST /User/login   (Program.cs'deki /login de kullanılabilir)
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.Password == request.Password);

        if (user == null)
            return Unauthorized("Geçersiz kullanıcı adı veya şifre");

        var token = _tokenService.GenerateToken(
            user.Username,
            user.Role.ToString(),
            user.Email,
            user.Phone
        );

        return Ok(new { token });
    }

    // ─────────────────────  CREATE  ─────────────────────
    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("create")]              // eski:  POST /User/create
    [HttpPost("users")]               // yeni:  POST /User/users
    public async Task<IActionResult> CreateUser([FromBody] UserCreateRequest newUser)
    {
        if (string.IsNullOrWhiteSpace(newUser.Username))
            return BadRequest("Kullanıcı adı zorunlu.");

        if (await _dbContext.Users.AnyAsync(u => u.Username == newUser.Username))
            return BadRequest("Bu kullanıcı adı zaten mevcut");

        var role = newUser.Role; // string gönderilirse model binder enum'a çevirir (case-insensitive)

        var user = new User
        {
            Username = newUser.Username,
            Password = newUser.Password ?? string.Empty,
            Role = role,
            Email = newUser.Email,
            Phone = newUser.Phone
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

    // ─────────────────────  UPDATE  ─────────────────────
    [Authorize(Roles = "Admin,Manager")]
    [HttpPut("update/{id:int}")]      // eski:  PUT /User/update/{id}
    [HttpPut("users/{id:int}")]       // yeni:  PUT /User/users/{id}
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateRequest updateUser)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
            return NotFound("Kullanıcı bulunamadı");

        if (!string.IsNullOrWhiteSpace(updateUser.Username))
            user.Username = updateUser.Username;

        if (!string.IsNullOrWhiteSpace(updateUser.Password))
            user.Password = updateUser.Password; // boş/NULL gelirse şifreyi değiştirme

        if (updateUser.Role.HasValue)
            user.Role = updateUser.Role.Value;

        if (updateUser.Email != null)
            user.Email = updateUser.Email;

        if (updateUser.Phone != null)
            user.Phone = updateUser.Phone;

        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    // ─────────────────────  DELETE  ─────────────────────
    [Authorize(Roles = "Admin,Manager")]
    [HttpDelete("delete/{id:int}")]   // eski:  DELETE /User/delete/{id}
    [HttpDelete("users/{id:int}")]    // yeni:  DELETE /User/users/{id}
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

// ───────────── DTO’lar ─────────────

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserCreateRequest
{
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }  // "Admin"/"Manager"/"Chief"/"Staff" string'i de kabul edilir
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class UserUpdateRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public UserRole? Role { get; set; }   // NULL gelirse rolü değiştirme
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
