using Dalamud.Configuration;

namespace NearAssist;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public float MaxDistance { get; set; } = 30.0f;

    // Ability macros continue to their next line even if a plugin command fails.
    // Clearing on failure prevents the macro from firing at an unrelated stale target.
    public bool ClearTargetOnFailure { get; set; } = true;

    public bool ChatFeedback { get; set; }

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
