using System.Text;

public static class Player
{
    public static uint myPlayerId;
    public static char myPlayerChar = '@';

    public const float basePlayerSpeed = 4f;

    public const float sendRate = 1 / 20f;

    public const float maxHealth = 10f;
    public const float maxHunger = 10f;
    public const float regenerationSpeedPerSecond = 0.1f;

    public const float defaultHungerDrainPerSecond = -0.01f;
    public const float defaultStarvingHealthDrainPerSecond = -0.1f;

    public const float attackCooldown = 0.3f;

    public static Entity CreateDefaultPlayer(uint id, char character)
    {
        Entity player = new Entity(id, Entity.EntityTypes.Player, 0f, 0f, character, maxHealth);
        Entity.SpawnEntityInTerrain(player, Game.mainTerrain);

        player.SetMeta("timeBeforeSend", 0f, false, Entity.metaDataTypes.FLOAT);
        player.SetMeta("hunger", maxHunger, false, Entity.metaDataTypes.FLOAT);
        player.SetMeta("attackCooldown", attackCooldown, false, Entity.metaDataTypes.FLOAT);

        if (id == myPlayerId)
        {
            Inventory.ResetInventory();
        }

        return player;
    }

    public static void Update(Entity entity)
    {
        if (entity.id == myPlayerId)
        {
            UpdateInventory(entity);

            UpdateMovement(entity);
        }

        if (NetworkManager.isHost)
        {
            ProcessHunger(entity);

            ProcessHealth(entity);
        }

        SendData(entity);
    }

    public static void UpdateInventory(Entity entity)
    {
        if (entity.health <= 0)
        {
            Inventory.isInInventory = false;
            Inventory.selectedSlot = 0;
            Crafting.isInCrafting = false;
            Crafting.selectedCraft = 0;
        }
        else
        {
            if (DebugMenu.isInDebugMenu) return;

            if (Input.GetKeyDown(ConsoleKey.E))
            {
                Inventory.isInInventory = !Inventory.isInInventory;
                Crafting.isInCrafting = Crafting.isInCrafting && !Inventory.isInInventory;

                Inventory.selectedSlot = 0;
            }

            if (Inventory.isInInventory)
            {
                if (Input.GetKeyDown(ConsoleKey.UpArrow))
                {
                    Inventory.ListUp();
                }
                if (Input.GetKeyDown(ConsoleKey.DownArrow))
                {
                    Inventory.ListDown();
                }

                if (Input.GetKeyDown(ConsoleKey.RightArrow))
                {
                    Inventory.EquipSelected();
                }

                if (Input.GetKeyDown(ConsoleKey.LeftArrow))
                {
                    Inventory.DropSelected(entity.x, entity.y, false);
                }
            }

            if (Input.GetKeyDown(ConsoleKey.Q))
            {
                Inventory.DropSelected(entity.x, entity.y, true);
            }

            if (Input.GetKeyDown(ConsoleKey.C))
            {
                Crafting.isInCrafting = !Crafting.isInCrafting;
                Inventory.isInInventory = Inventory.isInInventory && !Crafting.isInCrafting;

                Crafting.selectedCraft = 0;
            }

            if (Crafting.isInCrafting)
            {
                if (Input.GetKeyDown(ConsoleKey.RightArrow))
                {
                    Crafting.ListRight();
                }

                if (Input.GetKeyDown(ConsoleKey.LeftArrow))
                {
                    Crafting.ListLeft();
                }

                if (Input.GetKeyDown(ConsoleKey.DownArrow))
                {
                    Crafting.CraftSelected();
                }
            }
        }
    }

