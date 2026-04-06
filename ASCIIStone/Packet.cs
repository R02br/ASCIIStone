using System.Text;
using static NetworkManager;
using static BinarySerializer;
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
            RemoveConnection(parseInput.conn);

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

        WriteByte(ref bytes, packetId);
        WriteInt(ref bytes, port);
        WriteString(ref bytes, playerChar.ToString());

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        if (!isHost) return;

        //uses conn just to send tcpSocket not as actuall connection because it doesn't exist yet

        if (input.conn.TcpSocket.RemoteEndPoint == null) return;

        int index = 1;

        IPEndPoint clientUdpEndpoint = new IPEndPoint(((IPEndPoint)input.conn.TcpSocket.RemoteEndPoint).Address, ReadInt(input.buffer, ref index));
        Connection conn = new Connection(Game.GetEntityID(), input.conn.TcpSocket, clientUdpEndpoint);

        AddConnection(conn);
        HandleConnection(conn);

        if (UdpSocket == null) return;
        if (UdpSocket.LocalEndPoint == null) return;

        SendPacket(ServerInfo.Build(((IPEndPoint)UdpSocket.LocalEndPoint).Port), conn.TcpSocket);
        SendPacket(TerrainData.Build(Game.mainTerrain), conn.TcpSocket);

        SendPacket(TimePacket.Build((float)DayNightCycle.time), conn.TcpSocket);


        foreach (TileModification tileModification in Game.mainTerrain.tileModifications)
        {
            SendPacket(TileModificationPacket.Build(tileModification), conn.TcpSocket);
        }

        SendPacket(InitializePlayerData.Build(conn.id), conn.TcpSocket);

        Entity.CreateEntity(Player.CreateDefaultPlayer(conn.id, ReadString(input.buffer, ref index)[0]), true, true, true);

        foreach (Entity entity in Game.entities.Values)
        {
            if (entity.id != conn.id)
            {
                SendPacket(CreateEntityPacket.Build(entity), conn.TcpSocket);
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

        WriteByte(ref bytes, packetId);
        WriteInt(ref bytes, port);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        if (isHost) return;

        if (input.conn.TcpSocket.RemoteEndPoint == null) return;

        int index = 1;

        serverUdpEndpoint = new IPEndPoint((((IPEndPoint)input.conn.TcpSocket.RemoteEndPoint).Address), ReadInt(input.buffer, ref index));
        serverConnection = new Connection(0, input.conn.TcpSocket, serverUdpEndpoint);

        HandleConnection(serverConnection);
    }
}

public static class TerrainData
{
    public static byte packetId = 0x02;

    //no packet size

    public static byte[] Build(Terrain terrain)
    {
        List<byte> bytes = new List<byte>();

        WriteByte(ref bytes, packetId);
        WriteInt(ref bytes, terrain.seed);
        WriteInt(ref bytes, terrain.terrainWidth);
        WriteInt(ref bytes, terrain.terrainHeight);
        WriteString(ref bytes, terrain.defaultGround.ToString() + terrain.defaultEdge.ToString());

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        if (isHost) return;

        int index = 1;

        Game.mainTerrain = new Terrain();
        int seed = ReadInt(input.buffer, ref index);
        int terrainWidth = ReadInt(input.buffer, ref index);
        int terrainHeight = ReadInt(input.buffer, ref index);
        string terrainChars = ReadString(input.buffer, ref index);

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

        WriteByte(ref bytes, packetId);
        WriteInt(ref bytes, (int)entityId);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        if (isHost)
        {
            RemoveConnection(input.conn);
            return;
        }

        int index = 1;

        Player.myPlayerId = (uint)ReadInt(input.buffer, ref index);
    }
}

public static class CreateEntityPacket
{
    public static byte packetId = 0x04;

    public static int worstPacketSize = 19;

    public static byte[] Build(Entity entity)
    {
        List<byte> bytes = new List<byte>();

        WriteByte(ref bytes, packetId);
        WriteInt(ref bytes, (int)entity.id);
        WriteByte(ref bytes, (byte)entity.entityType);
        WriteFloat(ref bytes, entity.x);
        WriteFloat(ref bytes, entity.y);
        WriteString(ref bytes, entity.character.ToString());
        WriteFloat(ref bytes, entity.health);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        if (!isHost)
        {
            Entity entity = new Entity((uint)ReadInt(input.buffer, ref index), (Entity.EntityTypes)ReadByte(input.buffer, ref index), ReadFloat(input.buffer, ref index), ReadFloat(input.buffer, ref index), ReadString(input.buffer, ref index)[0], ReadFloat(input.buffer, ref index));

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

        WriteByte(ref bytes, packetId);
        WriteInt(ref bytes, (int)id);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        uint entityId = (uint)ReadInt(input.buffer, ref index);

        Entity.DestroyEntity(entityId, isHost);
    }
}

public static class EntityPosition
{
    public static byte packetId = 0x06;

