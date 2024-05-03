using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

//Add your Discord webhook URL here
const string DISCORD_WEBHOOK_URL = "";

//Optional: if the battery goes to battery power, you set the value below to have it alert you
//(eg setting to a value of 5 means "alert me of the battery state every 5 minutes while on battery power")
const double BATTERY_STATUS_CHECK_FREQUENCY = 0;

if (args.Length > 0 && args[0] == "onbattery")
{
    var onbatteryStats = new Dictionary<string, Func<string, (string, string)>>
    {
        { "TONBATT", val => ("Time on battery", val) },
        { "TIMELEFT", val => ("Battery time remaining", val) },
        { "BCHARGE", val => ("Battery percentage", $"{Regex.Match(val, @"\d+").Value}%") },
    };

    var stats = GetAPCStats(onbatteryStats);

    await SendDiscordMessage(DISCORD_WEBHOOK_URL,
        "Main power disconnected",
        "Server UPS is now on battery power",
        16522268,
        stats);

    if (BATTERY_STATUS_CHECK_FREQUENCY > 0)
    {
        onbatteryStats.Add("STATUS", val => ("STATUS", val));

        while (true)
        {
            Thread.Sleep(TimeSpan.FromMinutes(BATTERY_STATUS_CHECK_FREQUENCY));
            
            stats = GetAPCStats(onbatteryStats);

            if (stats["STATUS"] != "ONBATT")
            {
                break;
            }

            stats.Remove("STATUS"); //Don't need the status stat in the alert

            await SendDiscordMessage(DISCORD_WEBHOOK_URL,
                "Battery power status",
                "Server UPS is on battery power",
                16522268,
                stats);
        }
    }
}
else if (args.Length > 0 && args[0] == "offbattery")
{
    var stats = GetAPCStats(new()
    {
        { "XONBATT", val => ("XONBATT", val) },
        { "XOFFBATT", val => ("XOFFBATT", val) },
    });

    //Get the amount of time we were on batteries last (apparently there's not a direct stat name for this)
    var lastDurationOnBatt = DateTime.Parse(stats["XOFFBATT"]) - DateTime.Parse(stats["XONBATT"]);

    await SendDiscordMessage(DISCORD_WEBHOOK_URL, "Main power restored", "Server UPS is now off battery power", 1899548, new()
    {
        { "Amount of time on battery power", lastDurationOnBatt.ToString() },
    });
}
else
{
    Console.WriteLine("Unrecognized arg. If you are calling APCAlert manually for testing, try running `APCAlert onbattery` or `APCAlert offbattery`");
}

async Task<string> SendDiscordMessage(string webhookUrl, string message, string description, int color, Dictionary<string, string> fields = null)
{
    fields ??= new Dictionary<string, string>();

    using (var client = new HttpClient())
    {
        var response = await client.PostAsync(webhookUrl, JsonContent.Create(new
        {
            username = "APC UPS",
            avatar_url = "https://i.imgur.com/oWNwTTp.png",
            embeds = new[]
            {
                new
                {
                    title = message,
                    description = description,
                    color = color,
                    fields = fields.Select(kvp => new
                    {
                        name = kvp.Key,
                        value = kvp.Value
                    }),
                    thumbnail = new
                    {
                        //Todo: this image looks fine on desktop, but not great on Discord mobile
                        //Todo: in the future it'd be great if the image changed based on a rounded value of the percentage left
                        url = "https://i.imgur.com/5uLdJ47.png",
                    },
                },
            },
        }));

        return await response.Content.ReadAsStringAsync();
    }
}

//Gets APC stats via apcaccess. Stat names and descriptions can be found at http://www.apcupsd.org/manual/manual.html#status-report-fields
Dictionary<string, string> GetAPCStats(Dictionary<string, Func<string, (string, string)>> statsAndFormatters)
{
    Dictionary<string, string> stats = new Dictionary<string, string>();

    var proc = new Process 
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = @"..\..\bin\apcaccess.exe",
            Arguments = "status",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        }
    };

    proc.Start();
    while (!proc.StandardOutput.EndOfStream)
    {
        string line = proc.StandardOutput.ReadLine();
        if (!string.IsNullOrEmpty(line))
        {
            string[] stat = line.Split(new[] { ':' }, 2); //Split on first : (timestamps will have multiple)
            string statName = stat[0].Trim();
            if (statsAndFormatters.ContainsKey(statName))
            {
                (string, string) formatted = statsAndFormatters[statName].Invoke(stat[1].Trim());
                stats.Add(formatted.Item1, formatted.Item2);
            }
        }
    }

    return stats;
} 
