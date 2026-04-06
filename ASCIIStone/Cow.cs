using static Damage;

public static class Cow
{
    public const char defaultChar = 'c';
    public const float defaultHealth = 10f;
    public const float defaultSpeed = 3.5f;

    public const float sendRate = 1 / 5f;

    public static DropLoot defaultDropLoot = new DropLoot(new DropChance[]
    {
        new DropChance(Item.ItemTypes.meat_raw, 1, 3, 1.0, Item.ToolTypes.none, Item.ItemTiers.none)
    });

    public static Entity CreateDefaultCow(uint id, float x, float y)
    {
        Entity cow = new Entity(id, Entity.EntityTypes.Cow, x, y, defaultChar, defaultHealth);
        cow.SetMeta("timeBeforeSend", 0f, false, Entity.metaDataTypes.FLOAT);

        return cow;
    }

    public static void Update(Entity entity)
    {
        if (!NetworkManager.isHost) return;

        Movement(entity);

        SendData(entity);
    }

    public static void Movement(Entity entity)
    {

    }

    public static void SendData(Entity entity)
    {
        entity.SendData(sendRate, false, true, true);
    }
}
