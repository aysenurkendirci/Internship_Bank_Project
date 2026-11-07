using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Contracts.Dashboard;
using MediatR;

namespace Bank.Application.Features.Dashboard;

/// <remarks>
/// A pure read model: the repository returns the screen's shape straight from SQL, with no domain
/// objects in between, because nothing here is changed.
/// </remarks>
public sealed record GetDashboardQuery : IRequest<DashboardResponse>;

public sealed class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardResponse>
{
    private readonly IDashboardRepository _dashboard;
    private readonly ICurrentUser _currentUser;

    public GetDashboardQueryHandler(IDashboardRepository dashboard, ICurrentUser currentUser)
    {
        _dashboard = dashboard;
        _currentUser = currentUser;
    }

    public Task<DashboardResponse> Handle(GetDashboardQuery query, CancellationToken ct)
        => _dashboard.GetDashboardAsync(_currentUser.UserId, ct);
}
