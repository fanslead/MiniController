using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniController;
using MiniController.Attributes;

namespace SampleApi;

[MiniController()]
public class TestEndpoints
{
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ApiExplorerSettings(GroupName = "Admin")]
    public static IResult Get(string id)
    {
        return Results.Ok("Hello, World!");
    }

    [HttpPost("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public static IResult Post(string id)
    {
        return Results.Ok("Hello, World!");
    }

    [HttpGet("ABC/{id}")]
    public static IResult GetABC(string id)
    {
        return Results.Ok("Hello, ABC!");
    }
}
