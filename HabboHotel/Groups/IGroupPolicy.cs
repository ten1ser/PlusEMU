using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Groups;

/// <summary>
/// Optional group rules a plugin adds. A group the plugin manages keeps its rows in the native group tables, but only the
/// plugin changes its members, requests and settings: the native group packets leave it alone. With no policy registered
/// the group services behave exactly as before.
/// </summary>
public interface IGroupPolicy
{
    /// <summary>True when the plugin owns this group's membership and settings.</summary>
    bool Manages(int groupId);

    /// <summary>Checked before a player buys a native group; a notice blocks it.</summary>
    string? AuthorizePurchase(Habbo habbo);
}

/// <summary>Shared checks, so every group service applies the same plugin rules.</summary>
public static class GroupPolicies
{
    public static bool Managed(IGroupPolicy[] policies, int groupId) => policies.Any(policy => policy.Manages(groupId));

    public static string? DenyPurchase(IGroupPolicy[] policies, Habbo habbo) =>
        policies.Select(policy => policy.AuthorizePurchase(habbo)).FirstOrDefault(denial => denial != null);
}
