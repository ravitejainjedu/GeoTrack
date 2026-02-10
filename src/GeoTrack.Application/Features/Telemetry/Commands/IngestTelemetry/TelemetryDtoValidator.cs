using FluentValidation;
using GeoTrack.Application.DTOs;

namespace GeoTrack.Application.Features.Telemetry.Commands.IngestTelemetry;

public class TelemetryDtoValidator : AbstractValidator<TelemetryDto>
{
    public TelemetryDtoValidator()
    {
        RuleFor(x => x.DeviceId).NotEmpty();

        RuleFor(x => x.Lat)
            .InclusiveBetween(-90, 90)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Lon)
            .InclusiveBetween(-180, 180)
            .WithMessage("Longitude must be between -180 and 180.");

        RuleFor(x => x.Timestamp)
            .NotEmpty()
            .WithMessage("Timestamp is required.");
    }
}
