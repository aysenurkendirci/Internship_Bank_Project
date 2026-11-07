namespace Bank.Application.Common.Exceptions;

/// <summary>
/// The idempotency key was already used for a different request. Answered with 422, as the IETF
/// Idempotency-Key draft recommends: replaying the stored result would hide that the new request was never run.
/// </summary>
public sealed class IdempotencyKeyReusedException : AppException
{
    public IdempotencyKeyReusedException()
        : base("Bu Idempotency-Key daha önce farklı bir istek için kullanıldı. Yeni bir istek için yeni bir anahtar gönderin.") { }
}
