using Plus.HabboHotel.Rooms;
using Xunit;

namespace Plus.Tests;

public class PositionRevisionTests
{
    [Fact]
    public void SettingTheSameCoordinateDoesNotAdvanceTheRevision()
    {
        var user = NewUser();
        var start = user.PositionRevision;

        user.X = 0;
        user.Y = 0;
        user.Z = 0;

        Assert.Equal(start, user.PositionRevision);
    }

    [Fact]
    public void ActualCoordinateChangesAdvanceTheRevisionOnceEach()
    {
        var user = NewUser();
        var start = user.PositionRevision;

        user.X = 3;
        Assert.Equal(start + 1, user.PositionRevision);

        user.Y = 4;
        Assert.Equal(start + 2, user.PositionRevision);

        user.Z = 1.5;
        Assert.Equal(start + 3, user.PositionRevision);

        // Re-assigning the same value is not a move.
        user.X = 3;
        Assert.Equal(start + 3, user.PositionRevision);
    }

    private static RoomUser NewUser() => new(1, 7u, 1, null!, null!, null!, null!);
}
