using System;
using System.Threading.Tasks;
using GeoTrack.Application.Features.Devices.Queries.GetDeviceDetail;
using GeoTrack.Application.Features.Devices.Queries.GetDeviceHistory;
using GeoTrack.Application.Features.Devices.Queries.GetDevices;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeoTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public DevicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetDevicesQuery());
        return Ok(result);
    }

    [HttpGet("{externalId}")]
    public async Task<IActionResult> GetDetail(string externalId)
    {
        var result = await _mediator.Send(new GetDeviceDetailQuery(externalId));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("{externalId}/latest")]
    public async Task<IActionResult> GetLatest(string externalId)
    {
        // "skip latest, the UI has to pull history just to show current location. Don’t."
        // We can reuse Detail query or specific query. Detail query already includes LatestLocation.
        // If the user specifically wants JUST the location:
        var detail = await _mediator.Send(new GetDeviceDetailQuery(externalId));
        if (detail == null) return NotFound();
        if (detail.LatestLocation == null) return NotFound(new { message = "No location data for this device" }); // Or 204? Usually 404 or 200 null is fine. Return DTO.
        return Ok(detail.LatestLocation);
    }

    [HttpGet("{externalId}/locations")]
    public async Task<IActionResult> GetHistory(
        string externalId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 1000,
        [FromQuery] string? cursor = null)
    {
        // Validation logic for params handled by FluentValidation if we hook it up, 
        // OR manual check here. MediatR pipeline validaton is cleaner.
        // But FromQuery binding needs to map to Request.

        // Defaults/Fixups:
        // "If only from is provided: allow to = now"
        // "If only to: allow from = to - 1h"

        var effectiveTo = to;
        var effectiveFrom = from;

        if (effectiveFrom.HasValue && !effectiveTo.HasValue)
        {
            effectiveTo = DateTime.UtcNow;
        }
        else if (effectiveTo.HasValue && !effectiveFrom.HasValue)
        {
            effectiveFrom = effectiveTo.Value.AddHours(-1);
        }

        var query = new GetDeviceHistoryQuery(externalId, effectiveFrom, effectiveTo, limit, cursor);

        try
        {
            var result = await _mediator.Send(query);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { message = "Validation failed", errors = ex.Errors });
        }
    }
}
