using System.Text;

public static class DebugMenu
{
    public enum DebugStates
    {
        MainMenu,
        ItemMenu
    }

    public static DebugStates debugState = DebugStates.MainMenu;

    public static Dictionary<DebugStates, DebugMenuItem[]> menuOptions = new Dictionary<DebugStates, DebugMenuItem[]>()
    {
        {DebugStates.MainMenu, new DebugMenuItem[]
        {
            new DebugMenuItem("Mob spawning", () => areMobSpawningActive.ToString(), (bool a, bool b) => areMobSpawningActive = !areMobSpawningActive),
            new DebugMenuItem("Day/night cycle", () => isDayNightCycleActive.ToString(), (bool a, bool b) => isDayNightCycleActive = !isDayNightCycleActive),
            new DebugMenuItem("Set day", null, (bool a, bool b) => DayNightCycle.SetDay()),
            new DebugMenuItem("Set night", null, (bool a, bool b) => DayNightCycle.SetNight()),
            new DebugMenuItem("Infinite crafting", () => infiniteCrafting.ToString(), (bool a, bool b) => infiniteCrafting = !infiniteCrafting),

        }
        },
    };

    public static bool isInDebugMenu = false;

    public static bool areMobSpawningActive = true;
    public static bool isDayNightCycleActive = true;

    public static bool infiniteCrafting = false;

    public static int selectedOption = 0;

    public static void ResetDebugMenu()
    {
        debugState = DebugStates.MainMenu;

        isInDebugMenu = false;
        areMobSpawningActive = true;
        isDayNightCycleActive = true;
        infiniteCrafting = false;

        selectedOption = 0;
    }

    public static void Update()
    {
        if (Input.GetKeyDown(ConsoleKey.UpArrow)) selectedOption--;
        if (Input.GetKeyDown(ConsoleKey.DownArrow)) selectedOption++;

        int maxElement = (menuOptions[debugState]).Length - 1;

        if (selectedOption < 0) selectedOption = maxElement;
        if (selectedOption > maxElement) selectedOption = 0;

        bool left = Input.GetKeyDown(ConsoleKey.LeftArrow);
        bool right = Input.GetKeyDown(ConsoleKey.RightArrow);

        if (left || right) menuOptions[debugState][selectedOption].onSelectFunction.Invoke(left, right);
    }

    public static string GetFormatedDebugMenu()
    {
        StringBuilder stringBuilder = new StringBuilder();

        int index = 0;

        foreach (DebugMenuItem debugMenuItem in menuOptions[debugState])
        {
            stringBuilder.Append(debugMenuItem);

            if (selectedOption == index) stringBuilder.Append(" <<<");

            stringBuilder.Append("\n");

            index++;
        }

        stringBuilder.Append("\n\n\nUp/Down - select item\nRight/Left - choose option\n");

        return stringBuilder.ToString();
    }
}

public class DebugMenuItem
{
    public string defaultText;
    public Func<string>? additionalDisplayValue;

    public Action<bool, bool> onSelectFunction;

    public DebugMenuItem(string defaultText, Func<string>? additionalDisplayValue, Action<bool, bool> onSelectFunction)
    {
        this.defaultText = defaultText;
        this.additionalDisplayValue = additionalDisplayValue;
        this.onSelectFunction = onSelectFunction;
    }

    public override string ToString()
    {
        if (additionalDisplayValue == null) return defaultText;

        return defaultText + " " + additionalDisplayValue.Invoke();
    }
}