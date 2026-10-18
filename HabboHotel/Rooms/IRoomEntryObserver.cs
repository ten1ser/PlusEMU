using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms;

public interface IRoomEntryObserver
{
    void Entered(GameClient session, Room room);
}
