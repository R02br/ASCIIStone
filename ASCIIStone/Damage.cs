using static Item;

public static class Damage
{
    public struct DamageData
    {
        public ItemTypes itemType;
        public bool hasAttacker;
        public uint attackerId;

        public int tileX;
        public int tileY;
    }

    public struct DamageOut
    {
        public DamagedObject damagedObject;
    }

    public enum DamagedObject
    {
        none,
        entity,
        tile
    }

    public static void DamageAt(DamageData damageData, Terrain terrain, bool send, out DamageOut damageOut)
    {
        damageOut = new DamageOut()
        {
            damagedObject = DamagedObject.none,
        };

        foreach (Entity entity in Game.entities.Values)
        {
            int x = (int)MathF.Floor(entity.x);
            int y = (int)MathF.Floor(entity.y);

            if (!damageData.hasAttacker || (damageData.attackerId != entity.id))
            {
                if (x == damageData.tileX && y == damageData.tileY)
                {
                    if (entity.entityType != Entity.EntityTypes.Dropped_item)
                    {
                        damageOut.damagedObject = DamagedObject.entity;
                        if (NetworkManager.isHost)
                        {
                            entity.Damage(ItemProperty.itemProperties[damageData.itemType].entityDamage, damageData);
                            return;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        if (NetworkManager.isHost || (damageOut.damagedObject == DamagedObject.none))
        {
            terrain.DamageTile(damageData, out bool usedDurabilityTile);

            if (damageOut.damagedObject == DamagedObject.none)
            {
                if (usedDurabilityTile)
                {
                    damageOut.damagedObject = DamagedObject.tile;
                }
            }
        }

        if (!send) return;

        if (NetworkManager.isHost) return;
        if (NetworkManager.serverConnection == null) return;

        NetworkManager.SendPacket(AttackPacket.Build(damageData), NetworkManager.serverConnection.TcpSocket);
    }
}