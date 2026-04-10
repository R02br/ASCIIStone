using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;


public static class Packet
{
    public const int maxPacketSize = ushort.MaxValue * 4;


    public static Dictionary<byte, Action<ParseInput>> packetParsers = new Dictionary<byte, Action<ParseInput>>()
    {
        {HandshakePacket.packetId, HandshakePacket.Parse},
        {ServerInfo.packetId, ServerInfo.Parse},
        {TerrainData.packetId, TerrainData.Parse},
        {InitializePlayerData.packetId, InitializePlayerData.Parse},
        {CreateEntityPacket.packetId, CreateEntityPacket.Parse},
        {DestroyEntityPacket.packetId, DestroyEntityPacket.Parse},
        {EntityPosition.packetId, EntityPosition.Parse},
        {EntityHealth.packetId, EntityHealth.Parse},
        {EntityMetaData.packetId, EntityMetaData.Parse},
        {AttackPacket.packetId, AttackPacket.Parse},
        {DropItemPacket.packetId, DropItemPacket.Parse},
        {CheckPickup.packetId, CheckPickup.Parse},
        {ConfirmPickup.packetId, ConfirmPickup.Parse},
        {GiveItem.packetId, GiveItem.Parse},
        {ConsumeItemPacket.packetId, ConsumeItemPacket.Parse},
        {TileModificationPacket.packetId, TileModificationPacket.Parse},
        {BuildingPacket.packetId, BuildingPacket.Parse},
        {TimePacket.packetId, TimePacket.Parse},
    };

    public static void ParsePacket(ParseInput parseInput)
    {
        if (packetParsers.TryGetValue(parseInput.buffer[0], out Action<ParseInput>? actionOutput))
        {
            if (actionOutput != null)
            {
                actionOutput.Invoke(parseInput);
            }
        }
        else
        {
            NetworkManager.RemoveConnection(parseInput.conn);

            Debug.WriteLine($"Unknown message type {parseInput.buffer[0]}");
        }
    }
}

public struct ParseInput
{
    public byte[] buffer;
    public Connection conn;
    public bool isTcp;

    public ParseInput(byte[] buffer, Connection conn, bool isTcp)
    {
        this.buffer = buffer;
        this.conn = conn;
        this.isTcp = isTcp;
    }
}

public static class HandshakePacket
{
    public static byte packetId = 0x00;
    public static int worstPacketSize = 9;

    public static byte[] Build(int port, char playerChar)
    {
        List<byte> bytes = new List<byte>();

        BinarySerializer.WriteByte(ref bytes, packetId);
        BinarySerializer.WriteInt(ref bytes, port);
        BinarySerializer.WriteString(ref bytes, playerChar.ToString());

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        if (!NetworkManager.isHost) return;

        //uses conn just to send tcpSocket not as actuall connection because it doesn't exist yet

        if (input.conn.TcpSocket.RemoteEndPoint == null) return;

        int index = 1;

        IPEndPoint clientUdpEndpoint = new IPEndPoint(((IPEndPoint)input.conn.TcpSocket.RemoteEndPoint).Address, BinarySerializer.ReadInt(input.buffer, ref index));
        Connection conn = new Connection(Game.GetEntityID(), input.conn.TcpSocket, clientUdpEndpoint);

        NetworkManager.AddConnection(conn);
        NetworkManager.HandleConnection(conn);

        if (NetworkManager.UdpSocket == null) return;
        if (NetworkManager.UdpSocket.LocalEndPoint == null) return;

        NetworkManager.SendPacket(ServerInfo.Build(((IPEndPoint)NetworkManager.UdpSocket.LocalEndPoint).Port), conn.TcpSocket);
        NetworkManager.SendPacket(TerrainData.Build(Game.mainTerrain), conn.TcpSocket);

        NetworkManager.SendPacket(TimePacket.Build((float)DayNightCycle.time), conn.TcpSocket);


        foreach (TileModification tileModification in Game.mainTerrain.tileModifications)
        {
            NetworkManager.SendPacket(TileModificationPacket.Build(tileModification), conn.TcpSocket);
        }

        NetworkManager.SendPacket(InitializePlayerData.Build(conn.id), conn.TcpSocket);

        Entity.CreateEntity(Player.CreateDefaultPlayer(conn.id, BinarySerializer.ReadString(input.buffer, ref index)[0]), true, true, true);

        foreach (Entity entity in Game.entities.Values)
        {
            if (entity.id != conn.id)
            {
                NetworkManager.SendPacket(CreateEntityPacket.Build(entity), conn.TcpSocket);
            }
        }
    }
}

