public class DropLoot
{
    public DropLoot(DropChance[] drops)
    {
        this.drops = drops;
    }

    DropChance[] drops;

    public Item[] GetLoot(Item.ToolTypes usedToolType = Item.ToolTypes.none, Item.ItemTiers usedItemTier = Item.ItemTiers.none)
    {
        List<Item> items = new List<Item>();

        for (int i = 0; i < drops.Length; i++)
        {
            if (drops[i].toolType == Item.ToolTypes.none || drops[i].toolType == usedToolType)
            {
                if (drops[i].minTier <= usedItemTier)
                {
                    if (Random.Shared.NextDouble() < drops[i].chance)
                    {
                        int amount = Random.Shared.Next(drops[i].minAmount, drops[i].maxAmount + 1);
                        Item dropItem = new Item()
                        {
                            itemType = drops[i].item,
                            count = amount,
                        };

                        items.Add(dropItem);
                    }
                }
            }
        }

        return items.ToArray();
    }
}

public struct DropChance
{
    public Item.ItemTypes item;
    public int minAmount;
    public int maxAmount;
    public double chance;
    public Item.ToolTypes toolType;
    public Item.ItemTiers minTier;

    public DropChance(Item.ItemTypes item, int minAmount, int maxAmount, double chance, Item.ToolTypes toolType, Item.ItemTiers minTier)
    {
        this.item = item;
        this.minAmount = minAmount;
        this.maxAmount = maxAmount;
        this.chance = chance;
        this.toolType = toolType;
        this.minTier = minTier;
    }
}