    public static void UpdateMovement(Entity entity)
    {
        if (DebugMenu.isInDebugMenu) return;

        float inputX = 0;
        float inputY = 0;

        int tileX = (int)MathF.Floor(entity.x);
        int tileY = (int)MathF.Floor(entity.y);
        bool damage = false;

        if (Input.GetKey(ConsoleKey.W))
        {
            inputY++;
        }
        if (Input.GetKey(ConsoleKey.S))
        {
            inputY--;
        }

        if (Input.GetKey(ConsoleKey.D))
        {
            inputX++;
        }
        if (Input.GetKey(ConsoleKey.A))
        {
            inputX--;
        }

        if (Input.GetKey(ConsoleKey.UpArrow))
        {
            tileY++;
            damage = true;
        }
        if (Input.GetKey(ConsoleKey.DownArrow))
        {
            tileY--;
            damage = true;
        }

        if (Input.GetKey(ConsoleKey.RightArrow))
        {
            tileX++;
            damage = true;
        }
        if (Input.GetKey(ConsoleKey.LeftArrow))
        {
            tileX--;
            damage = true;
        }

        if (entity.health <= 0f || Inventory.isInInventory || Crafting.isInCrafting)
        {
            inputX = 0f;
            inputY = 0f;
            damage = false;
        }

        float cooldown = entity.GetMetaAsFloat("attackCooldown", attackCooldown);

        if (cooldown > 0)
        {
            entity.SetMeta("attackCooldown", cooldown - Game.deltaTime, false, Entity.metaDataTypes.FLOAT);
        }

        if (damage)
        {
            if (cooldown <= 0)
            {
                entity.SetMeta("attackCooldown", attackCooldown, false, Entity.metaDataTypes.FLOAT);
                damage = false;

                ItemProperty itemProperty = ItemProperty.GetItemProperty(Inventory.items[0]);

                if (itemProperty.itemUsage == Item.ItemUsage.building)
                {
                    BuildWithItem(entity, tileX, tileY, itemProperty);
                }
                else if (itemProperty.itemUsage == Item.ItemUsage.consumable)
                {
                    ConsumeItem(entity, Inventory.items[0]);
                }
                else
                {
                    AttackWithItem(entity, tileX, tileY, itemProperty);
                }
            }
            else
            {
                entity.SetMeta("attackCooldown", attackCooldown, false, Entity.metaDataTypes.FLOAT);
            }
        }

        float length = MathF.Sqrt((inputX * inputX) + (inputY * inputY));

        if (length > 0)
        {
            inputX /= length;
            inputY /= length;
        }

        float speed = basePlayerSpeed;
        speed *= Game.deltaTime;

        Entity.MoveEntityWithCollisionsInTerrain(entity, Game.mainTerrain, inputX * speed, inputY * speed);
    }

    public static void ProcessHealth(Entity entity)
    {
        if (entity.health <= 0)
        {
            if (Input.GetKeyDown(ConsoleKey.Spacebar))
            {
                Entity newPlayer = CreateDefaultPlayer(entity.id, entity.character);

                entity.x = newPlayer.x;
                entity.y = newPlayer.y;
                entity.health = newPlayer.health;
                entity.SetMeta("hunger", maxHunger, true, Entity.metaDataTypes.FLOAT);
            }
        }
        else
        {
            if (entity.health <= 0f)
            {
                return;
            }

            float hunger = entity.GetMetaAsFloat("hunger", 0f);

            float percentage = hunger / maxHunger;

            entity.health += regenerationSpeedPerSecond * percentage * Game.deltaTime;

            if (entity.health > maxHealth)
            {
                entity.health = maxHealth;
            }

            if (hunger <= 0f)
            {
                entity.health -= defaultStarvingHealthDrainPerSecond;
            }

            //player just died of starvation
            if (entity.health <= 0f)
            {
                Die(entity);
            }
        }
    }

    public static void Die(Entity entity, Damage.DamageData? damageData = null)
    {
        entity.health = 0f;

        if (entity.id == myPlayerId)
        {
            Inventory.DropEverything(entity.x, entity.y);
        }
    }

    public static void ProcessHunger(Entity entity)
    {
        AddHunger(entity, defaultHungerDrainPerSecond * Game.deltaTime);
    }
    