public static class ServerInfo
{
    public static byte packetId = 0x01;

    public static int packetSize = 5;

    public static byte[] Build(int port)
    {
        List<byte> bytes = new List<byte>();

        BinarySerializer.WriteByte(ref bytes, packetId);
        BinarySerializer.WriteInt(ref bytes, port);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        if (NetworkManager.isHost) return;

        if (input.conn.TcpSocket.RemoteEndPoint == null) return;

        int index = 1;

        NetworkManager.serverUdpEndpoint = new IPEndPoint((((IPEndPoint)input.conn.TcpSocket.RemoteEndPoint).Address), BinarySerializer.ReadInt(input.buffer, ref index));
        NetworkManager.serverConnection = new Connection(0, input.conn.TcpSocket, NetworkManager.serverUdpEndpoint);

        NetworkManager.HandleConnection(NetworkManager.serverConnection);
    }
}

public static class TerrainData
{
    public static byte packetId = 0x02;

    //no packet size

    public static byte[] Build(Terrain terrain)
    {
        List<byte> bytes = new List<byte>();

        BinarySerializer.WriteByte(ref bytes, packetId);
        BinarySerializer.WriteInt(ref bytes, terrain.seed);
        BinarySerializer.WriteInt(ref bytes, terrain.terrainWidth);
        BinarySerializer.WriteInt(ref bytes, terrain.terrainHeight);
        BinarySerializer.WriteString(ref bytes, terrain.defaultGround.ToString() + terrain.defaultEdge.ToString());

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        if (NetworkManager.isHost) return;

        int index = 1;

        Game.mainTerrain = new Terrain();
        int seed = BinarySerializer.ReadInt(input.buffer, ref index);
        int terrainWidth = BinarySerializer.ReadInt(input.buffer, ref index);
        int terrainHeight = BinarySerializer.ReadInt(input.buffer, ref index);
        string terrainChars = BinarySerializer.ReadString(input.buffer, ref index);

        Game.mainTerrain.GenerateTerrain(seed, terrainWidth, terrainHeight, terrainChars[0], terrainChars[1]);

        Game.gameState = Game.GameStates.Game;
    }
}

public static class InitializePlayerData
{
    public static byte packetId = 0x03;

    public static int packetSize = 5;

    public static byte[] Build(uint entityId)
    {
        List<byte> bytes = new List<byte>();

        BinarySerializer.WriteByte(ref bytes, packetId);
        BinarySerializer.WriteInt(ref bytes, (int)entityId);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        if (NetworkManager.isHost)
        {
            NetworkManager.RemoveConnection(input.conn);
            return;
        }

        int index = 1;

        Player.myPlayerId = (uint)BinarySerializer.ReadInt(input.buffer, ref index);
    }
}

public static class CreateEntityPacket
{
    public static byte packetId = 0x04;

    public static int worstPacketSize = 19;

