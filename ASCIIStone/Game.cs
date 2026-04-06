using System.Diagnostics;
using System.Net;
using System.Text;

class Game
{
    public static HashSet<char> reservedCharacters = new HashSet<char>() // items are added automatically
    {
        '.' 
    };

    public enum GameStates
    {
        MainMenu,
        HostOrJoin,
        HostMenu,
        JoinMenu,
        Connecting,
        Game,
        Settings,
        PlayerCustomization,
    }

    public static Terrain mainTerrain = new Terrain();

    public static Dictionary<uint, Entity> entities = new Dictionary<uint, Entity>();
    public static Queue<Entity> entitiesToCreate = new Queue<Entity>();
    public static Queue<uint> entitiesToDestroy = new Queue<uint>();

    public static float deltaTime;
    private static DateTime oldTime = DateTime.Now;

    public static GameStates gameState = GameStates.MainMenu;
    public static GameStates oldGameState = GameStates.Game;

    public static uint entityID = 0;

    public void Start()
    {
        GetUsedChars();

        Input.InitializeInput();

        Renderer.Start();

        QuitToMenu();
    }

    public void GetUsedChars()
    {
        foreach (ItemProperty itemProperty in ItemProperty.itemProperties.Values)
        {
            reservedCharacters.Add((char)itemProperty.normalChar);
        }

        foreach (TileProperty tileProperty in TileProperty.tileProperties.Values)
        {
            if (tileProperty.normalChar != null)
            {
                reservedCharacters.Add((char)tileProperty.normalChar);
            }
        }

        reservedCharacters.Add(Zombie.defaultChar);
        reservedCharacters.Add(Cow.defaultChar);
    }

    public static void QuitToMenu()
    {
        UpdateDeltaTime();

        NetworkManager.Cleanup();

        mainTerrain = new Terrain();

        entities.Clear();

        entitiesToCreate.Clear();

        entitiesToDestroy.Clear();

        entityID = 0;

        Player.myPlayerId = 0;

        Inventory.ResetInventory();

        gameState = GameStates.MainMenu;
    }

    private void HostGame()
    {
        gameState = GameStates.HostMenu;

        entities.Clear();
        entitiesToCreate.Clear();
        entitiesToDestroy.Clear();

        uint id = GetEntityID();

        Player.myPlayerId = id;

        mainTerrain = new Terrain();
        mainTerrain.GenerateTerrain(Random.Shared.Next(), 128, 128, '.', ' ');

        DayNightCycle.Reset();

        Entity.CreateEntity(Player.CreateDefaultPlayer(Player.myPlayerId, Player.myPlayerChar), false, false, true);

        NetworkManager.HostGame();
    }

    private void JoinGame()
    {
        gameState = GameStates.JoinMenu;
    }

    public static uint GetEntityID()
    {
        return entityID++;
    }

    public void Update()
    {
        UpdateDeltaTime();

        ProcessGameStateChange();

        ProcessGame();

        Render();
    }

    private void ProcessGameStateChange()
    {
        if (gameState != oldGameState)
        {
            oldGameState = gameState;

            if (gameState != GameStates.Game)
            {
                try
                {
                    Console.CursorVisible = true;
                }
                catch
                {

                }

                Renderer.DrawMenu();

                //don't ask why it's not switch here and in renderer it is
                if (gameState == GameStates.MainMenu)
                {
                    char[] allowedChars = new char[] { '1', '2', '3' };

                    char output = ConsoleInput.GetCharFromConsole(allowedChars);

                    if (output == '1') { gameState = GameStates.HostOrJoin; }
                    if (output == '2') { gameState = GameStates.Settings; }
                    if (output == '3') { Program.isRunning = false; }
                }
                else if (gameState == GameStates.HostOrJoin)
                {
                    char[] allowedChars = new char[] { '1', '2', '3' };

                    char output = ConsoleInput.GetCharFromConsole(allowedChars);

                    if (output == '1') { HostGame(); }
                    if (output == '2') { JoinGame(); }
                    if (output == '3') { QuitToMenu(); }
                }
                else if (gameState == GameStates.HostMenu)
                {
                    ConsoleInput.GetCharFromConsole();

                    Input.GetKeyDown(ConsoleKey.E);
                    Input.GetKeyDown(ConsoleKey.C);

                    gameState = GameStates.Game;
                }
                else if (gameState == GameStates.JoinMenu)
                {
                    IPEndPoint? endPoint = ConsoleInput.GetEndPointFromConsole();
                    if (endPoint == null)
                    {
                        gameState = GameStates.HostOrJoin;
                    }
                    else
                    {
                        entities.Clear();
                        entitiesToCreate.Clear();
                        entitiesToDestroy.Clear();
                        NetworkManager.ConnectToServer(endPoint);
                        gameState = GameStates.Connecting;
                    }
                }
                else if (gameState == GameStates.Connecting)
                {

                }
                else if (gameState == GameStates.Settings)
                {
                    char[] allowedChars = new char[] { '1', '2' };

                    char output = ConsoleInput.GetCharFromConsole(allowedChars);

                    if (output == '1') { gameState = GameStates.PlayerCustomization; }
                    if (output == '2') { QuitToMenu(); }
                }
                else if (gameState == GameStates.PlayerCustomization)
                {
                    char? output = ConsoleInput.GetNonReservedCharFromConsole();

                    if (output == null)
                    {
                        QuitToMenu();
                    }
                    else
                    {
                        Player.myPlayerChar = (char)output;

                        oldGameState = GameStates.Settings;
                    }
                }
            }
            else
            {
                Console.CursorVisible = false;

                Console.Clear();

                Renderer.forceRedraw = true;

                UpdateDeltaTime();
                deltaTime = 0f; //forces to 0
            }
        }
    }
    private static void UpdateDeltaTime()
    {
        deltaTime = (float)((DateTime.Now - oldTime).TotalSeconds);
        oldTime = DateTime.Now;
    }

