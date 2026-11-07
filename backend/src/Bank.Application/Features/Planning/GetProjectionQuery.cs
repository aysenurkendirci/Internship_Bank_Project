using Bank.Contracts.Finance;
using Bank.Domain.Projection;
using FluentValidation;
using MediatR;

namespace Bank.Application.Features.Planning;

/// <summary>Cash-flow timeline and monthly summary from tomorrow for the given number of months.</summary>
public sealed record GetProjectionQuery(int Months) : IRequest<ProjectionResponse>;

public sealed class GetProjectionQueryValidator : AbstractValidator<GetProjectionQuery>
{
    public GetProjectionQueryValidator()
    {
        RuleFor(q => q.Months)
            .InclusiveBetween(1, 12)
            .WithMessage("Projeksiyon süresi 1 ile 12 ay arasında olmalıdır.");
    }
}

/// <remarks>
/// All projection rules live in <see cref="CashFlowProjector"/>; this handler only gathers inputs and maps the result.
/// </remarks>
public sealed class GetProjectionQueryHandler : IRequestHandler<GetProjectionQuery, ProjectionResponse>
{
    private readonly ProjectionInputLoader _inputs;

    public GetProjectionQueryHandler(ProjectionInputLoader inputs) => _inputs = inputs;

    public async Task<ProjectionResponse> Handle(GetProjectionQuery query, CancellationToken ct)
    {
        var projection = CashFlowProjector.Project(await _inputs.LoadAsync(query.Months, ct));

        return new ProjectionResponse(
            projection.From,
            projection.To,
            projection.OpeningBalance.Currency.Code,
            projection.OpeningBalance.Amount,
            projection.Months.Select(ProjectionMapper.ToResponse).ToList(),
            projection.Entries.Select(ProjectionMapper.ToResponse).ToList(),
            projection.ExcludedItems);
    }
}
