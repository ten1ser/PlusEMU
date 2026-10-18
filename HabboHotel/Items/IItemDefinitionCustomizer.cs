namespace Plus.HabboHotel.Items;

/// <summary>Adjusts a furniture definition after its database row is read and before the definition is published.</summary>
public interface IItemDefinitionCustomizer
{
    void Customize(ItemDefinition definition);
}