    public static void AddHunger(Entity entity, float hungerToAdd)
    {
        float hunger = entity.GetMetaAsFloat("hunger", 0f);

        hunger += hungerToAdd;

        if (hunger < 0) hunger = 0;
        if (hunger > maxHunger) hunger = maxHunger;

        entity.SetMeta("hunger", hunger, true, Entity.metaDataTypes.FLOAT);
    }

    public static void SendData(Entity entity)
    {
        if (NetworkManager.isHost)
        {
            if (entity.id == Player.myPlayerId)
            {
                entity.SendData(sendRate, false, true, true);
            }
            else
            {
                entity.SendData(sendRate, false, false, true);
            }
        }
        else
        {
            entity.SendData(sendRate, false, true, false);
        }
    }

    public static string GetAttackFormatedString(Entity entity)
    {
        StringBuilder s = new StringBuilder();

        float cooldown = entity.GetMetaAsFloat("attackCooldown", attackCooldown);

        float percentage = cooldown / attackCooldown;

        for (float i = 0; i < 1; i += 0.2f)
        {
            if (percentage >= i)
            {
                s.Append('>');
            }
        }

        return s.ToString();
    }

    public static void AttackWithItem(Entity entity, int tileX, int tileY, ItemProperty itemProperty)
    {
        Damage.DamageData damageData = new Damage.DamageData()
        {
            itemType = Inventory.items[0].itemType,
            hasAttacker = true,
            attackerId = entity.id,
            tileX = tileX,
            tileY = tileY,
        };

        Damage.DamageAt(damageData, Game.mainTerrain, true, out Damage.DamageOut damageOut);

        bool usedAppropriateTool = false;

        if (damageOut.damagedObject == Damage.DamagedObject.entity && itemProperty.itemUsage == Item.ItemUsage.malee_weapon) usedAppropriateTool = true;
        else if (damageOut.damagedObject == Damage.DamagedObject.tile && itemProperty.itemUsage == Item.ItemUsage.tool) usedAppropriateTool = true;


        if ((itemProperty.normalHealth != Item.NON_DURABLE_HEALTH) && damageOut.damagedObject != Damage.DamagedObject.none)
        {
            Inventory.items[0].health -= usedAppropriateTool ? 1 : 2;

            if (Inventory.items[0].health <= 0)
            {
                Inventory.items[0] = Item.empty;
            }
        }
    }

    public static void BuildWithItem(Entity entity, int tileX, int tileY, ItemProperty itemProperty)
    {
        if (itemProperty.tileType == null) return;

        Building.BuildingData buildingData = new Building.BuildingData()
        {
            tileX = tileX,
            tileY = tileY,
            itemType = itemProperty.itemType,
            tileType = itemProperty.tileType.Value,
        };

        if (Building.TryBuildAt(buildingData))
        {
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
            else
            {
                Tile? tile = Game.mainTerrain.GetTileAt(buildingData.tileX, buildingData.tileY);

                if (tile != null)
                {
                    if (NetworkManager.serverConnection != null)
                    {
                        NetworkManager.SendPacket(BuildingPacket.Build(buildingData), NetworkManager.serverConnection.TcpSocket);
                    }
                }
            }

            Inventory.items[0].count--;

            if (Inventory.items[0].count <= 0)
            {
                Inventory.items[0] = Item.empty;
            }
        }
    }
    
    public static void ConsumeItem(Entity entity, Item item)
    {
        ItemProperty itemProperty = ItemProperty.GetItemProperty(item);

        if (NetworkManager.isHost)
        {
            AddHunger(entity, itemProperty.hungerRegeneration);
        }
        else
        {
            NetworkManager.SendPacket(ConsumeItemPacket.Build(item), NetworkManager.serverConnection?.TcpSocket);
        }

        if (entity.id == myPlayerId)
        {
            Inventory.items[0].count--;

            if (Inventory.items[0].count <= 0)
            {
                Inventory.items[0] = Item.empty;
            }
        }
    }
}