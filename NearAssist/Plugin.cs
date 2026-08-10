using System.Globalization;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace NearAssist;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/nearassist";

    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    internal static IObjectTable ObjectTable { get; private set; } = null!;

    [PluginService]
    internal static IPartyList PartyList { get; private set; } = null!;

    [PluginService]
    internal static IClientState ClientState { get; private set; } = null!;

    [PluginService]
    internal static ITargetManager TargetManager { get; private set; } = null!;

    [PluginService]
    internal static IChatGui ChatGui { get; private set; } = null!;

    [PluginService]
    internal static IPluginLog PluginLog { get; private set; } = null!;

    internal Configuration Configuration { get; }

    private readonly AssistService assistService;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.MaxDistance = Math.Clamp(Configuration.MaxDistance, 5.0f, 60.0f);
        assistService = new AssistService(Configuration);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            AllowedInMacros = true,
            HelpMessage = "Assistiert dem nächsten Party-/Alliance-Ally. Optionen: keep, range <5-60>, clear <on|off>, feedback <on|off>, status, help",
        });
    }

    public void Dispose()
    {
        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string arguments)
    {
        var parts = arguments
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            ExecuteAssist(null);
            return;
        }

        switch (parts[0].ToLowerInvariant())
        {
            case "keep":
                ExecuteAssist(false);
                break;

            case "range" when parts.Length >= 2:
                SetRange(parts[1]);
                break;

            case "clear" when parts.Length >= 2:
                SetBoolean(parts[1], value => Configuration.ClearTargetOnFailure = value, "Clear-on-failure");
                break;

            case "feedback" when parts.Length >= 2:
                SetBoolean(parts[1], value => Configuration.ChatFeedback = value, "Chat-Feedback");
                break;

            case "status":
                PrintStatus();
                break;

            case "help":
            case "config":
            default:
                PrintHelp();
                break;
        }
    }

    private void ExecuteAssist(bool? clearTargetOnFailureOverride)
    {
        var result = assistService.TryAssist(clearTargetOnFailureOverride);
        if (!Configuration.ChatFeedback)
        {
            return;
        }

        if (result.Success)
        {
            ChatGui.Print(
                $"[Near Assist] {result.AllyName} ({result.Distance:0.0}y) → {result.TargetName}");
        }
        else
        {
            ChatGui.PrintError($"[Near Assist] {result.Error}");
        }
    }

    private void SetRange(string value)
    {
        if (!TryParseFloat(value, out var parsed) || !float.IsFinite(parsed))
        {
            ChatGui.PrintError("[Near Assist] Beispiel: /nearassist range 30");
            return;
        }

        Configuration.MaxDistance = Math.Clamp(parsed, 5.0f, 60.0f);
        Configuration.Save();
        ChatGui.Print($"[Near Assist] Reichweite: {Configuration.MaxDistance:0.#} Yalm");
    }

    private void SetBoolean(string value, Action<bool> setter, string label)
    {
        if (!TryParseBoolean(value, out var parsed))
        {
            ChatGui.PrintError($"[Near Assist] {label}: on oder off erwartet.");
            return;
        }

        setter(parsed);
        Configuration.Save();
        ChatGui.Print($"[Near Assist] {label}: {(parsed ? "an" : "aus")}");
    }

    private void PrintStatus()
    {
        ChatGui.Print(
            $"[Near Assist] Reichweite {Configuration.MaxDistance:0.#}y · " +
            $"Clear-on-failure {(Configuration.ClearTargetOnFailure ? "an" : "aus")} · " +
            $"Feedback {(Configuration.ChatFeedback ? "an" : "aus")}");
    }

    private static void PrintHelp()
    {
        ChatGui.Print("[Near Assist] /nearassist — nächstes gültiges Ally-Ziel übernehmen");
        ChatGui.Print("[Near Assist] /nearassist keep — bei Fehlschlag altes Ziel behalten");
        ChatGui.Print("[Near Assist] /nearassist range 30 — maximale Ally-Distanz");
        ChatGui.Print("[Near Assist] /nearassist clear on|off · feedback on|off · status");
    }

    private static bool TryParseFloat(string value, out float parsed)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed) ||
               float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
    }

    private static bool TryParseBoolean(string value, out bool parsed)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "on":
            case "an":
            case "true":
            case "1":
                parsed = true;
                return true;

            case "off":
            case "aus":
            case "false":
            case "0":
                parsed = false;
                return true;

            default:
                parsed = false;
                return false;
        }
    }
}
