using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms;
using Plus.Utilities;
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

    [Fact]
    public void IncapacitatedUserOnBedUsesLiveBedHeightRotationAndCorrectZ()
    {
        var bed = Furni(60, InteractionType.Bed, WiredBoxType.None);
        bed.Definition.Height = 1.5;
        Assert.True(_room.GetRoomItemHandler().SetFloorItem(null, bed, 2, 2, 4, true, false, false));
        var actor = IncapacitationPlayer(_client, 2, 2);
        actor.SetPos(2, 2, bed.GetZ + 1);
        _client.GetHabbo().IsIncapacitated = true;

        actor.ReconcileIncapacitation();
        actor.ReconcileIncapacitation();

        Assert.True(actor.IsLying);
        Assert.Equal($"{TextHandling.GetString(bed.Definition.Height)} null", actor.Statusses["lay"]);
        Assert.Equal(bed.GetZ, actor.Z);
        Assert.Equal(bed.Rotation, actor.RotBody);
        Assert.Equal(bed.Rotation, actor.RotHead);

        _client.GetHabbo().IsIncapacitated = false;
        actor.ReconcileIncapacitation();

        Assert.False(actor.IsLying);
        Assert.False(actor.HasStatus("lay"));
    }

    [Fact]
    public void IncapacitatedUserRelocatedOffBedReconcilesFromTheNewFloor()
    {
        var bed = Furni(61, InteractionType.Bed, WiredBoxType.None);
        Assert.True(_room.GetRoomItemHandler().SetFloorItem(null, bed, 2, 2, 0, true, false, false));
        var actor = IncapacitationPlayer(_client, 2, 2);
        _client.GetHabbo().IsIncapacitated = true;
        actor.ReconcileIncapacitation();
        Assert.Equal(bed.GetZ, actor.Z);

        // A forced relocation (same-room hospital transfer) must not reuse the stale bed height.
        actor.SetPos(4, 4, 0);
        actor.ReconcileIncapacitation();

        Assert.True(actor.IsLying);
        Assert.Equal("1.0 null", actor.Statusses["lay"]);
        Assert.Equal(-0.35, actor.Z, 3);
    }

    [Fact]
    public void IncapacitationReconcilePreservesHealthyOrdinaryLay()
    {
        var actor = IncapacitationPlayer(_client, 2, 2);
        actor.SetStatus("lay", "2.0 null");
        actor.IsLying = true;
        var originalZ = actor.Z;

        actor.ReconcileIncapacitation();

        Assert.True(actor.IsLying);
        Assert.Equal("2.0 null", actor.Statusses["lay"]);
        Assert.Equal(originalZ, actor.Z);
    }

    [Fact]
    public void IncapacitationRebasesAfterForcedFloorRelocation()
    {
        var actor = IncapacitationPlayer(_client, 2, 2);
        actor.SetPos(2, 2, 3);
        _client.GetHabbo().IsIncapacitated = true;
        actor.ReconcileIncapacitation();
        actor.SetPos(3, 3, 1);
        actor.ReconcileIncapacitation();
        actor.ReconcileIncapacitation();
        Assert.Equal(0.65, actor.Z, 3);
        _client.GetHabbo().IsIncapacitated = false;
        actor.ReconcileIncapacitation();
        Assert.Equal(1, actor.Z);
    }

    [Fact]
    public void IncapacitationHospitalPlacementUsesTheV2OwnerAndLiveBedPosture()
    {
        _room.EnableV2Movement();
        var bed = Furni(62, InteractionType.Bed, WiredBoxType.None);
        bed.Definition.Height = 1.5;
        Assert.True(_room.GetRoomItemHandler().SetFloorItem(null, bed, 2, 2, 4, true, false, false));
        var actor = ExecutorActor(4, 4);
        _client.GetHabbo().IsIncapacitated = true;
        actor.ReconcileIncapacitation();
        _room.GetGameMap().Navigation.RunOwner(actor, (user, _) =>
        {
            _room.GetGameMap().TeleportToItem(user, bed);
            user.ReconcileIncapacitation();
        });
        ExecutorTick();
        Assert.Equal((bed.GetX, bed.GetY), (actor.X, actor.Y));
        Assert.Equal(bed.GetZ, actor.Z);
        Assert.Equal(bed.Rotation, actor.RotBody);
        Assert.Equal("1.5 null", actor.Statusses["lay"]);
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
