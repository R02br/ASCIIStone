using static Tile;
using static TileProperty;

public class Terrain
{
    Tile[,]? tiles;
    public List<TileModification> tileModifications = new List<TileModification>();
    public char defaultGround;
    public char defaultEdge;

    public int seed;

    public int terrainWidth;
    public int terrainHeight;

    public void GenerateTerrain(int seed, int terrainWidth, int terrainHeight, char defaultGround, char defaultEdge)
    {
        tileModifications.Clear();

        this.seed = seed;

        this.terrainWidth = terrainWidth;
        this.terrainHeight = terrainHeight;

        this.defaultGround = defaultGround;
        this.defaultEdge = defaultEdge;

        tiles = TerrainGenerator.GenerateTerrain(seed, terrainWidth, terrainHeight);
    }

    public Tile? GetTileAt(float x, float y)
    {
        x = MathF.Floor(x);
        y = MathF.Floor(y);

        if (x < 0) { return null; }
        if (x >= terrainWidth) { return null; }
        if (y < 0) { return null; }
        if (y >= terrainHeight) { return null; }

        if (tiles == null) { return null; }

        return tiles[(int)x, (int)y];
    }

    //sets the tile in the terrain
    private void SetTileAt(int x, int y, Tile tile)
    {
        if (x < 0) return;
        if (x >= terrainWidth) return;
        if (y < 0) return;
        if (y >= terrainHeight) return;

        if (tiles == null) return;

        tile.originalTileType = tiles[x, y].originalTileType;
        tiles[x, y] = tile;

        if (NetworkManager.isHost)
        {
            TileModification tileModification = new TileModification()
            {
                x = x,
                y = y,
                tile = tile,
            };

            for (int i = 0; i < tileModifications.Count; i++)
            {
                if (tileModifications[i].x == x)
                {
                    if (tileModifications[i].y == y)
                    {
                        if (tile.tileType == tile.originalTileType)
                        {
                            tileModifications.RemoveAt(i);
                        }
                        else
                        {
                            tileModifications[i] = tileModification;
                        }
                        return;
                    }
                }
            }

            if (tile.tileType != tile.originalTileType)
            {
                tileModifications.Add(tileModification);
            }
        }
    }

    //represents server or client modifying the tile
    //actually sends data if client
    public void ModifyTileAt(TileModification tileModification, bool send)
    {
        SetTileAt(tileModification.x, tileModification.y, tileModification.tile);

        if (!send) return;

        if (NetworkManager.isHost)
        {
            NetworkManager.SendToAllClients(TileModificationPacket.Build(tileModification), true, null);
        }
        else
        {
            if (NetworkManager.serverConnection == null) return;

            NetworkManager.SendPacket(TileModificationPacket.Build(tileModification), NetworkManager.serverConnection.TcpSocket);
        }
    }

    public void DamageTile(Damage.DamageData damageData, out bool usedDurability)
    {
        usedDurability = false;

        if (tiles == null) return;

        TileProperty tileProperty = TileProperty.GetTileProperty(tiles[damageData.tileX, damageData.tileY]);
        ItemProperty itemProperty = ItemProperty.itemProperties[damageData.itemType];

        if (tileProperty.destructibleType == DestructibleTypes.NONE) return;

        usedDurability = true;

        if (!NetworkManager.isHost) return;

        float damage = 0f;

        if (tileProperty.destructibleType == DestructibleTypes.ENTITY) { damage = itemProperty.entityDamage; }
        if (tileProperty.destructibleType == DestructibleTypes.WOOD) { damage = itemProperty.woodDamage; }
        if (tileProperty.destructibleType == DestructibleTypes.STONE) { damage = itemProperty.stoneDamage; }

        tiles[damageData.tileX, damageData.tileY].health -= damage;
        bool isAppropriateTool = tileProperty.toolType == Item.ToolTypes.none || (tileProperty.toolType == itemProperty.toolType);
        tiles[damageData.tileX, damageData.tileY].usedOnlyAppropriateTool = tiles[damageData.tileX, damageData.tileY].usedOnlyAppropriateTool && isAppropriateTool;
        if (tiles[damageData.tileX, damageData.tileY].lowestTierUsed > itemProperty.itemTier)
        {
            tiles[damageData.tileX, damageData.tileY].lowestTierUsed = itemProperty.itemTier;
        }

        if (tiles[damageData.tileX, damageData.tileY].health <= 0)
        {
            tiles[damageData.tileX, damageData.tileY].health = 0;

            SpawnTileDrop(tiles[damageData.tileX, damageData.tileY].tileType, damageData.tileX, damageData.tileY, tiles[damageData.tileX, damageData.tileY].usedOnlyAppropriateTool ? itemProperty.toolType : Item.ToolTypes.none, tiles[damageData.tileX, damageData.tileY].lowestTierUsed);

            SetTileAt(damageData.tileX, damageData.tileY, TileProperty.tileProperties[TileTypes.AIR].CreateDefaultTile());
        }

        TileModification tileModification = new TileModification()
        {
            x = damageData.tileX,
            y = damageData.tileY,
            tile = tiles[damageData.tileX, damageData.tileY],
        };

        ModifyTileAt(tileModification, true);
    }

    public bool IsLitAt(int x, int y)
    {
        if (x < 0 || x >= terrainWidth) return false;
        if (y < 0 || y >= terrainHeight) return false;

        return true;

        //TODO: add lighting
    }
}