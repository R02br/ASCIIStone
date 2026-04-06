    public enum TileTypes
    {
        AIR,
        TREE,
        ROCK,
        WOOD_WALL,
        WATER,
    }

    public struct Tile
    {
        public TileTypes tileType;
        public TileTypes originalTileType; //used for telling what was there originally
        public float health;
        public bool usedOnlyAppropriateTool;
        public Item.ItemTiers lowestTierUsed;
        public byte lightLevel;
    }

public struct TileModification
{
    public int x;
    public int y;
    public Tile tile;
}

public struct TileLightUpdate
{
    public int x;
    public int y;

    public TileLightUpdate(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

public class TileProperty
{
    public enum DestructibleTypes
    {
        NONE,
        ENTITY,
        WOOD,
        STONE,

    }

    public static Dictionary<TileTypes, TileProperty> tileProperties = new Dictionary<TileTypes, TileProperty>
    {
        {TileTypes.AIR, new TileProperty(TileTypes.AIR, false, 0f, 0, 0, false, true, null, -1, DestructibleTypes.NONE, Item.ToolTypes.none, Item.ItemTiers.none, new DropLoot(new DropChance[] { }))},
        {TileTypes.WATER, new TileProperty(TileTypes.WATER, false, 2f, 0, 0, false, true, '~', -1, DestructibleTypes.NONE, Item.ToolTypes.none, Item.ItemTiers.none, new DropLoot(new DropChance[] { }))},
        {TileTypes.WOOD_WALL, new TileProperty(TileTypes.WOOD_WALL, true, 15f, byte.MaxValue / 2, 15, false, false, '#', 3, DestructibleTypes.WOOD, Item.ToolTypes.none, Item.ItemTiers.none, new DropLoot(new DropChance[]
        {
            new DropChance(Item.ItemTypes.wood, 1, 1, 0.5, Item.ToolTypes.axe, Item.ItemTiers.wood)
        }))},
        {TileTypes.TREE, new TileProperty(TileTypes.TREE, true, 10f, byte.MaxValue / 4, 0, true, false, 'T', 5, DestructibleTypes.WOOD, Item.ToolTypes.none, Item.ItemTiers.none, new DropLoot(new DropChance[]
        {
            new DropChance(Item.ItemTypes.wood, 1, 3, 1.0, Item.ToolTypes.none, Item.ItemTiers.none)
        }))},
        {TileTypes.ROCK, new TileProperty(TileTypes.ROCK, true, 30f, byte.MaxValue, 0, true, false, '&', 10, DestructibleTypes.STONE, Item.ToolTypes.pickaxe, Item.ItemTiers.wood, new DropLoot(new DropChance[]
        {
            new DropChance(Item.ItemTypes.stone, 1, 5, 1.0, Item.ToolTypes.pickaxe, Item.ItemTiers.wood),
        }))}
    };

    public static TileProperty GetTileProperty(Tile tile)
    {
        return tileProperties[tile.tileType];
    }

    public TileTypes tileType;
    public bool hasCollision;
    public float pathCost;
    public byte occulisionStrength;
    public byte lightEmission;
    public bool blocksLight;
    public bool isBuildableOn;
    public char? normalChar;

    public int normalHealth;
    public DestructibleTypes destructibleType;
    public Item.ToolTypes toolType;
    public Item.ItemTiers minTier;
    public DropLoot dropLoot;

    public TileProperty(TileTypes tileType, bool hasCollision, float pathCost, byte occulisionStrength, byte lightEmission, bool blocksLight, bool isBuildableOn, char? normalChar, int normalHealth, DestructibleTypes destructibleType, Item.ToolTypes toolType, Item.ItemTiers minTier, DropLoot dropLoot)
    {
        this.tileType = tileType;
        this.hasCollision = hasCollision;
        this.pathCost = pathCost;
        this.occulisionStrength = occulisionStrength;
        this.lightEmission = lightEmission;
        this.blocksLight = blocksLight;
        this.isBuildableOn = isBuildableOn;
        this.normalChar = normalChar;
        this.normalHealth = normalHealth;
        this.destructibleType = destructibleType;
        this.toolType = toolType;
        this.minTier = minTier;

        this.dropLoot = dropLoot;
    }

    public Tile CreateDefaultTile()
    {
        Tile tile = new Tile();
        tile.tileType = tileType;
        tile.originalTileType = tileType;
        tile.health = tileProperties[tileType].normalHealth;
        tile.usedOnlyAppropriateTool = true;
        tile.lowestTierUsed = Item.highestItemTier;

        return tile;
    }

    public static void SpawnTileDrop(TileTypes tileType, int x, int y, Item.ToolTypes toolType, Item.ItemTiers itemTier)
    {
        Item[] items = tileProperties[tileType].dropLoot.GetLoot(toolType, itemTier);

        foreach (Item item in items)
        {
            Entity.CreateEntity(DroppedItem.CreateDroppedItem(Game.GetEntityID(), x, y, item, true), true, false, false);
        }
    }
}