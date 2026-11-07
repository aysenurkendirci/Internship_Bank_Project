namespace Bank.Application.Common;

/// <summary>
/// A stored item together with its row version. The version is handed to clients and must be sent
/// back on update, so a change based on stale data is rejected instead of overwriting newer data.
/// </summary>
public sealed record Versioned<T>(T Value, byte[] Version);
