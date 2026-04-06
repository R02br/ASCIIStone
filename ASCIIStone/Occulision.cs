public static class Occlusion
{
    public static byte[,] CalculateOcclusion(Terrain terrain, int camX, int camY, int halfBufferWidth, int halfBufferHeight)
    {
        int bufferWidth = halfBufferWidth * 2;
        int bufferHeight = halfBufferHeight * 2;

        byte[,] visibilityBuffer = new byte[bufferWidth, bufferHeight];

        for (int x = 0; x < bufferWidth; x++)
        {
            for (int y = 0; y < bufferHeight; y++)
            {
                visibilityBuffer[x, y] = byte.MaxValue;
            }
        }

        for (int x = 0; x < bufferWidth; x++)
        {
            CastOcclusionRay(terrain, camX, camY, halfBufferWidth, halfBufferHeight, x, 0, ref visibilityBuffer, bufferWidth, bufferHeight, halfBufferWidth, halfBufferHeight);
            CastOcclusionRay(terrain, camX, camY, halfBufferWidth, halfBufferHeight, x, bufferHeight - 1, ref visibilityBuffer, bufferWidth, bufferHeight, halfBufferWidth, halfBufferHeight);
        }

        for (int y = 0; y < bufferWidth; y++)
        {
            CastOcclusionRay(terrain, camX, camY, halfBufferWidth, halfBufferHeight, 0, y, ref visibilityBuffer, bufferWidth, bufferHeight, halfBufferWidth, halfBufferHeight);
            CastOcclusionRay(terrain, camX, camY, halfBufferWidth, halfBufferHeight, bufferWidth - 1, y, ref visibilityBuffer, bufferWidth, bufferHeight, halfBufferWidth, halfBufferHeight);
        }

        return visibilityBuffer;
    }

    public static void CastOcclusionRay(Terrain terrain, int camX, int camY, int startX, int startY, int destinationX, int destinationY, ref byte[,] visibilityBuffer, int bufferWidth, int bufferHeight, int halfBufferWidth, int halfBufferHeight)
    {
        float fx = startX;
        float fy = startY;

        int x;
        int y;

        int currentCost = 0;

        float dx = destinationX - startX;
        float dy = destinationY - startY;

        float step;

        if (Math.Abs(dx) > Math.Abs(dy))
        {
            step = Math.Abs(dx);
        }
        else
        {
            step = Math.Abs(dy);
        }

        dx /= step;
        dy /= step;

        Tile? tile;

        for (int i = 0; i <= step; i++)
        {
            x = (int)MathF.Round(fx);
            y = (int)MathF.Round(fy);

            if (x < 0 || x >= bufferWidth || y < 0 || y >= bufferHeight) return;

            tile = terrain.GetTileAt(x + camX - halfBufferWidth, y + camY - halfBufferHeight);
            if (tile == null) return;

            if (currentCost < byte.MaxValue)
            {
                visibilityBuffer[x, y] = (byte)Math.Min(visibilityBuffer[x, y], currentCost);

                currentCost += TileProperty.GetTileProperty(tile.Value).occulisionStrength;

                fx += dx;
                fy += dy;
            }
            else
            {
                return;
            }
        }
    }
}