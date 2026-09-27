namespace Bank.Contracts.Cards;

public sealed record UpdateCardSettingsRequest(
    bool IsContactlessEnabled,
    bool IsOnlineEnabled
);
