using System.ComponentModel.DataAnnotations;

namespace Sofra.API.Options;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required] public string Host { get; set; } = string.Empty;
    [Range(1, 65535)] public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseSsl { get; set; }
    [Required, EmailAddress] public string FromAddress { get; set; } = string.Empty;
    [Required] public string FromName { get; set; } = string.Empty;
}
