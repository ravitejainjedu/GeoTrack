using FluentValidation;

namespace GeoTrack.Application.Features.Devices.Queries.GetDeviceHistory;

public class GetDeviceHistoryQueryValidator : AbstractValidator<GetDeviceHistoryQuery>
{
    public GetDeviceHistoryQueryValidator()
    {
        RuleFor(x => x.ExternalId).NotEmpty();

        RuleFor(x => x.Limit)
            .GreaterThan(0)
            .LessThanOrEqualTo(5000)
            .WithMessage("Limit must be between 1 and 5000.");

        RuleFor(x => x)
            .Must(x => !x.From.HasValue || !x.To.HasValue || x.From <= x.To)
            .WithMessage("'From' date must be before or equal to 'To' date.");
    }
}
