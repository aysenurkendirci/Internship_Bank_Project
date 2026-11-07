namespace Bank.Application.Imports;

/// <param name="LineNumber">1-based line in the file, so errors can point the user to it.</param>
/// <param name="Amount">Signed: money in is positive, money out negative.</param>
public sealed record StatementLine(int LineNumber, DateOnly Date, string Description, decimal Amount, string? Category);

public sealed record StatementLineError(int LineNumber, string Message);

public sealed record StatementParseResult(IReadOnlyList<StatementLine> Lines, IReadOnlyList<StatementLineError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
