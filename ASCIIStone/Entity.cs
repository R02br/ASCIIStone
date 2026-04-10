using static Game;
using static Damage;

public class Entity
{
    public static Dictionary<uint, Path> paths = new Dictionary<uint, Path>();

    public Entity(uint id, EntityTypes entityType, float x, float y, char character, float health)
    {
        this.id = id;
        this.entityType = entityType;
        this.x = x;
        this.y = y;
        this.character = character;
        this.health = health;
    }

    public static void CreateEntity(Entity entity, bool send, bool sendMeta, bool instantSpawn)
    {
        AddEntity(entity, instantSpawn);

        if (!send) return;

        SendEntityAsNew(entity, sendMeta);
    }

    private static void AddEntity(Entity entity, bool instantSpawn)
    {
        if (instantSpawn)
        {
            entities[entity.id] = entity;
        }
        else
        {
            entitiesToCreate.Enqueue(entity);
        }
    }

    public static void DestroyEntity(uint id, bool send)
    {
        RemoveEntity(id);

        if (!send) return;

        if (NetworkManager.isHost)
        {
            NetworkManager.SendToAllClients(DestroyEntityPacket.Build(id), true, null);
        }
        else
        {
            if (NetworkManager.TcpSocket != null)
            {
                NetworkManager.SendPacket(DestroyEntityPacket.Build(id), NetworkManager.TcpSocket);
            }
        }
    }

    private static void RemoveEntity(uint id)
    {
        entitiesToDestroy.Enqueue(id);
    }

    public static void SpawnEntityInTerrain(Entity entity, Terrain terrain)
    {
        do
        {
            entity.x = Random.Shared.Next(0, terrain.terrainWidth);
            entity.y = Random.Shared.Next(0, terrain.terrainHeight);
        }
        while (!ValidatePosition(entity.x, entity.y, terrain));
    }

    public static bool ValidatePosition(float x, float y, Terrain terrain)
    {
        Tile? tile = terrain.GetTileAt(x, y);

        if (tile == null) { return false; }

        return !TileProperty.GetTileProperty(tile.Value).hasCollision;
    }

    public static void MoveEntityWithCollisionsInTerrain(Entity entity, Terrain terrain, float moveX, float moveY)
    {
        entity.x += moveX;
        if (!ValidatePosition(entity.x, entity.y, terrain))
        {
            entity.x -= moveX;
        }

        entity.y += moveY;
        if (!ValidatePosition(entity.x, entity.y, terrain))
        {
            entity.y -= moveY;
        }
    }

    public static void SendEntityAsNew(Entity entity, bool sendMeta)
    {
        if (NetworkManager.isHost)
        {
            NetworkManager.SendToAllClients(CreateEntityPacket.Build(entity), true, null);

            if (sendMeta)
            {
                SendCorrespondingMeta(entity);
            }
        }
    }

    public static void CalculatePath(Entity entity, int destinationX, int destinationY)
    {
        int entityX = (int)MathF.Floor(entity.x);
        int entityY = (int)MathF.Floor(entity.y);

        paths[entity.id] = Pathfinding.GetPath(entityX, entityY, destinationX, destinationY, Game.mainTerrain);
    }

    public static bool TryGetPath(Entity entity, out Path? path)
    {
        path = null;

        if (NetworkManager.isHost)
        {
            if (paths.ContainsKey(entity.id))
            {
                path = paths[entity.id];
                return true;
            }
        }

        return false;
    }

    public static PathTile? GetNextPathTile(Entity entity)
    {
        if (!TryGetPath(entity, out Path? path)) { return null; }

        if (path == null) { return null; }

        int entityX = (int)MathF.Floor(entity.x);
        int entityY = (int)MathF.Floor(entity.y);

        for (int i = 0; i < path.pathTiles.Length; i++)
        {
            if ((entityX == path.pathTiles[i].x) && (entityY == path.pathTiles[i].y))
            {
                if (i < (path.pathTiles.Length - 1))
                {
                    return path.pathTiles[i + 1];
                }
                else
                {
                    return null;
                }
            }
        }

        return null;
    }

    public static void SendCorrespondingMeta(Entity entity)
    {
        switch (entity.entityType)
        {
            case EntityTypes.Player:
                NetworkManager.SendToAllClients(EntityMetaData.Build(entity.id, "hunger", entity.GetMetaAsFloat("hunger", 0f), metaDataTypes.FLOAT), true, null);
                break;
            case EntityTypes.Dropped_item:
                NetworkManager.SendToAllClients(EntityMetaData.Build(entity.id, "itemType", entity.GetMetaAsInt("itemType", 0), metaDataTypes.INT), true, null);
                NetworkManager.SendToAllClients(EntityMetaData.Build(entity.id, "count", entity.GetMetaAsInt("count", 0), metaDataTypes.INT), true, null);
                NetworkManager.SendToAllClients(EntityMetaData.Build(entity.id, "health", entity.GetMetaAsInt("health", 0), metaDataTypes.INT), true, null);

                break;
        }
    }

