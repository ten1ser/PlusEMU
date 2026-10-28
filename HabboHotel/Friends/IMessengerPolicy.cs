using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Friends;

/// <summary>
/// Optional rules a plugin adds to the messenger. Every registered policy must allow an action; a denial is the notice the
/// user sees. With no policy registered the messenger behaves exactly as before.
/// </summary>
public interface IMessengerPolicy
{
    /// <summary>Checked before a private message is delivered; a notice blocks it.</summary>
    Task<string?> AuthorizeMessage(Habbo sender, int friendId);

    /// <summary>Runs once a private message has been delivered, for example to charge for it.</summary>
    Task MessageDelivered(Habbo sender, int friendId);

    /// <summary>Checked before a friend request is sent or accepted; a notice blocks it.</summary>
    string? AuthorizeFriendship(Habbo habbo, int otherUserId);
}

/// <summary>Shared friendship check, so the request and accept paths apply the same plugin rules.</summary>
public static class MessengerPolicies
{
    public static string? DenyFriendship(IEnumerable<IMessengerPolicy> policies, Habbo habbo, int otherUserId) =>
        policies.Select(policy => policy.AuthorizeFriendship(habbo, otherUserId)).FirstOrDefault(denial => denial != null);
}
