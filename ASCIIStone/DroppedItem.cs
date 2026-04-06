public static class DroppedItem
{
    public static float resendTime = 1f;

    public static Entity CreateDroppedItem(uint id, float x, float y, Item item, bool instantPickup)
    {
        Entity entity = new Entity(id, Entity.EntityTypes.Dropped_item, x, y, ItemProperty.GetItemProperty(item).normalChar, 1f);
        entity.SetMeta("itemType", (int)item.itemType, false, Entity.metaDataTypes.INT);
        entity.SetMeta("count", item.count, false, Entity.metaDataTypes.INT);
        entity.SetMeta("health", item.health, false, Entity.metaDataTypes.INT);

        entity.SetMeta("timeBeforeSend", instantPickup ? 0f : resendTime, false, Entity.metaDataTypes.FLOAT);

        return entity;
    }

    public static void Update(Entity entity)
    {
        if (NetworkManager.isHost)
        {
            float time = entity.GetMetaAsFloat("timeBeforeSend", resendTime);
            if (time >= 0f)
            {
                entity.SetMeta("timeBeforeSend", time - Game.deltaTime, false, Entity.metaDataTypes.FLOAT);
            }

            CheckForPlayers(entity);
        }
    }
    
    public static void CheckForPlayers(Entity entity)
    {
        if (entity.GetMetaAsFloat("timeBeforeSend", resendTime) >= 0f)
        {
            return;
        }

        foreach (Entity player in Game.entities.Values)
        {
            if (player == null)
            {
                continue;
            }
            if (player.entityType != Entity.EntityTypes.Player)
            {
                continue;
            }
            if (player.health <= 0f)
            {
                continue;
            }
            if (MathF.Floor(player.x) != MathF.Floor(entity.x))
            {
                continue;
            }
            if (MathF.Floor(player.y) != MathF.Floor(entity.y))
            {
                continue;
            }

            entity.SetMeta("timeBeforeSend", resendTime, false, Entity.metaDataTypes.FLOAT);

            if (player.id == Player.myPlayerId)
            {
                Item item = new Item
                {
                    itemType = (Item.ItemTypes)entity.GetMetaAsInt("itemType", 0),
                    count = entity.GetMetaAsInt("count", 0),
                    health = entity.GetMetaAsInt("health", 0),
                };

                if (Inventory.CheckItem(item))
                {
                    Entity.DestroyEntity(entity.id, true);
                    Inventory.AddItem(item);
                }
            }
            else
            {
                NetworkManager.SendPacket(CheckPickup.Build(entity.id, new Item
                {
                    itemType = (Item.ItemTypes)entity.GetMetaAsInt("itemType", 0),
                    count = entity.GetMetaAsInt("count", 0),
                    health = entity.GetMetaAsInt("health", 0),
                }), NetworkManager.ConnectionByID[player.id].UdpEndpoint);
            }

            return;
        }
    }
}