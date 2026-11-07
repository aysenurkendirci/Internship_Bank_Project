using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Bank.Application.Common.Behaviors;

/// <summary>
/// Runs every validator registered for a command or query before its handler. Handlers can then assume
/// their input is well-formed and contain only the use case itself.
/// </summary>
/// <remarks>
/// Request bodies keep their data annotations: the shape of the JSON (required fields, lengths, allowed
/// values) is checked at the HTTP edge. Validators here cover what belongs to the use case: query ranges,
/// rules that compare fields, and inputs that do not come from a body at all.
/// </remarks>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var failures = new List<ValidationFailure>();
        foreach (var validator in _validators)
            failures.AddRange((await validator.ValidateAsync(request, ct)).Errors);

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next(ct);
    }
}
