using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace NearAssist;

internal sealed class AssistService
{
    private readonly Configuration configuration;

    public AssistService(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public AssistResult TryAssist(bool? clearTargetOnFailureOverride = null)
    {
        try
        {
            return TryAssistCore(clearTargetOnFailureOverride);
        }
        catch (Exception exception)
        {
            Plugin.PluginLog.Error(exception, "Near Assist failed while resolving an ally target.");
            if (ShouldClearTarget(clearTargetOnFailureOverride))
            {
                TryClearTargets();
            }

            return new AssistResult(
                false,
                string.Empty,
                string.Empty,
                0.0f,
                "Ziel konnte nicht sicher ermittelt werden.");
        }
    }

    private AssistResult TryAssistCore(bool? clearTargetOnFailureOverride)
    {
        var localPlayer = Plugin.ObjectTable.LocalPlayer;
        if (localPlayer is null || !localPlayer.IsValid())
        {
            return Fail("Lokaler Spieler ist gerade nicht verfügbar.", clearTargetOnFailureOverride);
        }

        var partyEntityIds = Plugin.PartyList
            .Select(member => member.EntityId)
            .Where(entityId => entityId != 0)
            .ToHashSet();

        // Materialize the currently loaded player objects once. The party list covers CC even
        // when relation flags are late; AllianceMember adds nearby allies in large PvP content.
        // Never cache these wrappers across commands, frames, or zone changes.
        var allies = Plugin.ObjectTable.PlayerObjects
            .OfType<IPlayerCharacter>()
            .Where(player =>
                player.EntityId != localPlayer.EntityId &&
                (partyEntityIds.Contains(player.EntityId) ||
                 player.StatusFlags.HasFlag(StatusFlags.PartyMember) ||
                 player.StatusFlags.HasFlag(StatusFlags.AllianceMember)))
            .ToArray();
        if (allies.Length == 0)
        {
            return Fail("Keine Party-/Alliance-Mitspieler gefunden.", clearTargetOnFailureOverride);
        }

        IGameObject? nearestTarget = null;
        string? nearestAllyName = null;
        var nearestDistanceSquared = configuration.MaxDistance * configuration.MaxDistance;

        foreach (var ally in allies)
        {
            if (!ally.IsValid() || !ally.IsTargetable || ally.IsDead || ally.CurrentHp == 0)
            {
                continue;
            }

            var distanceSquared = Vector3.DistanceSquared(localPlayer.Position, ally.Position);
            if (distanceSquared > nearestDistanceSquared)
            {
                continue;
            }

            var hardTargetId = GetNativeHardTargetId(ally);
            var target = hardTargetId == 0
                ? null
                : Plugin.ObjectTable.SearchById(hardTargetId);
            if (target is null ||
                !target.IsValid() ||
                !target.IsTargetable ||
                target.IsDead ||
                target.EntityId == 0 ||
                target is not ICharacter targetCharacter)
            {
                continue;
            }

            var targetIsAlly =
                partyEntityIds.Contains(target.EntityId) ||
                targetCharacter.StatusFlags.HasFlag(StatusFlags.PartyMember) ||
                targetCharacter.StatusFlags.HasFlag(StatusFlags.AllianceMember);
            var targetIsOpponent =
                !targetIsAlly &&
                (targetCharacter.StatusFlags.HasFlag(StatusFlags.Hostile) ||
                 (Plugin.ClientState.IsPvPExcludingDen && target is IPlayerCharacter));
            if (!targetIsOpponent)
            {
                continue;
            }

            nearestDistanceSquared = distanceSquared;
            nearestTarget = target;
            nearestAllyName = ally.Name.TextValue;
        }

        if (nearestTarget is null)
        {
            return Fail(
                $"Kein Ally mit gültigem Ziel innerhalb von {configuration.MaxDistance:0.#} Yalm gefunden.",
                clearTargetOnFailureOverride);
        }

        Plugin.TargetManager.SoftTarget = null;
        Plugin.TargetManager.Target = nearestTarget;
        return new AssistResult(
            true,
            nearestAllyName ?? "Ally",
            nearestTarget.Name.TextValue,
            MathF.Sqrt(nearestDistanceSquared),
            string.Empty);
    }

    private AssistResult Fail(string message, bool? clearTargetOnFailureOverride)
    {
        if (ShouldClearTarget(clearTargetOnFailureOverride))
        {
            TryClearTargets();
        }

        return new AssistResult(false, string.Empty, string.Empty, 0.0f, message);
    }

    private bool ShouldClearTarget(bool? clearTargetOnFailureOverride) =>
        clearTargetOnFailureOverride ?? configuration.ClearTargetOnFailure;

    private static unsafe ulong GetNativeHardTargetId(IPlayerCharacter player)
    {
        if (player.Address == nint.Zero)
        {
            return 0;
        }

        return ((Character*)player.Address)->GetTargetId().Id;
    }

    private static void TryClearTargets()
    {
        try
        {
            Plugin.TargetManager.SoftTarget = null;
            Plugin.TargetManager.Target = null;
        }
        catch (Exception exception)
        {
            Plugin.PluginLog.Warning(exception, "Near Assist could not clear the current target.");
        }
    }
}

internal readonly record struct AssistResult(
    bool Success,
    string AllyName,
    string TargetName,
    float Distance,
    string Error);