    public static byte[] Build(Entity entity)
    {
        List<byte> bytes = new List<byte>();

        BinarySerializer.WriteByte(ref bytes, packetId);
        BinarySerializer.WriteInt(ref bytes, (int)entity.id);
        BinarySerializer.WriteByte(ref bytes, (byte)entity.entityType);
        BinarySerializer.WriteFloat(ref bytes, entity.x);
        BinarySerializer.WriteFloat(ref bytes, entity.y);
        BinarySerializer.WriteString(ref bytes, entity.character.ToString());
        BinarySerializer.WriteFloat(ref bytes, entity.health);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        if (!NetworkManager.isHost)
        {
            Entity entity = new Entity((uint)BinarySerializer.ReadInt(input.buffer, ref index), (Entity.EntityTypes)BinarySerializer.ReadByte(input.buffer, ref index), BinarySerializer.ReadFloat(input.buffer, ref index), BinarySerializer.ReadFloat(input.buffer, ref index), BinarySerializer.ReadString(input.buffer, ref index)[0], BinarySerializer.ReadFloat(input.buffer, ref index));

            Entity.CreateEntity(entity, false, false, true);

            if (entity.id == Player.myPlayerId)
            {
                Game.gameState = Game.GameStates.Game;
            }
        }
    }
}

public static class DestroyEntityPacket
{
    public static byte packetId = 0x05;

    public static int packetSize = 5;

    public static byte[] Build(uint id)
    {
        List<byte> bytes = new List<byte>();

        BinarySerializer.WriteByte(ref bytes, packetId);
        BinarySerializer.WriteInt(ref bytes, (int)id);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        uint entityId = (uint)BinarySerializer.ReadInt(input.buffer, ref index);

        Entity.DestroyEntity(entityId, NetworkManager.isHost);
    }
}

public static class EntityPosition
{
    public static byte packetId = 0x06;

    public static int packetSize = 13;

    public static byte[] Build(Entity entity)
    {
        List<byte> bytes = new List<byte>();

        BinarySerializer.WriteByte(ref bytes, packetId);
        BinarySerializer.WriteInt(ref bytes, (int)entity.id);
        BinarySerializer.WriteFloat(ref bytes, entity.x);
        BinarySerializer.WriteFloat(ref bytes, entity.y);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        uint entityId = (uint)BinarySerializer.ReadInt(input.buffer, ref index);

        if (!Game.entities.TryGetValue(entityId, out Entity? entity)) return;

        if (NetworkManager.isHost)
        {
            if (input.conn.id == entityId)
            {
                float x = BinarySerializer.ReadFloat(input.buffer, ref index);
                float y = BinarySerializer.ReadFloat(input.buffer, ref index);

                if (Entity.ValidatePosition(x, y, Game.mainTerrain))
                {
                    entity.x = x;
                    entity.y = y;

                    NetworkManager.SendToAllClients(input.buffer, input.isTcp, input.conn);
                }
                else
                {
                    NetworkManager.SendToAllClients(EntityPosition.Build(entity), input.isTcp, null);
                }
            }
        }
        else
        {
            entity.x = BinarySerializer.ReadFloat(input.buffer, ref index);
            entity.y = BinarySerializer.ReadFloat(input.buffer, ref index);
        }
    }
}

public static class EntityHealth
{
    public static byte packetId = 0x07;

    public static int packetSize = 9;

    public static byte[] Build(Entity entity)
    {
        List<byte> bytes = new List<byte>();

        BinarySerializer.WriteByte(ref bytes, packetId);
        BinarySerializer.WriteInt(ref bytes, (int)entity.id);
        BinarySerializer.WriteFloat(ref bytes, entity.health);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        uint entityId = (uint)BinarySerializer.ReadInt(input.buffer, ref index);

        float health = BinarySerializer.ReadFloat(input.buffer, ref index);

        if (!Game.entities.TryGetValue(entityId, out Entity? entity)) return;

        if (entity.entityType == Entity.EntityTypes.Player)
        {
            if (entityId == Player.myPlayerId)
            {
                if (entity.health > 0 && health <= 0)
                {
                    Player.Die(entity);
                }
            }
        }

        entity.health = health;
    }
}

public static class EntityMetaData
{
    public static byte packetId = 0x09;

