using System.Collections.Generic;
using GeoTrack.Application.DTOs;
using MediatR;

namespace GeoTrack.Application.Features.Telemetry.Commands.IngestTelemetry;

public record IngestTelemetryCommand(IEnumerable<TelemetryDto> Points) : IRequest<IngestTelemetryResult>;

public record IngestTelemetryResult(int Accepted, int Duplicates, int Rejected);
