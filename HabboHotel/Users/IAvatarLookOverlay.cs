namespace Plus.HabboHotel.Users;

/// <summary>
/// A memory-only change to how an avatar looks (for example worn armor). Strip removes the overlay from a figure
/// before it is validated and saved, so users.look never contains it; Apply adds it back to the live look.
/// </summary>
public interface IAvatarLookOverlay
{
    string Strip(Habbo habbo, string figure);
    string Apply(Habbo habbo, string look);
}
