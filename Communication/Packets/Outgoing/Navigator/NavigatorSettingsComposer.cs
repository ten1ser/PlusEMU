using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Navigator;

public class NavigatorSettingsComposer : IServerPacket
{
    private readonly uint _homeroom;
    private readonly uint _roomToEnter;
    public uint MessageId => ServerPacketHeader.NavigatorSettingsComposer;

    public NavigatorSettingsComposer(uint homeroom, uint roomToEnter = 0)
    {
        _homeroom = homeroom;
        _roomToEnter = roomToEnter == 0 ? homeroom : roomToEnter;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteUInteger(_homeroom);
        packet.WriteUInteger(_roomToEnter);
    }
}
