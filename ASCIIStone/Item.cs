using static Item;

public struct Item
{
    public static Item empty = new Item()
    {
        itemType = ItemTypes.none,
        count = 0,
        health = 0,
    };

    public enum ItemTypes
    {
        none,
        ZOMBIE,
        wood,
        stick,
        stone,
        wood_wall,
        wood_sword,
        wood_pickaxe,
        wood_axe,

        stone_sword,
        stone_pickaxe,
        stone_axe,
        meat_raw,
    }

    public enum ItemTiers
    {
        none,
        wood,
        stone,

    }

    public static ItemTiers highestItemTier = ItemTiers.stone;

    public enum ToolTypes
    {
        none,
        sword,
        axe,
        pickaxe,
    }

    public enum ItemUsage
    {
        none,
        malee_weapon,
        tool,
        building,
        consumable
    }

    public ItemTypes itemType;
    public int count;
    public int health;

    public const int NON_DURABLE_HEALTH = -1;
    public const float MIN_ENTITY_DAMAGE = 0.5f;
    public const float MIN_WOOD_DAMAGE = 1f;

    public const float MIN_STONE_DAMAGE = 0f;

    public const float DEFAULT_HUNGER_REGENERATION = 0f;

}

public class ItemProperty
{
    public static Dictionary<ItemTypes, ItemProperty> itemProperties = new Dictionary<ItemTypes, ItemProperty>
    {
        {ItemTypes.none, new ItemProperty(itemType: ItemTypes.none, itemName: "", normalChar: ' ')},
        {ItemTypes.ZOMBIE, new ItemProperty(itemType: ItemTypes.ZOMBIE, itemName: "", normalChar: ' ', entityDamage: Zombie.defaultEntityDamage, woodDamage: Zombie.defaultWoodDamage, stoneDamage: Zombie.defaultStoneDamage)},
        {ItemTypes.wood, new ItemProperty(itemType: ItemTypes.wood, itemName: "Wood", normalChar: 'w', maxStack: 20)},
        {ItemTypes.stone, new ItemProperty(itemType: ItemTypes.stone, itemName: "Stone", normalChar: 's', maxStack: 15, entityDamage: MIN_ENTITY_DAMAGE * 1.1f)},
        {ItemTypes.stick, new ItemProperty(itemType: ItemTypes.stick, itemName: "Stick", normalChar: '/', maxStack: 10)},
        {ItemTypes.wood_wall, new ItemProperty(itemType: ItemTypes.wood_wall, itemName: "Wooden Wall", normalChar: 'x', maxStack: 5, itemUsage: ItemUsage.building, tileType: TileTypes.WOOD_WALL)},
        {ItemTypes.wood_sword, new ItemProperty(itemType: ItemTypes.wood_sword, itemName: "Wooden Sword", itemTier: ItemTiers.wood, toolType: ToolTypes.sword, normalChar: 's', normalHealth: 32, entityDamage: 4f, woodDamage: 2f, itemUsage: ItemUsage.malee_weapon)},
        {ItemTypes.wood_pickaxe, new ItemProperty(itemType: ItemTypes.wood_pickaxe, itemName: "Wooden Pickaxe", itemTier: ItemTiers.wood, toolType: ToolTypes.pickaxe, 'p', normalHealth: 32, entityDamage: MIN_ENTITY_DAMAGE * 2f, stoneDamage: 1f, itemUsage: ItemUsage.tool)},
        {ItemTypes.wood_axe, new ItemProperty(itemType: ItemTypes.wood_axe, itemName: "Wooden Axe", itemTier: ItemTiers.wood, toolType: ToolTypes.axe, normalChar: 'a', normalHealth: 32, entityDamage: 2f, woodDamage: 2f, itemUsage: ItemUsage.tool)},
        {ItemTypes.stone_sword, new ItemProperty(itemType: ItemTypes.stone_sword, itemName: "Stone Sword", itemTier: ItemTiers.stone, toolType: ToolTypes.sword, normalChar: 's', normalHealth: 64, entityDamage: 6f, woodDamage: 2f, itemUsage: ItemUsage.malee_weapon)},
        {ItemTypes.stone_pickaxe, new ItemProperty(itemType: ItemTypes.stone_pickaxe, itemName: "Stone Pickaxe", itemTier: ItemTiers.stone, toolType: ToolTypes.pickaxe, normalChar: 'p', normalHealth: 64, entityDamage: MIN_ENTITY_DAMAGE * 2.5f, woodDamage: MIN_WOOD_DAMAGE * 1.5f, stoneDamage: 2f, itemUsage: ItemUsage.tool)},
        {ItemTypes.stone_axe, new ItemProperty(itemType: ItemTypes.stone_axe, itemName: "Stone Axe", itemTier: ItemTiers.stone, toolType: ToolTypes.axe, normalChar: 'a', normalHealth: 64, entityDamage: 3f, woodDamage: 4f, itemUsage: ItemUsage.tool)},
        {ItemTypes.meat_raw, new ItemProperty(itemType: ItemTypes.meat_raw, itemName: "Raw Meat", normalChar: 'm', maxStack: 10, itemUsage: ItemUsage.consumable, hungerRegeneration: 2f)}
    };

    public static ItemProperty GetItemProperty(Item item)
    {
        return itemProperties[item.itemType];
    }

    public ItemTypes itemType;
    public string itemName;
    public ItemTiers itemTier;
    public ToolTypes toolType;
    public char normalChar;

    public int normalHealth;
    public int maxStack;

    public float entityDamage;
    public float woodDamage;
    public float stoneDamage;

    public ItemUsage itemUsage;
    public TileTypes? tileType;

    public float hungerRegeneration;


    public ItemProperty(ItemTypes itemType, string itemName, ItemTiers itemTier = ItemTiers.none, ToolTypes toolType = ToolTypes.none, char normalChar = ' ', int normalHealth = NON_DURABLE_HEALTH, int maxStack = 1, float entityDamage = MIN_ENTITY_DAMAGE, float woodDamage = MIN_WOOD_DAMAGE, float stoneDamage = MIN_STONE_DAMAGE, ItemUsage itemUsage = ItemUsage.none, TileTypes? tileType = null, float hungerRegeneration = DEFAULT_HUNGER_REGENERATION)
    {
        this.itemType = itemType;
        this.itemName = itemName;
        this.itemTier = itemTier;
        this.toolType = toolType;
        
        this.normalChar = normalChar;
        this.normalHealth = normalHealth;
        this.maxStack = maxStack;

        this.entityDamage = entityDamage;
        this.woodDamage = woodDamage;
        this.stoneDamage = stoneDamage;

        this.itemUsage = itemUsage;
        this.tileType = tileType;
        this.hungerRegeneration = hungerRegeneration;
    }

    public Item CreateDefaultItem(int count = 1)
    {
        Item item = new Item();
        item.itemType = itemType;
        item.count = count;
        item.health = itemProperties[itemType].normalHealth;

        return item;
    }
}