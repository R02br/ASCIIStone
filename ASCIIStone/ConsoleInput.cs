using System.Net;

static class ConsoleInput
{
    public static string? GetStringFromConsole()
    {
        return Console.ReadLine();
    }

    public static char? GetCharFromConsole()
    {
        return Console.ReadKey(true).KeyChar;
    }

    public static char GetCharFromConsole(char[] allowedChars)
    {
        char c;
        do
        {
            Renderer.DrawMenu();
            c = Console.ReadKey(true).KeyChar;
        } while (!allowedChars.Contains(c));

        return c;
    }

    public static char? GetNonReservedCharFromConsole()
    {
        char c;
        do
        {
            Renderer.DrawMenu();
            c = Console.ReadKey(true).KeyChar;
            c = char.ToUpper(c);
        } while (Game.reservedCharacters.Contains(c) && !char.IsWhiteSpace(c));

        return char.IsWhiteSpace(c) ? null : c;
    }

    public static IPEndPoint? GetEndPointFromConsole()
    {
        IPEndPoint? endPoint = null;

        while (true)
        {
            string? s = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(s))
            {
                break;
            }
            else
            {
                if (IPEndPoint.TryParse(s, out endPoint))
                {
                    break;
                }
            }

            Renderer.DrawMenu();
        }

        return endPoint;
    }
}