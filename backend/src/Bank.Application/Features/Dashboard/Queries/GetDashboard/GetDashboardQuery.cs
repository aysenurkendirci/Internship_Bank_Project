using Bank.Contracts.Dashboard;
using MediatR;

namespace Bank.Application.Features.Dashboard.Queries.GetDashboard;

// Query: Sadece okuma işlemi (Veri çekme) yapar.
// Dışarıdan sadece UserId alır ve sonucunda DashboardResponse döner.
public sealed record GetDashboardQuery(long UserId) : IRequest<DashboardResponse>;
