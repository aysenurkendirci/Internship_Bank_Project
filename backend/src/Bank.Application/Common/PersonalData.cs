namespace Bank.Application.Common;

/// <summary>
/// Masking for personal data that has to appear in logs. Logs are read by more people and systems than
/// the database (operations, log shipping, support), so an identity number is never written in full:
/// enough remains to correlate events, not enough to identify the person (KVKK data minimisation).
/// </summary>
public static class PersonalData
{
    /// <summary>"10000000146" → "100******46".</summary>
    public static string MaskTcKimlikNo(string? tcNo)
    {
        if (string.IsNullOrEmpty(tcNo) || tcNo.Length < 6)
            return "***";

        return string.Concat(tcNo.AsSpan(0, 3), new string('*', tcNo.Length - 5), tcNo.AsSpan(tcNo.Length - 2));
    }
}
