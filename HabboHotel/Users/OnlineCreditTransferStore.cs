using System.Data;
using Dapper;
using Plus.Database;
using Plus.HabboHotel.Users.Grants;

namespace Plus.HabboHotel.Users;

/// <summary>Writes both sides of an online Credits transfer in one locked transaction.</summary>
public interface IOnlineCreditTransferStore
{
    /// <summary>
    /// Locks both users rows ascending by id, writes both resulting Credits, runs the callback, then commits. Throwing
    /// from the callback rolls the whole transaction back. The error is a <see cref="GrantOutcome"/> code.
    /// </summary>
    (string? Error, int SenderBalance, int RecipientBalance) Transfer(int senderId, int senderValue, int recipientId, int recipientValue,
        Action<IDbConnection, IDbTransaction>? beforeCommit);
}

public sealed class OnlineCreditTransferStore(IDatabase database) : IOnlineCreditTransferStore
{
    public (string? Error, int SenderBalance, int RecipientBalance) Transfer(int senderId, int senderValue, int recipientId, int recipientValue,
        Action<IDbConnection, IDbTransaction>? beforeCommit)
    {
        using var connection = database.Connection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var ids = new[] { senderId, recipientId };

        // Both rows are locked in ascending id order so inverse transfers cannot deadlock, and a missing row refuses the transfer.
        if (connection.Query<int>("SELECT id FROM users WHERE id IN @ids ORDER BY id FOR UPDATE", new { ids }, transaction).Count() != 2) {
            return (GrantOutcome.UserNotFound, 0, 0);
        }

        connection.Execute("UPDATE users SET credits = CASE id WHEN @senderId THEN @senderValue WHEN @recipientId THEN @recipientValue END WHERE id IN @ids",
            new { senderId, senderValue, recipientId, recipientValue, ids }, transaction);

        beforeCommit?.Invoke(connection, transaction);
        transaction.Commit();

        return (null, senderValue, recipientValue);
    }
}
