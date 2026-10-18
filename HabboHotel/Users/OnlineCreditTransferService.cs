using System.Data;
using Microsoft.Extensions.Logging;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users.Authentication;
using Plus.HabboHotel.Users.Grants;

namespace Plus.HabboHotel.Users;

/// <summary>Balances involved in a committed transfer; the callback receives them just before the transaction commits.</summary>
public delegate void CreditTransferBeforeCommit(IDbConnection connection, IDbTransaction transaction,
    int senderBefore, int senderAfter, int recipientBefore, int recipientAfter);

/// <summary>Result of an online Credits transfer. <see cref="Error"/> is a stable machine-readable code.</summary>
public sealed record CreditTransferOutcome(bool Applied, string? Error, int SenderBalance = 0, int RecipientBalance = 0);

/// <summary>
/// Moves Credits between two online accounts as one atomic unit. The current live in-memory balances are authoritative,
/// so unsaved awards or purchases are included; both accounts' rows are written together and memory is only updated
/// after the transaction commits.
/// </summary>
public interface IOnlineCreditTransferService
{
    Task<CreditTransferOutcome> Transfer(GameClient sender, GameClient recipient, int amount, Func<bool> stillEligible,
        CreditTransferBeforeCommit? beforeCommit = null);
}

public sealed class OnlineCreditTransferService(IOnlineCreditTransferStore store, IAccountSessionGate sessionGate,
    IGameClientManager clients, ILogger<OnlineCreditTransferService> logger) : IOnlineCreditTransferService
{
    public const string InvalidAmount = "invalid_amount";
    public const string SelfTransfer = "self_transfer";
    public const string PlayerUnavailable = "player_unavailable";
    public const string WalletClosed = "wallet_closed";
    public const string NoLongerValid = "no_longer_valid";

    public async Task<CreditTransferOutcome> Transfer(GameClient sender, GameClient recipient, int amount, Func<bool> stillEligible,
        CreditTransferBeforeCommit? beforeCommit = null)
    {
        if (amount <= 0) {
            return new(false, InvalidAmount);
        }

        if (sender == null || recipient == null || ReferenceEquals(sender, recipient)) {
            return new(false, SelfTransfer);
        }

        var senderHabbo = sender.GetHabbo();
        var recipientHabbo = recipient.GetHabbo();

        if (senderHabbo == null || recipientHabbo == null || senderHabbo.Id <= 0) {
            return new(false, PlayerUnavailable);
        }

        if (senderHabbo.Id == recipientHabbo.Id) {
            return new(false, SelfTransfer);
        }

        if (!IsCurrent(sender, senderHabbo) || !IsCurrent(recipient, recipientHabbo)) {
            return new(false, PlayerUnavailable);
        }

        var senderId = senderHabbo.Id;
        var recipientId = recipientHabbo.Id;

        using var held = await sessionGate.EnterManyAsync(new[] { senderId, recipientId });

        // A session may have been replaced or logged out while the gate was pending.
        if (!IsCurrent(sender, senderHabbo) || !IsCurrent(recipient, recipientHabbo)) {
            return new(false, PlayerUnavailable);
        }

        var first = senderId < recipientId ? senderHabbo : recipientHabbo;
        var second = senderId < recipientId ? recipientHabbo : senderHabbo;

        lock (first.WalletSync)
        lock (second.WalletSync) {
            // The exact captured sessions must still be the live, authenticated clients for these ids under the wallet lock.
            if (!IsCurrent(sender, senderHabbo) || !IsCurrent(recipient, recipientHabbo)) {
                return new(false, PlayerUnavailable, senderHabbo.Credits, recipientHabbo.Credits);
            }

            if (senderHabbo.WalletClosed || recipientHabbo.WalletClosed) {
                return new(false, WalletClosed, senderHabbo.Credits, recipientHabbo.Credits);
            }

            // Corrupt balances outside the valid wire range are refused instead of arithmetically wrapped.
            if (GrantOutcome.CheckBalance(senderHabbo.Credits) is { } existingSenderError) {
                return new(false, existingSenderError, senderHabbo.Credits, recipientHabbo.Credits);
            }

            if (GrantOutcome.CheckBalance(recipientHabbo.Credits) is { } existingRecipientError) {
                return new(false, existingRecipientError, senderHabbo.Credits, recipientHabbo.Credits);
            }

            var senderAfter = (long)senderHabbo.Credits - amount;

            if (GrantOutcome.CheckBalance(senderAfter) is { } senderError) {
                return new(false, senderError, senderHabbo.Credits, recipientHabbo.Credits);
            }

            var recipientAfter = (long)recipientHabbo.Credits + amount;

            if (GrantOutcome.CheckBalance(recipientAfter) is { } recipientError) {
                return new(false, recipientError, senderHabbo.Credits, recipientHabbo.Credits);
            }

            // Preflight: refuse before touching the database when a participant already stopped being eligible.
            if (!stillEligible()) {
                return new(false, NoLongerValid, senderHabbo.Credits, recipientHabbo.Credits);
            }

            var senderBefore = senderHabbo.Credits;
            var recipientBefore = recipientHabbo.Credits;
            (string? Error, int SenderBalance, int RecipientBalance) result;

            try {
                result = store.Transfer(senderId, (int)senderAfter, recipientId, (int)recipientAfter, (connection, transaction) => {
                    // The receipt is written first; the final identity and eligibility check runs immediately before commit.
                    beforeCommit?.Invoke(connection, transaction, senderBefore, (int)senderAfter, recipientBefore, (int)recipientAfter);

                    if (!IsCurrent(sender, senderHabbo) || !IsCurrent(recipient, recipientHabbo) || !stillEligible()) {
                        throw new CreditTransferInvalidatedException();
                    }
                });
            }
            catch (CreditTransferInvalidatedException) {
                return new(false, NoLongerValid, senderBefore, recipientBefore);
            }

            if (result.Error != null) {
                return new(false, result.Error, senderBefore, recipientBefore);
            }

            // Memory is authoritative only after the transaction committed both rows.
            senderHabbo.Credits = result.SenderBalance;
            recipientHabbo.Credits = result.RecipientBalance;

            Notify(sender, senderHabbo, result.SenderBalance);
            Notify(recipient, recipientHabbo, result.RecipientBalance);

            return new(true, null, result.SenderBalance, result.RecipientBalance);
        }
    }

    private bool IsCurrent(GameClient client, Habbo habbo) => client is { IsAuthenticated: true } &&
        ReferenceEquals(client.GetHabbo(), habbo) && habbo.Id > 0 &&
        ReferenceEquals(clients.GetClientByUserId(habbo.Id), client) && ReferenceEquals(habbo.Client, client);

    private void Notify(GameClient client, Habbo habbo, int balance)
    {
        try {
            client.Send(new CreditBalanceComposer(balance));
        }
        catch (Exception exception) {
            // A committed transfer is not undone by a delivery failure.
            logger.LogWarning(exception, "Failed to send the Credits balance after a committed transfer to user {UserId}.", habbo.Id);
        }
    }
}

/// <summary>Signals that a participant stopped being eligible; the transaction must roll back.</summary>
internal sealed class CreditTransferInvalidatedException : Exception
{
    public CreditTransferInvalidatedException() : base("The credit transfer is no longer valid.") { }
}
