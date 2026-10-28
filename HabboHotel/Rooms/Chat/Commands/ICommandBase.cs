using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Rooms.Chat.Commands;

[Singleton]
public interface ICommandBase
{
    string Key { get; }
    string Parameters { get; }
    string Description { get; }

    /// <summary>
    /// Whether using this command counts towards the public-chat flood limit. Commands a player may use repeatedly
    /// without a cooldown opt out; every other command keeps the native chat flood behaviour.
    /// </summary>
    bool CountsTowardsChatFlood => true;
}
