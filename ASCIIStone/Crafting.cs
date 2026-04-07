using System.Text;
using static Item;
using static ItemProperty;

public static class Crafting
{
    public static bool isInCrafting = false;

    public static int selectedCraft = 0;

    public static void ResetCraft()
    {
        isInCrafting = false;
        selectedCraft = 0;
    }

    public static CraftingRecipe[] craftingRecipes = new CraftingRecipe[]
    {
        new CraftingRecipe(new Ingredient(ItemTypes.stick, 2), new Ingredient(ItemTypes.wood, 1)),

        new CraftingRecipe(new Ingredient(ItemTypes.wood_wall, 1), new Ingredient(ItemTypes.wood, 2)),

        new CraftingRecipe(new Ingredient(ItemTypes.wood_sword, 1), new Ingredient(ItemTypes.wood, 10), new Ingredient(ItemTypes.stick, 1)),
        new CraftingRecipe(new Ingredient(ItemTypes.wood_pickaxe, 1), new Ingredient(ItemTypes.wood, 10), new Ingredient(ItemTypes.stick, 1)),
        new CraftingRecipe(new Ingredient(ItemTypes.wood_axe, 1), new Ingredient(ItemTypes.wood, 10), new Ingredient(ItemTypes.stick, 1)),

        new CraftingRecipe(new Ingredient(ItemTypes.stone_sword, 1), new Ingredient(ItemTypes.stone, 10), new Ingredient(ItemTypes.stick, 1)),
        new CraftingRecipe(new Ingredient(ItemTypes.stone_pickaxe, 1), new Ingredient(ItemTypes.stone, 10), new Ingredient(ItemTypes.stick, 1)),
        new CraftingRecipe(new Ingredient(ItemTypes.stone_axe, 1), new Ingredient(ItemTypes.stone, 10), new Ingredient(ItemTypes.stick, 1)),

        new CraftingRecipe(new Ingredient(ItemTypes.torch, 1), new Ingredient(ItemTypes.stick, 1), new Ingredient(ItemTypes.coal, 2)),
    };

    public static void ListLeft()
    {
        selectedCraft--;

        if (selectedCraft < 0)
        {
            selectedCraft = (craftingRecipes.Length - 1);
        }
    }

    public static void ListRight()
    {
        selectedCraft++;

        if (selectedCraft >= craftingRecipes.Length)
        {
            selectedCraft = 0;
        }
    }

    public static void CraftSelected()
    {
        if (!DebugMenu.infiniteCrafting)
        {
            foreach (Ingredient ingredient in craftingRecipes[selectedCraft].ingredients)
            {
                if (Inventory.CountItem(ingredient.itemType) < ingredient.count)
                {
                    return;
                }
            }

            foreach (Ingredient ingredient in craftingRecipes[selectedCraft].ingredients)
            {
                Inventory.RemoveItem(ingredient.itemType, ingredient.count);
            }
        }

        Ingredient result = craftingRecipes[selectedCraft].result;

        ItemProperty itemProperty = itemProperties[result.itemType];

        Item item = itemProperty.CreateDefaultItem(result.count);

        Inventory.AddItem(item);
    }

    public static string GetFormatedRecipeText()
    {
        StringBuilder output = new StringBuilder("Left/Right - select recipe\nDown - craft item\n\n");

        StringBuilder space = new StringBuilder("\n");

        CraftingRecipe craftingRecipe = craftingRecipes[selectedCraft];

        ItemProperty itemProperty = itemProperties[craftingRecipe.result.itemType];

        string itemToCraft = $"{itemProperty.itemName} {craftingRecipe.result.count}X (You have {Inventory.CountItem(craftingRecipe.result.itemType)}) <- ";

        for (int j = 0; j < itemToCraft.Length; j++)
        {
            space.Append(' ');
        }

        output.Append(itemToCraft);

        int i = 0;

        foreach (Ingredient ingredient in craftingRecipe.ingredients)
        {
            itemProperty = itemProperties[ingredient.itemType];

            if (i != 0)
            {
                output.Append(space);
            }

            output.Append($"{itemProperty.itemName} {ingredient.count}X (You have {Inventory.CountItem(ingredient.itemType)})");

            i++;
        }

        return output.ToString();
    }
}