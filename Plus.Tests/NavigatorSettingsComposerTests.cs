using Plus.Communication.Packets.Outgoing.Navigator;
using Xunit;

namespace Plus.Tests;

public class NavigatorSettingsComposerTests
{
    [Fact]
    public void RoomToEnterDefaultsToTheHomeRoom()
    {
        var packet = new HabbiconTestSupport.RecordingPacket();

        new NavigatorSettingsComposer(7).Compose(packet);

        Assert.Equal(new object[] { 7u, 7u }, packet.Writes);
    }

    [Fact]
    public void RoomToEnterCanDifferFromTheHomeRoom()
    {
        var packet = new HabbiconTestSupport.RecordingPacket();

        new NavigatorSettingsComposer(7, 12).Compose(packet);

        Assert.Equal(new object[] { 7u, 12u }, packet.Writes);
    }
}
