public static class Building
{
    public struct BuildingData
    {
        public int tileX;
        public int tileY;
        public Item.ItemTypes itemType;
        public TileTypes tileType;
    }

    public static bool TryBuildAt(BuildingData buildingData)
    {
        if (ValidatePosition(buildingData.tileX, buildingData.tileY, Game.mainTerrain))
        {
            Game.mainTerrain.ModifyTileAt(new TileModification
            {
                x = buildingData.tileX,
                y = buildingData.tileY,
                tile = TileProperty.tileProperties[buildingData.tileType].CreateDefaultTile(),
            }, true, false);

            return true;
        }
        else
        {
            return false;
        }
    }
    
    public static bool ValidatePosition(int tileX, int tileY, Terrain terrain)
    {
        foreach (Entity entity in Game.entities.Values)
        {
            int x = (int)MathF.Floor(entity.x);
            int y = (int)MathF.Floor(entity.y);

            if (x == tileX && y == tileY)
            {
                return false;
            }
        }


        Tile? tile = terrain.GetTileAt(tileX, tileY);

        if (tile == null) { return false; }

        return TileProperty.GetTileProperty(tile.Value).isBuildableOn;
    }
}