    public void SendData(float sendRate, bool useTcp, bool sendPosition, bool sendHealth)
    {
        object? timeObj;

        if (!(TryGetMeta("timeBeforeSend", out timeObj)))
        {
            timeObj = 0f;
        }

        if (timeObj == null) return;

        float time = (float)timeObj;

        time -= Game.deltaTime;

        if (time < 0)
        {
            time = sendRate - Random.Shared.NextSingle() * 0.2f;

            if (NetworkManager.isHost)
            {
                if (sendPosition)
                {
                    NetworkManager.SendToAllClients(EntityPosition.Build(this), useTcp, null);
                }
                if (sendHealth)
                {
                    NetworkManager.SendToAllClients(EntityHealth.Build(this), useTcp, null);
                }
            }
            else
            {
                if (useTcp)
                {
                    if (sendPosition)
                    {
                        NetworkManager.SendPacket(EntityPosition.Build(this), NetworkManager.serverConnection?.TcpSocket);
                    }
                    if (sendHealth)
                    {
                        NetworkManager.SendPacket(EntityHealth.Build(this), NetworkManager.serverConnection?.TcpSocket);
                    }
                }
                else
                {
                    if (NetworkManager.serverUdpEndpoint == null) return;

                    if (sendPosition)
                    {
                        NetworkManager.SendPacket(EntityPosition.Build(this), NetworkManager.serverUdpEndpoint);
                    }
                    if (sendHealth)
                    {
                        NetworkManager.SendPacket(EntityHealth.Build(this), NetworkManager.serverUdpEndpoint);
                    }
                }
            }
        }

        SetMeta("timeBeforeSend", time, false, metaDataTypes.FLOAT);
    }

    public enum EntityTypes
    {
        Player,
        Dropped_item,
        Zombie,
        Cow,
    }

    public uint id;
    public EntityTypes entityType;

    public float x;
    public float y;

    public char character;

    public float health;

    public enum metaDataTypes
    {
        INT,
        FLOAT,
        STRING,
    }
    public Dictionary<string, object> metaData = new Dictionary<string, object>();

    public void SetMeta(string name, object data, bool send, metaDataTypes typeToSend)
    {
        metaData[name] = data;

        if (!send) return;

        if (NetworkManager.isHost)
        {
            NetworkManager.SendToAllClients(EntityMetaData.Build(id, name, data, typeToSend), true, null);
        }
        else
        {
            if (NetworkManager.serverConnection != null)
            {
                NetworkManager.SendPacket(EntityMetaData.Build(id, name, data, typeToSend), NetworkManager.serverConnection.TcpSocket);
            }
        }
    }

    public bool TryGetMeta(string name, out object? data)
    {
        return metaData.TryGetValue(name, out data);
    }

    public int GetMetaAsInt(string name, int defaultValue)
    {
        if (TryGetMeta(name, out object? value))
        {
            if (value != null)
            {
                defaultValue = (int)value;
            }
        }

        return defaultValue;
    }

    public float GetMetaAsFloat(string name, float defaultValue)
    {
        if (TryGetMeta(name, out object? value))
        {
            if (value != null)
            {
                defaultValue = (float)value;
            }
        }

        return defaultValue;
    }

    public void Damage(float damage, DamageData damageData)
    {
        health -= damage;

        if (NetworkManager.isHost)
        {
            NetworkManager.SendToAllClients(EntityHealth.Build(this), true, null);
        }

        if (health <= 0)
        {
            health = 0;

            if (NetworkManager.isHost)
            {
                switch (entityType)
                {
                    case EntityTypes.Player:
                        Player.Die(this, damageData);
                        break;
                    case EntityTypes.Zombie:
                        Zombie.Die(this, damageData);
                        break;
                    case EntityTypes.Cow:
                        Cow.Die(this, damageData);
                        break;
                }

            }

            if (entityType == EntityTypes.Player) return; //players can respawn

            if (NetworkManager.isHost)
            {
                DestroyEntity(id, true);
            }
        }
    }

    public void DropEntityLoot(DropLoot dropLoot, Item.ToolTypes itemToolType, Item.ItemTiers itemTier)
    {
        Item[] items = dropLoot.GetLoot(itemToolType, itemTier);

        foreach (Item item in items)
        {
            CreateEntity(DroppedItem.CreateDroppedItem(Game.GetEntityID(), x, y, item, true), true, false, false);
        }
    }

    public void Update()
    {
        switch (entityType)
        {
            case EntityTypes.Player:
                Player.Update(this);
                break;
            case EntityTypes.Dropped_item:
                DroppedItem.Update(this);
                break;
            case EntityTypes.Zombie:
                Zombie.Update(this);
                break;
            case EntityTypes.Cow:
                Cow.Update(this);
                break;
            default:
                break;
        }
    }
}