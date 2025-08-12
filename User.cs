using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Index(nameof(Username), IsUnique = true)]
public class User
{
    public int Id { get; set; } // PRIMARY KEY

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    // Opsiyonel alanlar (JWT'ye claim olarak eklenecek)
    [EmailAddress, MaxLength(254)]
    public string? Email { get; set; }

    [Phone, MaxLength(20)]
    public string? Phone { get; set; }
}