    public static byte[] Build(uint entityId, string name, object data, Entity.metaDataTypes typeToSend)
    {
        List<byte> bytes = new List<byte>();

        BinarySerializer.WriteByte(ref bytes, packetId);
        BinarySerializer.WriteInt(ref bytes, (int)entityId);
        BinarySerializer.WriteString(ref bytes, name);
        BinarySerializer.WriteByte(ref bytes, (byte)typeToSend);
        if (typeToSend == Entity.metaDataTypes.INT)
        {
            BinarySerializer.WriteInt(ref bytes, (int)data);
        }
        else if (typeToSend == Entity.metaDataTypes.FLOAT)
        {
            BinarySerializer.WriteFloat(ref bytes, (float)data);
        }
        else if (typeToSend == Entity.metaDataTypes.STRING)
        {
            BinarySerializer.WriteString(ref bytes, (string)data);
        }

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        uint entityId = (uint)BinarySerializer.ReadInt(input.buffer, ref index);

        if (!Game.entities.TryGetValue(entityId, out Entity? entity)) return;

        string metaName = BinarySerializer.ReadString(input.buffer, ref index);

        Entity.metaDataTypes metaDataType = (Entity.metaDataTypes)BinarySerializer.ReadByte(input.buffer, ref index);

        if (metaDataType == Entity.metaDataTypes.INT)
        {
            entity.SetMeta(metaName, BinarySerializer.ReadInt(input.buffer, ref index), NetworkManager.isHost, metaDataType);
        }
        else if (metaDataType == Entity.metaDataTypes.FLOAT)
        {
            entity.SetMeta(metaName, BinarySerializer.ReadFloat(input.buffer, ref index), NetworkManager.isHost, metaDataType);
        }
        else if (metaDataType == Entity.metaDataTypes.STRING)
        {
            entity.SetMeta(metaName, BinarySerializer.ReadString(input.buffer, ref index), NetworkManager.isHost, metaDataType);
        }
    }
}

public static class AttackPacket
{
    public static byte packetId = 0x10;

    public static byte[] Build(Damage.DamageData damageData)
    {
        List<byte> bytes = new List<byte>();

        BinarySerializer.WriteByte(ref bytes, packetId);
        BinarySerializer.WriteByte(ref bytes, (byte)damageData.itemType);
        BinarySerializer.WriteByte(ref bytes, damageData.hasAttacker ? (byte)1 : (byte)0);
        BinarySerializer.WriteInt(ref bytes, (int)damageData.attackerId);
        BinarySerializer.WriteInt(ref bytes, damageData.tileX);
        BinarySerializer.WriteInt(ref bytes, damageData.tileY);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        Damage.DamageData damageData = new Damage.DamageData()
        {
            itemType = (Item.ItemTypes)BinarySerializer.ReadByte(input.buffer, ref index),
            hasAttacker = BinarySerializer.ReadByte(input.buffer, ref index) == 1,
            attackerId = (uint)BinarySerializer.ReadInt(input.buffer, ref index),
            tileX = BinarySerializer.ReadInt(input.buffer, ref index),
            tileY = BinarySerializer.ReadInt(input.buffer, ref index),
        };

        Damage.DamageAt(damageData, Game.mainTerrain, false, out _);
    }
}

public static class DropItemPacket
{
    public static byte packetId = 0x11;

    public static byte[] Build(float x, float y, Item item)
    {
        List<byte> bytes = new List<byte>();

        BinarySerializer.WriteByte(ref bytes, packetId);
        BinarySerializer.WriteFloat(ref bytes, x);
        BinarySerializer.WriteFloat(ref bytes, y);
        BinarySerializer.WriteByte(ref bytes, (byte)item.itemType);
        BinarySerializer.WriteInt(ref bytes, item.count);
        BinarySerializer.WriteInt(ref bytes, item.health);


        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        if (NetworkManager.isHost)
        {
            Inventory.DropItem(BinarySerializer.ReadFloat(input.buffer, ref index), BinarySerializer.ReadFloat(input.buffer, ref index), new Item
            {
                itemType = (Item.ItemTypes)BinarySerializer.ReadByte(input.buffer, ref index),
                count = BinarySerializer.ReadInt(input.buffer, ref index),
                health = BinarySerializer.ReadInt(input.buffer, ref index),
            });
        }
    }
}

