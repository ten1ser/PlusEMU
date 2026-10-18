using Plus.HabboHotel.Rooms;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    [Fact]
    public void IncapacitatedUserLiesAndRestoresWithoutSinkingOnRepeatedReconcile()
    {
        var actor = IncapacitationPlayer(_client, 2, 2);
        var baseZ = actor.Z;
        _client.GetHabbo().IsIncapacitated = true;

        actor.ReconcileIncapacitation();
        var lowered = actor.Z;
        actor.ReconcileIncapacitation();
        actor.ReconcileIncapacitation();

        Assert.True(actor.IsLying);
        Assert.True(actor.HasStatus("lay"));
        Assert.Equal("1.0 null", actor.Statusses["lay"]);
        Assert.Equal(lowered, actor.Z);
        Assert.True(lowered < baseZ);

        _client.GetHabbo().IsIncapacitated = false;
        actor.ReconcileIncapacitation();

        Assert.False(actor.IsLying);
        Assert.False(actor.HasStatus("lay"));
        Assert.Equal(baseZ, actor.Z);
    }

    [Fact]
    public void IncapacitatedUserIgnoresMoveDanceAndPostureCommands()
    {
        var actor = IncapacitationPlayer(_client, 2, 2);
        var actions = new RoomAvatarActionService(_interactionClock,
            Proxy<Plus.HabboHotel.Quests.IQuestManager>((method, _) => throw new InvalidOperationException(method)),
            TestNavigationRewards.Instance);
        _client.GetHabbo().IsIncapacitated = true;
        actor.ReconcileIncapacitation();

        actions.Move(_client, 3, 3);
        actions.Dance(_room, _client, 1);
        actions.SetPosture(_client, 1);
        actor.MoveTo(3, 3);

        Assert.Equal(2, actor.X);
        Assert.Equal(2, actor.Y);
        Assert.Equal(0, actor.DanceId);
        Assert.False(actor.IsSitting);
        Assert.True(actor.IsLying);
        Assert.False(actor.IsWalking);
    }

    [Fact]
    public void ReconcileIsNoOpForUnflaggedUserAndBot()
    {
        var user = IncapacitationPlayer(_client, 2, 2);
        var userZ = user.Z;
        var userStatuses = user.Statusses.Count;
        var bot = AcceptanceBot(3, 3, 900, admit: false);
        var botZ = bot.Z;
        var botStatuses = bot.Statusses.Count;

        user.ReconcileIncapacitation();
        bot.ReconcileIncapacitation();

        Assert.False(user.IsIncapacitated);
        Assert.False(user.IsLying);
        Assert.Equal(userZ, user.Z);
        Assert.Equal(userStatuses, user.Statusses.Count);
        Assert.False(bot.IsIncapacitated);
        Assert.False(bot.IsLying);
        Assert.Equal(botZ, bot.Z);
        Assert.Equal(botStatuses, bot.Statusses.Count);
    }

    private RoomUser IncapacitationPlayer(TestClient client, int x, int y)
    {
        var habbo = client.GetHabbo();
        habbo.Client = client;
        habbo.Effects = new(_interactionClock);
        habbo.Effects.Init(habbo);
        habbo.IgnoresComponent = new([]);
        Assert.True(_room.GetRoomUserManager().AddAvatarToRoom(client));
        var actor = Assert.IsType<RoomUser>(_room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id));
        actor.SetPos(x, y, 0);

        return actor;
    }
}
