public static class MobSpawner
{
    public static float timeToSpawnAnimal = 0f;
    public const float maxTimeToSpawnAnimal = 1f;

    public static float timeToSpawnEnemy = 0f;
    public const float maxTimeToSpawnEnemy = 1f;

    public static void Update()
    {
        timeToSpawnAnimal -= Game.deltaTime;

        if (timeToSpawnAnimal <= 0f)
        {
            timeToSpawnAnimal = maxTimeToSpawnAnimal;
            TryToSpawnAnimal();
        }

        timeToSpawnEnemy -= Game.deltaTime;

        if (timeToSpawnEnemy <= 0f)
        {
            timeToSpawnEnemy = maxTimeToSpawnEnemy;
            TryToSpawnEnemy();
        }
    }

    private static void TryToSpawnAnimal()
    {
        if (!DayNightCycle.IsDay()) return;

        if (Random.Shared.NextDouble() < (0.01))
        {
            SpawnCows();
        }
    }

    private static void TryToSpawnEnemy()
    {
        if (DayNightCycle.IsDay()) return;

        if (Random.Shared.NextDouble() < (0.05))
        {
            int count = Random.Shared.Next((DayNightCycle.GetDayCount())) + 1;
            for (int i = 0; i < count; i++)
            {
                SpawnZombie();
            }
        }
    }

    public static void SpawnZombie()
    {
        Entity zombie = Zombie.CreateDefaultZombie(Game.GetEntityID());
        Entity.SpawnEntityInTerrain(zombie, Game.mainTerrain);

        Entity.CreateEntity(zombie, true, false, false);
    }

    public static void SpawnCows()
    {
        int count = Random.Shared.Next(1, 5);

        int range = 5;

        int x = Random.Shared.Next(0, Game.mainTerrain.terrainWidth);
        int y = Random.Shared.Next(0, Game.mainTerrain.terrainHeight);

        int spawnX;
        int spawnY;

        Entity cow;

        for (int i = 0; i < count; i++)
        {
            spawnX = x + Random.Shared.Next(-range, range + 1);
            spawnY = y + Random.Shared.Next(-range, range + 1);

            if (Entity.ValidatePosition(spawnX, spawnY, Game.mainTerrain))
            {
                cow = Cow.CreateDefaultCow(Game.GetEntityID(), spawnX, spawnY);
                Entity.CreateEntity(cow, true, false, false);
            }
        }
    }
}