public static class CheckPickup
{
    public static byte packetId = 0x12;

    public static byte[] Build(uint id, Item item)
    {
        List<byte> bytes = new List<byte>();

        BinarySerializer.WriteByte(ref bytes, packetId);
        BinarySerializer.WriteInt(ref bytes, (int)id);
        BinarySerializer.WriteByte(ref bytes, (byte)item.itemType);
        BinarySerializer.WriteInt(ref bytes, item.count);
        BinarySerializer.WriteInt(ref bytes, item.health);


        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        uint id = (uint)BinarySerializer.ReadInt(input.buffer, ref index);

        Item item = new Item
        {
            itemType = (Item.ItemTypes)BinarySerializer.ReadByte(input.buffer, ref index),
            count = BinarySerializer.ReadInt(input.buffer, ref index),
            health = BinarySerializer.ReadInt(input.buffer, ref index),
        };

        if (Inventory.CheckItem(item))
        {
            if (NetworkManager.serverConnection != null)
            {
                NetworkManager.SendPacket(ConfirmPickup.Build(id), NetworkManager.serverConnection.UdpEndpoint);
            }
        }
    }
}

public static class ConfirmPickup
{
    public static byte packetId = 0x13;

    public static byte[] Build(uint id)
    {
        List<byte> bytes = new List<byte>();

        BinarySerializer.WriteByte(ref bytes, packetId);
        BinarySerializer.WriteInt(ref bytes, (int)id);


        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        if (!NetworkManager.isHost) return;

        int index = 1;
        uint id = (uint)BinarySerializer.ReadInt(input.buffer, ref index);

        if (Game.entities.TryGetValue(id, out Entity? entity))
        {
            if (entity != null)
            {
                if (entity.entityType == Entity.EntityTypes.Dropped_item)
                {
                    //is not destroyed instantly, so you can get values after destruction
                    Entity.DestroyEntity(entity.id, true);
                    NetworkManager.SendPacket(GiveItem.Build(new Item
                    {
                        itemType = (Item.ItemTypes)entity.GetMetaAsInt("itemType", 0),
                        count = entity.GetMetaAsInt("count", 0),
                        health = entity.GetMetaAsInt("health", 0),
                    }), input.conn.TcpSocket);
                }
            }
        }
    }
}

public static class GiveItem
{
    public static byte packetId = 0x14;

    public static byte[] Build(Item item)
    {
        List<byte> bytes = new List<byte>();

        BinarySerializer.WriteByte(ref bytes, packetId);
        BinarySerializer.WriteByte(ref bytes, (byte)item.itemType);
        BinarySerializer.WriteInt(ref bytes, item.count);
        BinarySerializer.WriteInt(ref bytes, item.health);
        

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        if (NetworkManager.isHost) return;

        int index = 1;
        Item item = new Item
        {
            itemType = (Item.ItemTypes)BinarySerializer.ReadByte(input.buffer, ref index),
            count = BinarySerializer.ReadInt(input.buffer, ref index),
            health = BinarySerializer.ReadInt(input.buffer, ref index),
        };

        Inventory.AddItem(item);
    }
}

public static class ConsumeItemPacket
{
    public static byte packetId = 0x15;

