using Plus.HabboHotel.Friends;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.FriendList;

internal class AcceptFriendEvent(IMessengerFriendMutationService friends, IMessengerCommunicationOutput? output = null, IEnumerable<IMessengerPolicy>? policies = null) : IPacketEvent
{
    private readonly IMessengerPolicy[] _policies = policies?.ToArray() ?? [];

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var amount = packet.ReadInt();

        if (amount > 50) {
            amount = 50;
        }
        else if (amount < 0) {
            return;
        }

        var requestIds = new int[amount];

        for (var i = 0; i < amount; i++) {
            requestIds[i] = packet.ReadInt();
        }

        foreach (var requestId in requestIds) {
            // A refused request stays pending, so it can still be accepted once the rule is met.
            if (MessengerPolicies.DenyFriendship(_policies, session.GetHabbo(), requestId) is { } denial) {
                output?.Notice(session, denial);

                return;
            }

            await friends.AcceptRequestAsync(session.GetHabbo(), requestId);
        }
    }
}
