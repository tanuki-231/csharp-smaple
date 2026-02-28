using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models.Requests;

public class LoginRequest
{
    [Required, MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}
