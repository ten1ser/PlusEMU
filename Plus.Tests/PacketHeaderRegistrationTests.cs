using Microsoft.Extensions.Logging.Abstractions;
using Plus.Communication.Attributes;
using Plus.Communication.Packets;
using Plus.Communication.Packets.Incoming;
using Plus.HabboHotel.GameClients;
using Xunit;

namespace Plus.Tests;

public class PacketHeaderRegistrationTests
{
    [Fact]
    public void ExplicitHeaderRegistersHandlerWithoutCoreConstant()
    {
        using var manager = Create(new PluginEvent());
        Assert.True(manager.IsRegistered(6030));
    }

    [Fact]
    public void HandlerNameStillResolvesCoreHeader()
    {
        using var manager = Create(new UniqueIdEvent());
        Assert.True(manager.IsRegistered(ClientPacketHeader.UniqueIdEvent));
    }

    [Fact]
    public void ExplicitHeaderTakesPrecedenceOverHandlerName()
    {
        using var manager = Create(new InfoRetrieveEvent());
        Assert.True(manager.IsRegistered(6031));
        Assert.False(manager.IsRegistered(ClientPacketHeader.InfoRetrieveEvent));
    }

    [Fact]
    public void HandlerWithoutAttributeOrCoreConstantIsSkipped()
    {
        using var manager = Create(new UnknownEvent());
        Assert.False(manager.IsRegistered(6030));
    }

    [Fact]
    public void DuplicateHeadersStillFailRegistration()
    {
        Assert.Throws<ArgumentException>(() => Create(new PluginEvent(), new PluginEvent()));
    }

    [Fact]
    public async Task ExplicitHeaderPreservesHandshakeAttributeAndDispatch()
    {
        var handler = new PluginEvent();
        using var manager = Create(handler);
        await manager.TryExecutePacket(null!, 6030, null!);
        Assert.True(handler.Parsed);
    }

    private static PacketManager Create(params IPacketEvent[] handlers) => new(handlers, NullLogger<PacketManager>.Instance);

    [PacketHeader(6030)]
    [NoAuthenticationRequired]
    private sealed class PluginEvent : IPacketEvent
    {
        public bool Parsed { get; private set; }
        public Task Parse(GameClient session, IIncomingPacket packet)
        {
            Parsed = true;

            return Task.CompletedTask;
        }
    }

    private sealed class UniqueIdEvent : IPacketEvent
    {
        public Task Parse(GameClient session, IIncomingPacket packet) => Task.CompletedTask;
    }

    [PacketHeader(6031)]
    private sealed class InfoRetrieveEvent : IPacketEvent
    {
        public Task Parse(GameClient session, IIncomingPacket packet) => Task.CompletedTask;
    }

    private sealed class UnknownEvent : IPacketEvent
    {
        public Task Parse(GameClient session, IIncomingPacket packet) => Task.CompletedTask;
    }
}
