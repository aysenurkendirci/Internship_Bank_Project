using Bank.Application.Abstractions.Persistence;
using MediatR;

namespace Bank.Application.Common.Behaviors;

/// <summary>
/// Marks a command whose handler must run inside a single database transaction: either every change it makes
/// is stored, or none is.
/// </summary>
public interface ITransactionalCommand;

/// <summary>
/// Opens a transaction around commands marked <see cref="ITransactionalCommand"/> and commits it only when the
/// handler finishes without an exception. Any exception (a broken business rule, a lost race, a timeout)
/// leaves the transaction uncommitted, and disposing it rolls everything back.
/// </summary>
/// <remarks>
/// When the database picks the transaction as a deadlock victim it rolls it back itself and the whole command is
/// run again, a few times. Handlers are written for this: they read everything they need inside the transaction
/// and change nothing outside it, so a repeat starts from a clean state.
/// </remarks>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public const int MaxAttempts = 3;

    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Checked here rather than with a generic constraint, which the container would not filter on.
        if (request is not ITransactionalCommand)
            return await next(ct);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var transaction = await _unitOfWork.BeginAsync(ct);
                var response = await next(ct);
                await transaction.CommitAsync(ct);
                return response;
            }
            catch (Exception e) when (attempt < MaxAttempts && _unitOfWork.IsRetryable(e))
            {
                // Back off a little, longer each time, so the two transactions that collided do not collide again.
                await Task.Delay(TimeSpan.FromMilliseconds(25 * attempt), ct);
            }
        }
    }
}
