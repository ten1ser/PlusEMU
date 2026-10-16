namespace Plus.Communication.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PacketHeaderAttribute(uint id) : Attribute
{
    public uint Id { get; } = id;
}
