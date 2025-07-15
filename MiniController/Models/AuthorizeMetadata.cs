namespace MiniController.Models;

public class AuthorizeMetadata
{
    public string? Policy { get; set; }

    public string? Roles { get; set; }

    public string? AuthenticationSchemes { get; set; }

    public bool AllowAnonymous { get; set; } = false;
}