    public static int packetSize = 13;

    public static byte[] Build(Entity entity)
    {
        List<byte> bytes = new List<byte>();

        WriteByte(ref bytes, packetId);
        WriteInt(ref bytes, (int)entity.id);
        WriteFloat(ref bytes, entity.x);
        WriteFloat(ref bytes, entity.y);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        uint entityId = (uint)ReadInt(input.buffer, ref index);

        if (!Game.entities.TryGetValue(entityId, out Entity? entity)) return;

        if (isHost)
        {
            if (input.conn.id == entityId)
            {
                float x = ReadFloat(input.buffer, ref index);
                float y = ReadFloat(input.buffer, ref index);

                if (Entity.ValidatePosition(x, y, Game.mainTerrain))
                {
                    entity.x = x;
                    entity.y = y;

                    SendToAllClients(input.buffer, input.isTcp, input.conn);
                }
                else
                {
                    SendToAllClients(EntityPosition.Build(entity), input.isTcp, null);
                }
            }
        }
        else
        {
            entity.x = ReadFloat(input.buffer, ref index);
            entity.y = ReadFloat(input.buffer, ref index);
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

        WriteByte(ref bytes, packetId);
        WriteInt(ref bytes, (int)entity.id);
        WriteFloat(ref bytes, entity.health);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        uint entityId = (uint)ReadInt(input.buffer, ref index);

        float health = ReadFloat(input.buffer, ref index);

        if (!Game.entities.TryGetValue(entityId, out Entity? entity)) return;

        if (entity.entityType == Entity.EntityTypes.Player)
        {
            if (entityId == Player.myPlayerId)
            {
                if (entity.health > 0 && health <= 0)
                {
                    Inventory.DropEverything(entity.x, entity.y);
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

        WriteByte(ref bytes, packetId);
        WriteInt(ref bytes, (int)entityId);
        WriteString(ref bytes, name);
        WriteByte(ref bytes, (byte)typeToSend);
        if (typeToSend == Entity.metaDataTypes.INT)
        {
            WriteInt(ref bytes, (int)data);
        }
        else if (typeToSend == Entity.metaDataTypes.FLOAT)
        {
            WriteFloat(ref bytes, (float)data);
        }
        else if (typeToSend == Entity.metaDataTypes.STRING)
        {
            WriteString(ref bytes, (string)data);
        }

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        uint entityId = (uint)ReadInt(input.buffer, ref index);

        if (!Game.entities.TryGetValue(entityId, out Entity? entity)) return;

        string metaName = ReadString(input.buffer, ref index);

        Entity.metaDataTypes metaDataType = (Entity.metaDataTypes)ReadByte(input.buffer, ref index);

        if (metaDataType == Entity.metaDataTypes.INT)
        {
            entity.SetMeta(metaName, ReadInt(input.buffer, ref index), isHost, metaDataType);
        }
        else if (metaDataType == Entity.metaDataTypes.FLOAT)
        {
            entity.SetMeta(metaName, ReadFloat(input.buffer, ref index), isHost, metaDataType);
        }
        else if (metaDataType == Entity.metaDataTypes.STRING)
        {
            entity.SetMeta(metaName, ReadString(input.buffer, ref index), isHost, metaDataType);
        }
    }
}

public static class AttackPacket
{
    public static byte packetId = 0x10;

    public static byte[] Build(Damage.DamageData damageData)
    {
        List<byte> bytes = new List<byte>();

        WriteByte(ref bytes, packetId);
        WriteByte(ref bytes, (byte)damageData.itemType);
        WriteByte(ref bytes, damageData.hasAttacker ? (byte)1 : (byte)0);
        WriteInt(ref bytes, (int)damageData.attackerId);
        WriteInt(ref bytes, damageData.tileX);
        WriteInt(ref bytes, damageData.tileY);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        Damage.DamageData damageData = new Damage.DamageData()
        {
            itemType = (Item.ItemTypes)ReadByte(input.buffer, ref index),
            hasAttacker = ReadByte(input.buffer, ref index) == 1,
            attackerId = (uint)ReadInt(input.buffer, ref index),
            tileX = ReadInt(input.buffer, ref index),
            tileY = ReadInt(input.buffer, ref index),
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

        WriteByte(ref bytes, packetId);
        WriteFloat(ref bytes, x);
        WriteFloat(ref bytes, y);
        WriteByte(ref bytes, (byte)item.itemType);
        WriteInt(ref bytes, item.count);
        WriteInt(ref bytes, item.health);


        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        if (isHost)
        {
            Inventory.DropItem(ReadFloat(input.buffer, ref index), ReadFloat(input.buffer, ref index), new Item
            {
                itemType = (Item.ItemTypes)ReadByte(input.buffer, ref index),
                count = ReadInt(input.buffer, ref index),
                health = ReadInt(input.buffer, ref index),
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

        WriteByte(ref bytes, packetId);
        WriteInt(ref bytes, (int)id);
        WriteByte(ref bytes, (byte)item.itemType);
        WriteInt(ref bytes, item.count);
        WriteInt(ref bytes, item.health);


        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        uint id = (uint)ReadInt(input.buffer, ref index);

        Item item = new Item
        {
            itemType = (Item.ItemTypes)ReadByte(input.buffer, ref index),
            count = ReadInt(input.buffer, ref index),
            health = ReadInt(input.buffer, ref index),
        };

        if (Inventory.CheckItem(item))
        {
            if (serverConnection != null)
            {
                SendPacket(ConfirmPickup.Build(id), serverConnection.UdpEndpoint);
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

        WriteByte(ref bytes, packetId);
        WriteInt(ref bytes, (int)id);


        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        if (!isHost) return;

        int index = 1;
        uint id = (uint)ReadInt(input.buffer, ref index);

        if (Game.entities.TryGetValue(id, out Entity? entity))
        {
            if (entity != null)
            {
                if (entity.entityType == Entity.EntityTypes.Dropped_item)
                {
                    //is not destroyed instantly, so you can get values after destruction
                    Entity.DestroyEntity(entity.id, true);
                    SendPacket(GiveItem.Build(new Item
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

        WriteByte(ref bytes, packetId);
        WriteByte(ref bytes, (byte)item.itemType);
        WriteInt(ref bytes, item.count);
        WriteInt(ref bytes, item.health);
        

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        if (isHost) return;

        int index = 1;
        Item item = new Item
        {
            itemType = (Item.ItemTypes)ReadByte(input.buffer, ref index),
            count = ReadInt(input.buffer, ref index),
            health = ReadInt(input.buffer, ref index),
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

        WriteByte(ref bytes, packetId);
        WriteByte(ref bytes, (byte)item.itemType);
        WriteInt(ref bytes, item.count);
        WriteInt(ref bytes, item.health);
        

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        if (!isHost) return;

        int index = 1;
        Item item = new Item
        {
            itemType = (Item.ItemTypes)ReadByte(input.buffer, ref index),
            count = ReadInt(input.buffer, ref index),
            health = ReadInt(input.buffer, ref index),
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

        WriteByte(ref bytes, packetId);
        WriteInt(ref bytes, tileModification.x);
        WriteInt(ref bytes, tileModification.y);
        WriteByte(ref bytes, (byte)tileModification.tile.tileType);
        WriteByte(ref bytes, (byte)tileModification.tile.originalTileType);
        WriteFloat(ref bytes, tileModification.tile.health);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        TileModification tileModification = new TileModification()
        {
            x = ReadInt(input.buffer, ref index),
            y = ReadInt(input.buffer, ref index),
            tile = new Tile()
            {
                tileType = (TileTypes)ReadByte(input.buffer, ref index),
                originalTileType = (TileTypes)ReadByte(input.buffer, ref index),
                health = ReadFloat(input.buffer, ref index),
            },
        };

        Game.mainTerrain.ModifyTileAt(tileModification, isHost);
    }
}

public static class BuildingPacket
{
    public static byte packetId = 0x21;

    public static byte[] Build(Building.BuildingData buildingData)
    {
        List<byte> bytes = new List<byte>();

        WriteByte(ref bytes, packetId);
        WriteInt(ref bytes, buildingData.tileX);
        WriteInt(ref bytes, buildingData.tileY);
        WriteByte(ref bytes, (byte)buildingData.tileType);
        WriteByte(ref bytes, (byte)buildingData.itemType);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        Building.BuildingData buildingData = new Building.BuildingData();
        buildingData.tileX = ReadInt(input.buffer, ref index);
        buildingData.tileY = ReadInt(input.buffer, ref index);
        buildingData.tileType = (TileTypes)ReadByte(input.buffer, ref index);
        buildingData.itemType = (Item.ItemTypes)ReadByte(input.buffer, ref index);

        if (Building.TryBuildAt(buildingData))
        {

        }
        else
        {
            SendPacket(GiveItem.Build(ItemProperty.itemProperties[buildingData.itemType].CreateDefaultItem()), input.conn.TcpSocket);
        }

        if (isHost)
        {
            Tile? tile = Game.mainTerrain.GetTileAt(buildingData.tileX, buildingData.tileY);

            if (tile != null)
            {
                SendToAllClients(TileModificationPacket.Build(new TileModification()
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

        WriteByte(ref bytes, packetId);
        WriteFloat(ref bytes, time);

        return bytes.ToArray();
    }

    public static void Parse(ParseInput input)
    {
        int index = 1;

        if (isHost) return;

        DayNightCycle.time = ReadFloat(input.buffer, ref index);
    }
}