    private void ProcessGame()
    {
        if ((gameState != GameStates.Game) && (gameState != GameStates.Connecting)) return;

        if (Input.GetKey(ConsoleKey.Escape))
        {
            QuitToMenu();
        }

        if (gameState != GameStates.Game) return;

        DayNightCycle.Update();

        if (NetworkManager.isHost)
        {
            MobSpawner.Update();
        }

        foreach (Entity entity in entities.Values)
        {
            entity.Update();
        }

        while (entitiesToDestroy.TryDequeue(out uint entityToDestroy))
        {
            entities.Remove(entityToDestroy);
            Entity.paths.Remove(entityToDestroy);
        }

        while (entitiesToCreate.TryDequeue(out Entity? entityToCreate))
        {
            entities[entityToCreate.id] = entityToCreate;
        }

        mainTerrain.Update();
    }

    private void Render()
    {
        if (gameState != GameStates.Game) return;

        if (entities.TryGetValue(Player.myPlayerId, out Entity? localPlayer))
        {
            if (localPlayer.health > 0)
            {
                if (Inventory.isInInventory)
                {
                    Renderer.RenderTextFromTopLeft(Inventory.GetFormatedInventory());
                }
                else if (Crafting.isInCrafting)
                {
                    Renderer.RenderTextFromTopLeft(Crafting.GetFormatedRecipeText());
                }
                else
                {
                    int playerX = (int)MathF.Floor(localPlayer.x);
                    int playerY = (int)MathF.Floor(localPlayer.y);
                    
                    Renderer.RecalculateOcculusion(mainTerrain, playerX, playerY);

                    Renderer.RenderTerrain(mainTerrain, playerX, playerY);

                    if (Program.isDebugMode)
                    {
                        foreach (Path path in Entity.paths.Values)
                        {
                            foreach (PathTile tile in path.pathTiles)
                            {
                                Renderer.RenderAt(tile.x - playerX, tile.y - playerY, '*');
                            }
                        }
                    }

                    foreach (Entity entity in entities.Values)
                    {
                        Renderer.RenderEntity(entity, mainTerrain, playerX, playerY);
                    }
                }
            }
            else
            {
                Renderer.RenderTextFromTopLeft("You died!\nPress Space to respawn");
            }
        }

        if (Input.GetKey(ConsoleKey.Tab))
        {
            Renderer.RenderTextFromTopLeft($"FPS: {1.0 / deltaTime}");
            if (NetworkManager.isHost)
            {
                if (NetworkManager.TcpSocket != null)
                {
                    if (NetworkManager.TcpSocket.LocalEndPoint != null)
                    {
                        IPEndPoint endPoint = (IPEndPoint)(NetworkManager.TcpSocket.LocalEndPoint);
                        Renderer.RenderTextFromTopLeft($"Port: {(endPoint).Port}");
                        Renderer.RenderTextFromTopLeft(string.Empty);
                    }
                }

                StringBuilder debugData = new StringBuilder();

                Renderer.RenderTextFromTopLeft($"Players: {NetworkManager.ConnectionByID.Count + 1}");

                if (Program.isDebugMode)
                {

                    Entity entity = entities[Player.myPlayerId];
                    debugData.Append("X: ");
                    debugData.Append(MathF.Floor(entity.x));
                    debugData.Append(" Y: ");
                    debugData.Append(MathF.Floor(entity.y));

                    Renderer.RenderTextFromTopLeft($"Your coords: {debugData}");
                }

                foreach (Connection conn in NetworkManager.ConnectionByID.Values)
                {
                    if (Program.isDebugMode)
                    {
                        debugData.Clear();

                        Entity entity = entities[conn.id];
                        debugData.Append(" coords: ");
                        debugData.Append(" X: ");
                        debugData.Append(MathF.Floor(entity.x));
                        debugData.Append(" Y: ");
                        debugData.Append(MathF.Floor(entity.y));
                        debugData.Append(" health: ");
                        debugData.Append(MathF.Ceiling(entity.health));
                    }

                    Renderer.RenderTextFromTopLeft($"Player: {entities[conn.id].character} id: {conn.id}{debugData}");
                }
            }

            Renderer.RenderTextFromTopLeft($"Day {DayNightCycle.GetDayCount()}");
        }

        if (localPlayer != null && localPlayer.health > 0)
        {
            Renderer.RenderTextFromBottomLeft($"Health {MathF.Ceiling(localPlayer.health)}");
            Renderer.RenderTextFromBottomLeft($"Hunger {MathF.Ceiling(localPlayer.GetMetaAsFloat("hunger", 0f))}");
            Renderer.RenderTextFromBottomLeft($"[{Inventory.GetFormatedItemText(0, false)}] {Player.GetAttackFormatedString(localPlayer)}");
        }

        Renderer.Draw();
    }
}