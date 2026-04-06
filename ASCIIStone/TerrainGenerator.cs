using static TileProperty;

public static class TerrainGenerator
{
    public const float MIN_RIVER_START_ANGLE = -1.0f;
    public const float MAX_RIVER_START_ANGLE = 1.0f;


    public static Tile[,] GenerateTerrain(int seed, int terrainWidth, int terrainHeight)
    {
        Random random = new Random(seed);

        Tile[,] tiles = new Tile[terrainWidth, terrainHeight];

        for (int x = 0; x < terrainWidth; x++)
        {
            for (int y = 0; y < terrainHeight; y++)
            {
                tiles[x, y] = tileProperties[TileTypes.AIR].CreateDefaultTile();
            }
        }


        tiles = AddTrees(tiles, terrainWidth, terrainHeight, 0.0005, 0.15, random);

        tiles = AddRocks(tiles, terrainWidth, terrainHeight, 0.0003, random);

        tiles = AddRivers(tiles, terrainWidth, terrainHeight, 0.25, random);

        return tiles;
    }

    public static Tile[,] FillCircle(Tile[,] tiles, int terrainWidth, int terrainHeight, int x, int y, int radius, Tile tile, double fillChance, Random? random)
    {
        int radiusSqr = radius * radius;

        for (int i = x - radius; i <= (x + radius); i++)
        {
            for (int j = y - radius; j <= (y + radius); j++)
            {
                if ((i >= 0 && i < terrainWidth) && (j >= 0 && j < terrainHeight))
                {
                    float dx = x - i;
                    float dy = y - j;
                    float distSqr = (dx * dx) + (dy * dy);
                    if (distSqr <= radiusSqr)
                    {
                        //doesn't generate useless numbers if you just want to fill the circle
                        if (random == null || fillChance >= 1.0 || random.NextDouble() < fillChance)
                        {
                            tiles[i, j] = tile;
                        }
                    }
                }
            }
        }

        return tiles;
    }

    public static Tile[,] AddTrees(Tile[,] tiles, int terrainWidth, int terrainHeight, double density, double chanceInPatch, Random random)
    {
        for (int x = 0; x < terrainWidth; x++)
        {
            for (int y = 0; y < terrainHeight; y++)
            {
                if (random.NextDouble() < density)
                {
                    int size = random.Next(3, 15);

                    tiles = FillCircle(tiles, terrainWidth, terrainHeight, x, y, size, tileProperties[TileTypes.TREE].CreateDefaultTile(), chanceInPatch, random);
                }
            }
        }

        return tiles;
    }

    public static Tile[,] AddRocks(Tile[,] tiles, int terrainWidth, int terrainHeight, double density, Random random)
    {
        for (int x = 0; x < terrainWidth; x++)
        {
            for (int y = 0; y < terrainHeight; y++)
            {
                if (random.NextDouble() < density)
                {
                    int offsetX = 0;
                    int offsetY = 0;
                    int tries = random.Next(3);
                    int size = random.Next(1, 4);

                    for (int k = 0; k <= tries; k++)
                    {
                        offsetX += random.Next(-5, 6);
                        offsetY += random.Next(-5, 6);

                        tiles = FillCircle(tiles, terrainWidth, terrainHeight, x + offsetX, y + offsetY, size, tileProperties[TileTypes.ROCK].CreateDefaultTile(), 1.0, null);
                    }
                }
            }
        }

        return tiles;
    }

    public static Tile[,] AddRivers(Tile[,] tiles, int terrainWidth, int terrainHeight, double density, Random random)
    {
        int riverCount = 1;

        while (random.NextDouble() < density) riverCount++;

        for (int i = 0; i < riverCount; i++)
        {
            int riverX;
            int riverY;

            float riverDirection = MIN_RIVER_START_ANGLE + (MAX_RIVER_START_ANGLE - MIN_RIVER_START_ANGLE) * random.NextSingle();

            if (random.Next(2) == 0)
            {
                if (random.Next(2) == 0)
                {
                    riverX = 0;
                    riverDirection += MathF.PI / 2f;
                }
                else
                {
                    riverX = terrainWidth - 1;
                    riverDirection -= MathF.PI / 2f;
                }

                riverY = random.Next(terrainHeight);
            }
            else
            {
                riverX = random.Next(terrainHeight);

                if (random.Next(2) == 0)
                {
                    riverY = 0;
                    riverDirection += 0f;
                }
                else
                {
                    riverY = terrainWidth - 1;
                    riverDirection += MathF.PI;
                }
            }

            tiles = AddRiver(tiles, riverX, riverY, riverDirection, terrainWidth, terrainHeight, 0.005, random);
        }

        return tiles;
    }

    public static Tile[,] AddRiver(Tile[,] tiles, float riverX, float riverY, float riverDirection, int terrainWidth, int terrainHeight, double chanceToSplitPerTile, Random random)
    {
        while (riverX >= 0 && riverX < terrainWidth && riverY >= 0 && riverY < terrainWidth)
        {
            tiles = FillCircle(tiles, terrainWidth, terrainHeight, (int)MathF.Floor(riverX), (int)MathF.Floor(riverY), 2, tileProperties[TileTypes.WATER].CreateDefaultTile(), 1.0, null);

            riverX += MathF.Sin(riverDirection);
            riverY += MathF.Cos(riverDirection);

            if (random.NextDouble() < chanceToSplitPerTile)
            {
                riverDirection += (random.NextSingle() - 0.5f) * MathF.PI;
                tiles = AddRiver(tiles, riverX, riverY, riverDirection + (random.NextSingle() - 0.5f) * MathF.PI, terrainWidth, terrainHeight, chanceToSplitPerTile / 2.0, random);
            }

            if (random.NextDouble() < 0.03)
            {
                riverDirection += (random.NextSingle() - 0.5f) * MathF.PI / 3f;
            }
        }

        return tiles;
    }
}