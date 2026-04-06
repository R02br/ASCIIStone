public static class Zombie
{
    public const char defaultChar = 'Z';
    public const float defaultHealth = 20f;
    public const float defaultSpeed = 3.5f;
    public const float attackRange = 1f;
    public const float attackingSpeedMultiplier = 0.5f;

    public const float preAttackCooldown = 0.5f;
    public const float pathUpdateCooldown = 1f;
    public const float sendRate = 1 / 10f;

    public const float defaultEntityDamage = 5f;
    public const float defaultWoodDamage = 5f;
    public const float defaultStoneDamage = 1f;


    public static Entity CreateDefaultZombie(uint id)
    {
        Entity zombie = new Entity(id, Entity.EntityTypes.Zombie, 0f, 0f, defaultChar, defaultHealth);
        zombie.SetMeta("timeBeforeSend", 0f, false, Entity.metaDataTypes.FLOAT);
        zombie.SetMeta("attackCooldown", preAttackCooldown, false, Entity.metaDataTypes.FLOAT);
        zombie.SetMeta("pathUpdate", pathUpdateCooldown, false, Entity.metaDataTypes.FLOAT);

        return zombie;
    }

    public static void Update(Entity entity)
    {
        if (!NetworkManager.isHost) return;

        Movement(entity);

        SendData(entity);
    }

    public static void Movement(Entity entity)
    {
        float leastDistance = float.PositiveInfinity;
        uint id = 0;

        float dx;
        float dy;

        float dist;

        Entity player = Game.entities[Player.myPlayerId];

        if (player != null && player.health > 0f)
        {
            dx = player.x - entity.x;
            dy = player.y - entity.y;

            dist = dx * dx + dy * dy;

            if (leastDistance > dist)
            {
                leastDistance = dist;
                id = Player.myPlayerId;
            }
        }

        foreach (Connection conn in NetworkManager.ConnectionByID.Values)
        {
            player = Game.entities[conn.id];

            if (player == null || player.health <= 0f) continue;

            dx = player.x - entity.x;
            dy = player.y - entity.y;

            dist = dx * dx + dy * dy;

            if (leastDistance > dist)
            {
                leastDistance = dist;
                id = conn.id;
            }
        }

        if (float.IsInfinity(leastDistance)) return;

        player = Game.entities[id];

        float pathCooldown = entity.GetMetaAsFloat("pathUpdate", preAttackCooldown);
        if (pathCooldown <= 0f || leastDistance < 2f)
        {
            Entity.CalculatePath(entity, (int)MathF.Floor(player.x), (int)MathF.Floor(player.y));
            pathCooldown = pathUpdateCooldown;
        }
        else
        {
            pathCooldown -= Game.deltaTime;
        }

        entity.SetMeta("pathUpdate", pathCooldown + (Random.Shared.NextSingle() - 0.5f), false, Entity.metaDataTypes.FLOAT);

        PathTile? pathTile = Entity.GetNextPathTile(entity);

        dx = 0;
        dy = 0;

        if (pathTile != null)
        {
            dx = (pathTile.x + 0.5f) - entity.x;
            dy = (pathTile.y + 0.5f) - entity.y;

            float distance = 1f / MathF.Sqrt(dx * dx + dy * dy);

            dx *= defaultSpeed * distance;
            dy *= defaultSpeed * distance;
        }



        dx *= Game.deltaTime;
        dy *= Game.deltaTime;

        float cooldown = entity.GetMetaAsFloat("attackCooldown", preAttackCooldown);

        if (cooldown >= preAttackCooldown)
        {
            if (leastDistance <= attackRange)
            {
                cooldown = preAttackCooldown - Game.deltaTime;
            }
        }
        else
        {
            dx *= attackingSpeedMultiplier;
            dy *= attackingSpeedMultiplier;

            cooldown -= Game.deltaTime;
            if (cooldown <= 0)
            {
                cooldown = preAttackCooldown;

                if (leastDistance <= attackRange)
                {
                    Damage.DamageAt(new Damage.DamageData
                    {
                        tileX = (int)MathF.Floor(player.x),
                        tileY = (int)MathF.Floor(player.y),
                        hasAttacker = true,
                        attackerId = entity.id,
                        itemType = Item.ItemTypes.ZOMBIE,
                    }, Game.mainTerrain, false, out _);
                }
            }
        }
        
        Entity.MoveEntityWithCollisionsInTerrain(entity, Game.mainTerrain, dx, dy);
        
        entity.SetMeta("attackCooldown", cooldown, false, Entity.metaDataTypes.FLOAT);
    }

    public static void SendData(Entity entity)
    {
        entity.SendData(sendRate, false, true, true);
    }
}