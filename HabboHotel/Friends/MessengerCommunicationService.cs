using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms.Chat.Filter;
using Plus.HabboHotel.Users.Messenger;

namespace Plus.HabboHotel.Friends;

/// <summary>Delivers messenger output without packet or composer types; the communication layer renders it.</summary>
public interface IMessengerCommunicationOutput
{
    void InstantMessageError(GameClient session, MessengerMessageErrors error, int userId);
    void Notice(GameClient session, string text);
}

public interface IMessengerCommunicationService
{
    Task SendMessage(GameClient session, int friendId, string text);
    Task RequestFriend(GameClient session, string username);
}

public sealed class MessengerCommunicationService(
    IWordFilterManager wordFilter,
    IMessengerDataLoader messengerData,
    IQuestManager quests,
    IRewardTrackManager rewards,
    IMessengerCommunicationOutput output,
    IMessengerFriendMutationService friends,
    IEnumerable<IMessengerPolicy>? policies = null) : IMessengerCommunicationService
{
    private readonly IMessengerPolicy[] _policies = policies?.ToArray() ?? [];

    public async Task SendMessage(GameClient session, int friendId, string text)
    {
        var habbo = session.GetHabbo();
        var messenger = habbo.Messenger;
        var friend = messenger?.GetFriend(friendId);

        if (friend == null) {
            output.InstantMessageError(session, MessengerMessageErrors.NotFriends, friendId);
        }

        var message = wordFilter.CheckMessage(text);

        if (string.IsNullOrWhiteSpace(message)) {
            return;
        }

        if (habbo.TimeMuted > 0) {
            output.Notice(session, "Oops, you're currently muted - you cannot send messages.");

            return;
        }

        // Without a friend there is nothing to deliver to, so the messenger is never asked to send.
        if (messenger == null || friend == null) {
            return;
        }

        foreach (var policy in _policies) {
            if (await policy.AuthorizeMessage(habbo, friendId) is { } denial) {
                output.Notice(session, denial);

                return;
            }
        }

        var error = messenger.SendMessage(friend, message);

        if (error == null) {
            rewards.Progress(session, RewardTrackActions.SendMessengerMessage);

            foreach (var policy in _policies) {
                await policy.MessageDelivered(habbo, friendId);
            }
        }

        if (error == MessageError.Flooding) {
            output.Notice(session, "You cannot send a message, you have flooded the console.\n\nYou can send a message in 60 seconds.");
        }
    }

    public async Task RequestFriend(GameClient session, string username)
    {
        var (userId, blocked) = await messengerData.CanReceiveFriendRequests(username);

        if (userId == 0 || blocked) {
            return;
        }

        if (MessengerPolicies.DenyFriendship(_policies, session.GetHabbo(), userId) is { } denial) {
            output.Notice(session, denial);

            return;
        }

        // Accepting is decided under the account holds, so only a newly stored outgoing request earns the reward.
        var outcome = await friends.SendRequestAsync(session.GetHabbo(), userId);

        if (outcome.Error == null && !outcome.Accepted) {
            rewards.Progress(session, RewardTrackActions.RequestFriend);
        }

        // The attempt counts toward the social quest even when the messenger refuses it.
        quests.ProgressUserQuest(session, QuestType.SocialFriend);
    }
}
