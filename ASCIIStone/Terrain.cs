using static Tile;
using static TileProperty;

public class Terrain
{
    Tile[,]? tiles;
    public List<TileModification> tileModifications = new List<TileModification>();
    public Queue<TileLightUpdate> lightUpdates = new Queue<TileLightUpdate>();
    public HashSet<TileLightUpdate> isInLightUpdates = new HashSet<TileLightUpdate>();
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
        UpdateLighting(true);
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
    //actually sends data
    public void ModifyTileAt(TileModification tileModification, bool updateLight, bool send)
    {
        SetTileAt(tileModification.x, tileModification.y, tileModification.tile);

        if (updateLight)
        {
            lightUpdates.Enqueue(new TileLightUpdate(tileModification.x, tileModification.y));
            lightUpdates.Enqueue(new TileLightUpdate(tileModification.x, tileModification.y + 1));
            lightUpdates.Enqueue(new TileLightUpdate(tileModification.x + 1, tileModification.y));
            lightUpdates.Enqueue(new TileLightUpdate(tileModification.x, tileModification.y - 1));
            lightUpdates.Enqueue(new TileLightUpdate(tileModification.x - 1, tileModification.y));
        }

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

        ModifyTileAt(tileModification, true, true);
    }

    public bool IsLitAt(int x, int y)
    {
        if (x < 0 || x >= terrainWidth) return false;
        if (y < 0 || y >= terrainHeight) return false;


        Tile? tile = GetTileAt(x, y);

        if (tile == null) return false;

        return tile.Value.lightLevel > 0;
    }

    public void Update()
    {
        UpdateLighting(false);
    }

    private void UpdateLighting(bool forceUpdateTerrain)
    {
        if (forceUpdateTerrain)
        {
            for (int x = 0; x < terrainWidth; x++)
            {
                for (int y = 0; y < terrainHeight; y++)
                {
                    UpdateLightAt(x, y);
                }
            }
        }

        TileLightUpdate tileLightUpdate;

        while (lightUpdates.TryDequeue(out tileLightUpdate))
        {
            isInLightUpdates.Remove(tileLightUpdate);
            UpdateLightAt(tileLightUpdate.x, tileLightUpdate.y);
        }
    }

    private void UpdateLightAt(int x, int y)
    {
        Tile? tile = GetTileAt(x, y);

        if (tile == null) return;

        TileProperty tileProperty = GetTileProperty(tile.Value);

        int light = 0;
        int startLight = tile.Value.lightLevel;

        if (tileProperty.lightEmission > 0)
        {
            SetLightAt(x, y, tileProperty.lightEmission);
            startLight = tileProperty.lightEmission;
        }

        int lightUp = TryGetLightFromTile(x, y + 1, ref light);
        int lightRight = TryGetLightFromTile(x + 1, y, ref light);
        int lightDown = TryGetLightFromTile(x, y - 1, ref light);
        int lightLeft = TryGetLightFromTile(x - 1, y, ref light);

        if (light > startLight)
        {
            light = Math.Max(light - 1, 0);
            SetLightAt(x, y, light);
            light--;
            if (light > 0)
            {
                if (tileProperty.blocksLight) return;

                if (light > lightUp) AddLightToUpdate(x, y + 1);
                if (light > lightRight) AddLightToUpdate(x + 1, y);
                if (light > lightDown) AddLightToUpdate(x, y - 1);
                if (light > lightLeft) AddLightToUpdate(x - 1, y);
            }
        }
        else
        {
            if (tileProperty.lightEmission > 0)
            {
                TryPropagateLightTo(x, y + 1, startLight);
                TryPropagateLightTo(x + 1, y, startLight);
                TryPropagateLightTo(x, y - 1, startLight);
                TryPropagateLightTo(x - 1, y, startLight);
            }
            else
            {
                light = Math.Max(light - 1, 0);
                SetLightAt(x, y, light);

                if (lightUp > 0) AddLightToUpdate(x, y + 1);
                if (lightRight > 0) AddLightToUpdate(x + 1, y);
                if (lightDown > 0) AddLightToUpdate(x, y - 1);
                if (lightLeft > 0) AddLightToUpdate(x - 1, y);
            }
        }
    }


    //returns light at tile but overwrites currentMaxLight only if tile isn't blocking it (light propagates to solid tiles but not from solid tiles)
    private int TryGetLightFromTile(int x, int y, ref int currentMaxLight)
    {
        Tile? tile = GetTileAt(x, y);

        if (tile == null) return 0;

        TileProperty tileProperty = GetTileProperty(tile.Value);

        if (!tileProperty.blocksLight)
        {
            currentMaxLight = Math.Max(currentMaxLight, tile.Value.lightLevel);
        }

        return tile.Value.lightLevel;
    }

    private void TryPropagateLightTo(int x, int y, int lightLevel)
    {
        if (x < 0 || x >= terrainWidth || y < 0 || y >= terrainHeight) return;

        Tile? tile = GetTileAt(x, y);

        if (tile == null) return;

        TileProperty tileProperty = GetTileProperty(tile.Value);

        if (lightLevel > tileProperty.lightEmission)
        {
            AddLightToUpdate(x, y);
        }
    }

    private void SetLightAt(int x, int y, int lightLevel)
    {
        if (x < 0 || x >= terrainWidth || y < 0 || y >= terrainHeight) return;
        if (tiles == null) return;

        tiles[x, y].lightLevel = (byte)lightLevel;
    }

    private void AddLightToUpdate(int x, int y)
    {
        if (x < 0 || x >= terrainWidth || y < 0 || y >= terrainHeight) return;

        TileLightUpdate tileLightUpdate = new TileLightUpdate(x, y);

        if (isInLightUpdates.Contains(tileLightUpdate)) return;

        lightUpdates.Enqueue(tileLightUpdate);
        isInLightUpdates.Add(tileLightUpdate);
    }
}