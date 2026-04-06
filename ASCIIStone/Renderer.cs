using System.Net;
using System.Diagnostics;
using System.Text;

static class Renderer
{
    public static bool forceRedraw = false;
    public static bool recalculateOcculusion = true;

    public const int screenWidth = 100;
    public const int screenHeight = 30;

    public const int halfScreenWidth = screenWidth / 2;
    public const int halfScreenHeight = screenHeight / 2;

    public const float screenAspectRatio = screenHeight / screenWidth;
    public const float charAspectRatio = 24f / 11f;

    public const float aspectRatio = screenAspectRatio * charAspectRatio;

    private static char[,] renderBuffer = new char[screenWidth, screenHeight];
    private static char[,] renderBackBuffer = new char[screenWidth, screenHeight];
    private static byte[,] occulisionBuffer = new byte[screenWidth, screenHeight];

    private static int topTextRenderOffset = 0;
    private static int bottomTextRenderOffset = 0;

    public static float maxVisibilityRadius = halfScreenWidth;
    public static float minVisibilityRadius = 10;

    public static byte maxOcculisionVisibility = byte.MaxValue;
    public static byte minOcculisionVisibility = 1;

    public static void Start()
    {
        Console.SetWindowSize(screenWidth + 2, screenHeight + 2);
        Console.SetBufferSize(screenWidth + 2, screenHeight + 2);

        Console.CursorVisible = false;


        for (int x = 0; x < screenWidth; x++)
        {
            for (int y = 0; y < screenHeight; y++)
            {
                renderBuffer[x, y] = ' ';
                renderBackBuffer[x, y] = ' ';
                occulisionBuffer[x, y] = 0;
            }
        }
    }

    public static void RenderObjectAt(int localX, int localY, int worldX, int worldY, Terrain? terrain, char c)
    {
        bool isInVisibilityRadius = IsInVisibilityRadius(localX, localY);
        bool isInLineOfSight = IsInLineOfSight(localX, localY);
        bool isLit = false;

        if (terrain != null)
        {
            isLit = terrain.IsLitAt(worldX, worldY);
        }

        RenderAt(localX, localY, isInLineOfSight && (isInVisibilityRadius || isLit) ? c : ' ');
    }

    public static void RenderAt(int localX, int localY, char c)
    {
        int x = halfScreenWidth + localX;
        int y = halfScreenHeight - localY;

        if (x >= screenWidth || x < 0) return;
        if (y >= screenHeight || y < 0) return;

        renderBuffer[x, y] = c;
    }

    public static void RenderTextFromTopLeft(string text)
    {
        int x = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n' || x >= screenWidth)
            {
                topTextRenderOffset++;
                x = 0;
            }
            else
            {
                renderBuffer[x, topTextRenderOffset] = text[i];

                x++;
            }
        }

