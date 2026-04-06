public static class Pathfinding
{
    public static Path GetPath(int startX, int startY, int destinationX, int destinationY, Terrain terrain)
    {
        List<PathTile> openTiles = new List<PathTile>();
        List<PathTile> closedTiles = new List<PathTile>();
        openTiles.Add(new PathTile(startX, startY, 0f, 0f, null));
        PathTile destination = new PathTile(destinationX, destinationY, 0f, 0f, null);
        PathTile? endTile = null;

        while (openTiles.Count > 0)
        {
            PathTile pathTile = openTiles[0];
            openTiles.RemoveAt(0);

            endTile = SearchAtTile(pathTile, destination, ref openTiles, ref closedTiles, terrain);
            if (endTile != null)
            {
                break;
            }
        }

        if (endTile == null)
        {
            float shortestDistance = float.PositiveInfinity;

            for (int i = 0; i < closedTiles.Count; i++)
            {
                if (endTile == null)
                {
                    endTile = closedTiles[i];
                }
                else
                {
                    float distance = MathF.Abs(closedTiles[i].x - destination.x) + MathF.Abs(closedTiles[i].y - destination.y);

                    if (shortestDistance > distance)
                    {
                        endTile = closedTiles[i];
                        shortestDistance = distance;
                    }
                }
            }
        }

        List<PathTile> paths = new List<PathTile>();

        PathTile currentTile = endTile;

        while (true)
        {
            paths.Add(currentTile);
            currentTile = currentTile.previousTile;
            if (currentTile == null)
            {
                break;
            }
        }

        paths.Reverse();

        return new Path(paths.ToArray());
    }

    public static PathTile? SearchAtTile(PathTile tileToSearch, PathTile destination, ref List<PathTile> openTiles, ref List<PathTile> closedTiles, Terrain terrain)
    {
        closedTiles.Add(tileToSearch);

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (((x == 0) || (y == 0)) && (!(x == 0 && y == 0)))
                {
                    int tileX = tileToSearch.x + x;
                    int tileY = tileToSearch.y + y;
                    Tile? tile = terrain.GetTileAt(tileX, tileY);
                    if (tile.HasValue)
                    {
                        float distance = MathF.Abs(tileX - destination.x) + MathF.Abs(tileY - destination.y);


                        float cost = tileToSearch.cost + TileProperty.GetTileProperty(tile.Value).pathCost;
                        PathTile pathTile = new PathTile(tileX, tileY, cost, distance, tileToSearch);

                        if (tileX == destination.x && tileY == destination.y)
                        {
                            return pathTile;
                        }

                        TryToAddTileToList(pathTile, ref openTiles, ref closedTiles);
                    }
                }
            }
        }

        return null;
    }

    public static void TryToAddTileToList(PathTile tileToAdd, ref List<PathTile> openTiles, ref List<PathTile> closedTiles)
    {
        for (int i = 0; i < closedTiles.Count; i++)
        {
            if ((tileToAdd.x == closedTiles[i].x) && (tileToAdd.y == closedTiles[i].y))
            {
                return;
            }
        }

        for (int i = 0; i < openTiles.Count; i++)
        {
            if (tileToAdd.x == openTiles[i].x && tileToAdd.y == openTiles[i].y)
            {
                if (tileToAdd.cost + tileToAdd.distance <
                    openTiles[i].cost + openTiles[i].distance)
                {
                    openTiles[i] = tileToAdd;
                }
                return;
            }
        }


        if (openTiles.Count > 0)
        {
            for (int i = 0; i < openTiles.Count; i++)
            {
                if (openTiles[i].cost + openTiles[i].distance > tileToAdd.cost + tileToAdd.distance)
                {
                    openTiles.Insert(i, tileToAdd);
                    return;
                }
            }

            openTiles.Add(tileToAdd);
        }
        else
        {
            openTiles.Add(tileToAdd);
        }
    }
}

public class Path
{
    public PathTile[] pathTiles;

    public Path(PathTile[] pathTiles)
    {
        this.pathTiles = pathTiles;
    }
}

public class PathTile
{
    public int x;
    public int y;
    public float cost;
    public float distance;
    public PathTile? previousTile;

    public PathTile(int x, int y, float cost, float distance, PathTile? previousTile)
    {
        this.x = x;
        this.y = y;
        this.cost = cost;
        this.distance = distance;
        this.previousTile = previousTile;
    }
}