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

    [Fact]
    public void AFreshUserHasNeverMoved()
    {
        var user = NewUser();

        Assert.Equal(RoomUser.NeverMovedTick, user.LastMovementTick);
    }

    [Fact]
    public void ActualCoordinateChangesStampTheMovementTick()
    {
        var user = NewUser();

        var beforeX = Environment.TickCount64;
        user.X = 3;
        var afterX = Environment.TickCount64;

        var xTick = user.LastMovementTick;
        Assert.NotEqual(RoomUser.NeverMovedTick, xTick);
        Assert.InRange(xTick, beforeX, afterX);

        var beforeY = Environment.TickCount64;
        user.Y = 4;
        var afterY = Environment.TickCount64;

        var yTick = user.LastMovementTick;
        Assert.InRange(yTick, beforeY, afterY);
        Assert.True(yTick >= xTick, "The movement tick must never go backwards.");

        var beforeZ = Environment.TickCount64;
        user.Z = 1.5;
        var afterZ = Environment.TickCount64;

        var zTick = user.LastMovementTick;
        Assert.InRange(zTick, beforeZ, afterZ);
        Assert.True(zTick >= yTick, "The movement tick must never go backwards.");
    }

    [Fact]
    public void UnchangedCoordinatesKeepTheMovementTick()
    {
        var user = NewUser();

        user.X = 3;
        user.Y = 4;
        user.Z = 1.5;

        var tick = user.LastMovementTick;
        var revision = user.PositionRevision;

        user.X = 3;
        user.Y = 4;
        user.Z = 1.5;

        Assert.Equal(tick, user.LastMovementTick);
        Assert.Equal(revision, user.PositionRevision);
    }

    [Fact]
    public void TurningDoesNotStampTheMovementTick()
    {
        var user = NewUser();

        user.X = 3;
        var tick = user.LastMovementTick;

        user.RotBody = 2;
        user.RotHead = 4;

        Assert.Equal(tick, user.LastMovementTick);
    }

    [Fact]
    public void MovementTicksAreMonotonicAcrossMoves()
    {
        var user = NewUser();

        var previous = user.LastMovementTick;

        for (var x = 1; x <= 5; x++) {
            user.X = x;
            var current = user.LastMovementTick;

            Assert.True(current >= previous, "The movement tick must never go backwards.");
            previous = current;
        }
    }

    private static RoomUser NewUser() => new(1, 7u, 1, null!, null!, null!, null!);
}