        topTextRenderOffset++;
    }

    public static void RenderTextFromBottomLeft(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (i >= screenWidth) { break; }

            renderBuffer[i, screenHeight - (1 + bottomTextRenderOffset)] = text[i];
        }

        bottomTextRenderOffset++;
    }

    public static void RenderTerrain(Terrain terrain, int camX, int camY)
    {
        for (int x = -halfScreenWidth; x <= halfScreenWidth; x++)
        {
            for (int y = -halfScreenHeight; y <= halfScreenHeight; y++)
            {
                int worldX = x + camX;
                int worldY = y + camY;

                char tileToRender;

                Tile? tile = terrain.GetTileAt(worldX, worldY);
                if (tile == null)
                {
                    tileToRender = terrain.defaultEdge;
                }
                else
                {
                    char? tileChar = TileProperty.tileProperties[((Tile)tile).tileType].normalChar;
                    if (tileChar == null)
                    {
                        tileToRender = terrain.defaultGround;
                    }
                    else
                    {
                        tileToRender = (char)tileChar;
                    }
                }

                RenderObjectAt(x, y, worldX, worldY, terrain, tileToRender);
            }
        }
    }

    public static void RenderEntity(Entity entity, Terrain terrain, int camX, int camY)
    {
        int worldX = (int)MathF.Floor(entity.x);
        int worldY = (int)MathF.Floor(entity.y);
        int x = worldX - camX;
        int y = worldY - camY;

        RenderObjectAt(x, y, worldX, worldY, terrain, entity.character);
    }

    public static bool IsInVisibilityRadius(int localX, int localY)
    {
        double visibility = minVisibilityRadius + (maxVisibilityRadius - minVisibilityRadius) * ((DayNightCycle.GetDayLevel() + 1) * 0.5);

        return (MathF.Abs(localX) + MathF.Abs(localY)) <= visibility;
    }

    public static bool IsInLineOfSight(int localX, int localY)
    {
        int x = halfScreenWidth + localX;
        int y = halfScreenHeight + localY;

        if (x >= screenWidth || x < 0) return false;
        if (y >= screenHeight || y < 0) return false;

        double visibility = minOcculisionVisibility + (maxOcculisionVisibility - minOcculisionVisibility) * ((DayNightCycle.GetDayLevel() + 1) * 0.5);

        return occulisionBuffer[x, y] < Math.Floor(visibility);
    }

    public static void RecalculateOcculusion(Terrain terrain, int camX, int camY)
    {
        if (recalculateOcculusion)
        {
            occulisionBuffer = Occulision.CalculateOcculision(terrain, camX, camY, halfScreenWidth, halfScreenHeight);
            recalculateOcculusion = true;
        }
    }

    public static void Draw()
    {
        StringBuilder s = new StringBuilder();

        for (int y = 0; y < screenHeight; y++)
        {
            for (int x = 0; x < screenWidth; x++)
            {
                if (forceRedraw)
                {
                    Console.SetCursorPosition(x, y);
                    s.Append(renderBuffer[x, y]);
                    renderBackBuffer[x, y] = renderBuffer[x, y];
                    Console.Write(s.ToString());
                    s.Clear();
                }
                else
                {
                    if (renderBuffer[x, y] != renderBackBuffer[x, y])
                    {
                        Console.SetCursorPosition(x, y);

                        s.Append(renderBuffer[x, y]);

                        renderBackBuffer[x, y] = renderBuffer[x, y];

                        x++;
                        while (x < screenWidth && (renderBuffer[x, y] != renderBackBuffer[x, y]))
                        {
                            s.Append(renderBuffer[x, y]);
                            renderBackBuffer[x, y] = renderBuffer[x, y];

                            x++;
                        }

                        Console.Write(s.ToString());
                        s.Clear();
                    }
                }
            }
        }

        if (forceRedraw)
        {
            forceRedraw = false;
        }

        for (int x = 0; x < screenWidth; x++)
        {
            for (int y = 0; y < screenHeight; y++)
            {
                renderBuffer[x, y] = ' ';
            }
        }

        topTextRenderOffset = 0;
        bottomTextRenderOffset = 0;
    }

    public static void DrawMenu()
    {
        Console.Clear();

        switch (Game.gameState)
        {
            case Game.GameStates.MainMenu:
                Console.WriteLine("ASCII Stone\n");
                Console.WriteLine("1. Play");
                Console.WriteLine("2. Settings");
                Console.WriteLine("3. Quit");
                break;
            case Game.GameStates.HostOrJoin:
                Console.WriteLine("1. Host");
                Console.WriteLine("2. Join");
                Console.WriteLine("3. Return to main menu");
                break;
            case Game.GameStates.HostMenu:
                if (NetworkManager.TcpSocket == null) { break; }
                if (NetworkManager.TcpSocket.LocalEndPoint == null) { break; }
                IPEndPoint endPoint = (IPEndPoint)(NetworkManager.TcpSocket.LocalEndPoint);
                Console.WriteLine($"Your port is: {(endPoint).Port}");
                Console.WriteLine("Remember you can always find your port by pressing TAB\n");
                Console.WriteLine("Press any key to start...");
                break;
            case Game.GameStates.JoinMenu:
                Console.WriteLine("Enter server ip and port\n(ip:port)");
                break;
            case Game.GameStates.Connecting:
                Console.WriteLine("Connecting...\nPress Esc to cancel connection");
                break;
            case Game.GameStates.Settings:
                Console.WriteLine("1. Player customization");
                Console.WriteLine("2. Return to main menu");
                break;
            case Game.GameStates.PlayerCustomization:
                Console.WriteLine("Enter your player symbol");

                Console.Write($"Reserved chars: ");
                foreach (char c in Game.reservedCharacters)
                {
                    Console.Write($"{c} ");
                }
                
                Console.WriteLine($"\nYour char: {Player.myPlayerChar}");
                break;
        }
    }

}