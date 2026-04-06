public class CraftingRecipe
{
    public Ingredient result;
    public Ingredient[] ingredients;

    public CraftingRecipe(Ingredient result, params Ingredient[] ingredients)
    {
        this.result = result;
        this.ingredients = ingredients;
    }
}

public struct Ingredient
{
    public Item.ItemTypes itemType;
    public int count;

    public Ingredient(Item.ItemTypes itemType, int count)
    {
        this.itemType = itemType;
        this.count = count;
    }
}