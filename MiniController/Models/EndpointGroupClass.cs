using System.Collections.Generic;

namespace MiniController.Models;

public class EndpointGroupClass
{
    public string Namespace { get; set; } = string.Empty;

    public string ClassName { get; set; } = string.Empty;

    public string RoutePrefix { get; set; } = string.Empty;

    public string? FilterType { get; set; }

    public AuthorizeMetadata? Authorize { get; set; }

    public List<EndpointMethod> EndpointMethods { get; set; } = new();

    /// <summary>
    /// �Ƿ�Ϊ��̬�ࣨ���������ݣ�
    /// </summary>
    public bool IsStatic { get; set; }
}