    public static byte[] Build(Item item)
    {
        List<byte> bytes = new List<byte>();

        BinarySerializer.WriteByte(ref bytes, packetId);
        BinarySerializer.WriteByte(ref bytes, (byte)item.itemType);
        BinarySerializer.WriteInt(ref bytes, item.count);
        BinarySerializer.WriteInt(ref bytes, item.health);
        

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        if (!NetworkManager.isHost) return;

        int index = 1;
        Item item = new Item
        {
            itemType = (Item.ItemTypes)BinarySerializer.ReadByte(input.buffer, ref index),
            count = BinarySerializer.ReadInt(input.buffer, ref index),
            health = BinarySerializer.ReadInt(input.buffer, ref index),
        };

        if (Game.entities.TryGetValue(input.conn.id, out Entity? player))
        {
            if (player == null) return;

            Player.ConsumeItem(player, item);
        }
    }
}

public static class TileModificationPacket
{
    public static byte packetId = 0x20;

    public static byte[] Build(TileModification tileModification)
    {
        List<byte> bytes = new List<byte>();

        BinarySerializer.WriteByte(ref bytes, packetId);
        BinarySerializer.WriteInt(ref bytes, tileModification.x);
        BinarySerializer.WriteInt(ref bytes, tileModification.y);
        BinarySerializer.WriteByte(ref bytes, (byte)tileModification.tile.tileType);
        BinarySerializer.WriteByte(ref bytes, (byte)tileModification.tile.originalTileType);
        BinarySerializer.WriteFloat(ref bytes, tileModification.tile.health);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        TileModification tileModification = new TileModification()
        {
            x = BinarySerializer.ReadInt(input.buffer, ref index),
            y = BinarySerializer.ReadInt(input.buffer, ref index),
            tile = new Tile()
            {
                tileType = (TileTypes)BinarySerializer.ReadByte(input.buffer, ref index),
                originalTileType = (TileTypes)BinarySerializer.ReadByte(input.buffer, ref index),
                health = BinarySerializer.ReadFloat(input.buffer, ref index),
            },
        };

        Game.mainTerrain.ModifyTileAt(tileModification, true, NetworkManager.isHost);
    }
}

public static class BuildingPacket
{
    public static byte packetId = 0x21;

    public static byte[] Build(Building.BuildingData buildingData)
    {
        List<byte> bytes = new List<byte>();

        BinarySerializer.WriteByte(ref bytes, packetId);
        BinarySerializer.WriteInt(ref bytes, buildingData.tileX);
        BinarySerializer.WriteInt(ref bytes, buildingData.tileY);
        BinarySerializer.WriteByte(ref bytes, (byte)buildingData.tileType);
        BinarySerializer.WriteByte(ref bytes, (byte)buildingData.itemType);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        Building.BuildingData buildingData = new Building.BuildingData();
        buildingData.tileX = BinarySerializer.ReadInt(input.buffer, ref index);
        buildingData.tileY = BinarySerializer.ReadInt(input.buffer, ref index);
        buildingData.tileType = (TileTypes)BinarySerializer.ReadByte(input.buffer, ref index);
        buildingData.itemType = (Item.ItemTypes)BinarySerializer.ReadByte(input.buffer, ref index);

        if (Building.TryBuildAt(buildingData))
        {

        }
        else
        {
            NetworkManager.SendPacket(GiveItem.Build(ItemProperty.itemProperties[buildingData.itemType].CreateDefaultItem()), input.conn.TcpSocket);
        }

        if (NetworkManager.isHost)
        {
            Tile? tile = Game.mainTerrain.GetTileAt(buildingData.tileX, buildingData.tileY);

            if (tile != null)
            {
                NetworkManager.SendToAllClients(TileModificationPacket.Build(new TileModification()
                {
                    x = buildingData.tileX,
                    y = buildingData.tileY,
                    tile = tile.Value,
                }), true, null);
            }
        }
    }
}

public static class TimePacket
{
    public static byte packetId = 0x30;

    public static byte[] Build(float time)
    {
        List<byte> bytes = new List<byte>();

        BinarySerializer.WriteByte(ref bytes, packetId);
        BinarySerializer.WriteFloat(ref bytes, time);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        if (NetworkManager.isHost) return;

        DayNightCycle.time = BinarySerializer.ReadFloat(input.buffer, ref index);
    }
}