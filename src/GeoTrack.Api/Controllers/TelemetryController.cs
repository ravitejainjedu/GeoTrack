using System.Collections.Generic;
using System.Threading.Tasks;
using GeoTrack.Application.DTOs;
using GeoTrack.Application.Features.Telemetry.Commands.IngestTelemetry;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeoTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new() 
    { 
        PropertyNameCaseInsensitive = true 
    };

    private readonly IMediator _mediator;

    public TelemetryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    public async Task<IActionResult> Ingest([FromBody] System.Text.Json.JsonElement payload)
    {
        IEnumerable<TelemetryDto> points;

        try 
        {
            if (payload.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                if (payload.GetArrayLength() == 0)
                {
                    return BadRequest(new { message = "Payload cannot be an empty array." });
                }
                points = System.Text.Json.JsonSerializer.Deserialize<List<TelemetryDto>>(payload.GetRawText(), _jsonOptions) ?? new List<TelemetryDto>();
            }
            else if (payload.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var point = System.Text.Json.JsonSerializer.Deserialize<TelemetryDto>(payload.GetRawText(), _jsonOptions);
                points = point != null ? new[] { point } : Array.Empty<TelemetryDto>();
            }
            else 
            {
                return BadRequest(new { message = "Invalid payload format. Expected JSON object or array." });
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            return BadRequest(new { message = "JSON Deserialization failed.", details = ex.Message });
        }

        if (!points.Any())
        {
             return BadRequest(new { message = "No valid telemetry data found." });
        }

        var result = await _mediator.Send(new IngestTelemetryCommand(points));

        return Accepted(result);
    }
}
