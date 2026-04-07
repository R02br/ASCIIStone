public static class DayNightCycle
{
    public static double time;

    public const double dayLength = 180;
    public const double nightLength = 120;
    public const double dayToNightFadeTime = 20;

    public static float timeBeforeSend = 0f;
    public const float sendRate = 10f;

    public static void Reset()
    {
        time = 0f;
    }

    public static void SetDay()
    {
        time = dayToNightFadeTime;
    }

    public static void SetNight()
    {
        time = dayLength + dayToNightFadeTime;
    }

    public static void Update()
    {
        time += Game.deltaTime;

        if (NetworkManager.isHost)
        {
            timeBeforeSend -= Game.deltaTime;
            if (timeBeforeSend <= 0f)
            {
                timeBeforeSend = sendRate;
                NetworkManager.SendToAllClients(TimePacket.Build((float)time), false, null);
            }
        }
    }

    public static int GetDayCount()
    {
        return (int)Math.Floor(time / (dayLength + nightLength));
    }

    public static bool IsDay()
    {
        return time % (dayLength + nightLength) <= dayLength;
    }
    
    public static double GetDayLevel()
    {
        double normTime = time % (dayLength + nightLength);

        if (normTime <= dayLength)
        {
            if (normTime <= dayToNightFadeTime)
            {
                return normTime / dayToNightFadeTime;
            }
            else
            {
                if (normTime >= dayLength - dayToNightFadeTime)
                {
                    return 1.0 - (normTime - (dayLength - dayToNightFadeTime)) / dayToNightFadeTime;
                }
                else
                {
                    return 1.0;
                }
            }
        }
        else
        {
            if (normTime <= dayLength + dayToNightFadeTime)
            {
                return (dayLength - normTime) / dayToNightFadeTime;
            }
            else
            {
                if (normTime >= dayLength + nightLength - dayToNightFadeTime)
                {
                    return -1.0 + (normTime - (dayLength + nightLength - dayToNightFadeTime)) / dayToNightFadeTime;
                }
                else
                {
                    return -1.0;
                }
            }
        }
    }
}