class Program
{
    public static bool isRunning = true;
    public static bool isDebugMode = false;

    public static void Main(string[] args)
    {
        HandleArgs(args);

        Game game = new Game();
        game.Start();

        while (isRunning)
        {
            game.Update();
            Thread.Sleep(16);
        }
    }
    
    private static void HandleArgs(string[] args)
    {
        foreach (string s in args)
        {
            switch (s.ToLower())
            {
                case "debug":
                    isDebugMode = true;
                    break;
                default:
                    break;
            }
        }
    }
}