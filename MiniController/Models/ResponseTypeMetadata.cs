namespace MiniController.Models;

public class ResponseTypeMetadata
{
    public int StatusCode { get; set; }
    public string? TypeName { get; set; }
    public string? ContentType { get; set; }
}