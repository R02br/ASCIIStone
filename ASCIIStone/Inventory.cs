using System.Text;

public static class Inventory
{
    const int inventorySize = 7;
    public static Item[] items = new Item[inventorySize];

    public static bool isInInventory = false;
    public static int selectedSlot;

    public static void ResetInventory()
    {
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = Item.empty;
        }

        isInInventory = false;

        Crafting.ResetCraft();
    }

    public static string GetFormatedInventory()
    {
        StringBuilder stringBuilder = new StringBuilder();

        for (int i = 0; i < items.Length; i++)
        {
            stringBuilder.Append(GetFormatedItemText(i, true));

            if (i <= 0)
            {
                stringBuilder.Append('\n');
            }
        }

        stringBuilder.Append("\n\n\nUp/Down - select item\nRight - equip item\nLeft - drop item\nQ - drop signel item (works outside inventory)");

        return stringBuilder.ToString();
    }

    public static string GetFormatedItemText(int index, bool showSelection)
    {

        StringBuilder s = new StringBuilder("---");

        if (items[index].itemType != Item.ItemTypes.none)
        {
            ItemProperty itemProperty = ItemProperty.GetItemProperty(items[index]);

            s.Clear();
            s.Append(itemProperty.itemName);

            if (itemProperty.maxStack > 1)
            {
                s.Append($" X {items[index].count}");
            }
            else
            {
                s.Append(" {");

                float healthFraction = items[index].health / (float)ItemProperty.GetItemProperty(items[index]).normalHealth;

                float segments = 5f;

                for (int i = 0; i < segments; i++)
                {
                    float indexFraction = i / segments;
                    if (healthFraction >= indexFraction)
                    {
                        s.Append('=');
                    }
                    else
                    {
                        s.Append('-');
                    }
                }

                s.Append('}');
            }
        }

        if (showSelection)
        {
            if (index == selectedSlot)
            {
                s.Append(" <<<");
            }

            s.Append("\n");
        }


        return s.ToString();
    }

    public static void EquipSelected()
    {
        Item temp = items[selectedSlot];
        items[selectedSlot] = items[0];
        items[0] = temp;
    }

    public static void DropSelected(float x, float y, bool dropOne)
    {
        if (items[selectedSlot].itemType == Item.ItemTypes.none) return;

        if (dropOne)
        {
            Item item = new Item()
            {
                itemType = items[selectedSlot].itemType,
                count = 1,
                health = items[selectedSlot].health,
            };
            DropItem(x, y, item);

            if ((--items[selectedSlot].count) <= 0)
            {
                items[selectedSlot] = Item.empty;
            }
        }
        else
        {
            DropItem(x, y, items[selectedSlot]);

            items[selectedSlot] = Item.empty;
        }
    }

    public static void ListUp()
    {
        selectedSlot--;

        if (selectedSlot < 0)
        {
            selectedSlot = items.Length - 1;
        }
    }

    public static void ListDown()
    {
        selectedSlot++;

        if (selectedSlot >= items.Length)
        {
            selectedSlot = 0;
        }
    }

    public static void AddItem(Item item)
    {
        int remaining = item.count;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].itemType == item.itemType)
            {
                ItemProperty itemProperty = ItemProperty.GetItemProperty(item);

                int stackSumm = remaining + items[i].count;

                if (stackSumm <= itemProperty.maxStack)
                {
                    items[i].count += remaining;
                    return;
                }
                else
                {
                    remaining -= (itemProperty.maxStack - items[i].count);
                    items[i].count = itemProperty.maxStack;
                }
            }
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].itemType == Item.ItemTypes.none)
            {
                items[i] = item;
                items[i].count = remaining;
                return;
            }
        }

        if (Game.entities.TryGetValue(Player.myPlayerId, out Entity? player))
        {
            if (player != null)
            {
                DropItem(player.x, player.y, item);
            }
        }
    }

    public static bool CheckItem(Item item)
    {
        int remaining = item.count;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].itemType == Item.ItemTypes.none)
            {
                return true;
            }
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].itemType == item.itemType)
            {
                ItemProperty itemProperty = ItemProperty.GetItemProperty(item);

                int stackSumm = remaining + items[i].count;

                if (stackSumm <= itemProperty.maxStack)
                {
                    return true;
                }
                else
                {
                    remaining -= (itemProperty.maxStack - items[i].count);
                }
            }
        }

        return false;
    }

    public static int CountItem(Item.ItemTypes itemType)
    {
        int count = 0;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].itemType == itemType)
            {
                count += items[i].count;
            }
        }

        return count;
    }

    public static void RemoveItem(Item.ItemTypes itemType, int count)
    {
        for (int i = items.Length - 1; i >= 0; i--)
        {
            if (items[i].itemType == itemType)
            {
                if (count >= items[i].count)
                {
                    count -= items[i].count;
                    items[i] = Item.empty;

                    if (count <= 0)
                    {
                        return;
                    }
                }
                else
                {
                    items[i].count -= count;
                    return;
                }
            }
        }
    }

    public static void DropItem(float x, float y, Item item)
    {
        if (NetworkManager.isHost)
        {
            Entity.CreateEntity(DroppedItem.CreateDroppedItem(Game.GetEntityID(), x, y, item, false), true, true, false);
        }
        else
        {
            if (NetworkManager.serverConnection != null)
            {
                NetworkManager.SendPacket(DropItemPacket.Build(x, y, item), NetworkManager.serverConnection.TcpSocket);
            }
        }
    }

    public static void DropEverything(float x, float y)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].itemType != Item.ItemTypes.none)
            {
                DropItem(x, y, items[i]);
                items[i] = Item.empty;
            }
        }
    }
}