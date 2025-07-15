namespace MiniController.Models;

public class ControllerMetadata
{
    public string RoutePrefix { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public string? FilterType { get; set; }
    public AuthorizeMetadata? Authorize { get; set; }
    public ApiExplorerSettingsMetadata? ApiExplorerSettings { get; set; }
}