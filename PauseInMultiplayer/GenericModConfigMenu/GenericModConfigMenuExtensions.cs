using PauseInMultiplayer;

using StardewModdingAPI;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace GenericModConfigMenu;
#pragma warning restore IDE0130 // Namespace does not match folder structure

internal static class GenericModConfigMenuExtensions
{
    public static IGenericModConfigMenuApi RegisterConfigMenu(this IGenericModConfigMenuApi configMenu,
        IManifest modManifest,
        IModHelper helper,
        ModConfig config)
    {
        configMenu.Register(
            mod: modManifest,
            reset: () => config = new ModConfig(),
            save: () => helper.WriteConfig(config));

        configMenu.AddSectionTitle(
            mod: modManifest,
            text: () => "Local Settings"
        );

        configMenu.AddBoolOption(
            mod: modManifest,
            name: () => "Show X while paused",
            tooltip: () => "Toggles whether or not to display an X over the clock while paused.",
            getValue: () => config.ShowPauseX,
            setValue: value => config.ShowPauseX = value
        );

        configMenu.AddBoolOption(
            mod: modManifest,
            name: () => "Show vote messages",
            tooltip: () => "Toggles whether or not to update the chat with vote pause messages.",
            getValue: () => config.DisplayVotePauseMessages,
            setValue: value => config.DisplayVotePauseMessages = value
        );

        configMenu.AddKeybind(
            mod: modManifest,
            name: () => "Vote to pause keybind",
            tooltip: () => "Set as a key that you won't use for other purposes.",
            getValue: () => config.VotePauseHotkey,
            setValue: value => config.VotePauseHotkey = value
        );

        configMenu.AddBoolOption(
            mod: modManifest,
            name: () => "Disable Skull Cavern shaft fix",
            tooltip: () => "Only set this to true if you have a specific reason to, such as using the Skull Cavern elevator mod.",
            getValue: () => config.DisableSkullShaftFix,
            setValue: value => config.DisableSkullShaftFix = value
        );

        configMenu.AddKeybind(
            mod: modManifest,
            name: () => "Debug Hotkey",
            tooltip: () => "Set as a key that you won't use for other purposes.",
            getValue: () => config.DebugHotkey,
            setValue: value => config.DebugHotkey = value
        );

        configMenu.AddSectionTitle(
            mod: modManifest,
            text: () => "Host-Only Settings"
        );

        configMenu.AddBoolOption(
            mod: modManifest,
            name: () => "Fix Skull Cavern time",
            tooltip: () => "(host only)\nToggles whether or not to slow down time like in single-player if all players are in the Skull Cavern.",
            getValue: () => config.FixSkullTime,
            setValue: value => config.FixSkullTime = value
        );

        configMenu.AddBoolOption(
            mod: modManifest,
            name: () => "Monster/HP pause lock",
            tooltip: () => "(host only)\nToggles whether or not monsters will freeze and health will lock while paused.",
            getValue: () => config.LockMonsters,
            setValue: value => config.LockMonsters = value
        );

        configMenu.AddBoolOption(
            mod: modManifest,
            name: () => "Enable vote to pause",
            tooltip: () => "(host only)\nToggles vote to pause functionality.\nThis is in addition to normal pause functionality.",
            getValue: () => config.EnableVotePause,
            setValue: value => config.EnableVotePause = value
        );

        configMenu.AddBoolOption(
            mod: modManifest,
            name: () => "Match joining player vote to host",
            tooltip: () => "(host only)\nToggles whether or not joining players will automatically have their vote to pause set to the host's.",
            getValue: () => config.JoinVoteMatchesHost,
            setValue: value => config.JoinVoteMatchesHost = value
        );

        configMenu.AddBoolOption(
            mod: modManifest,
            name: () => "Enable pause override key",
            tooltip: () => "(host only)\nToggles whether or not pressing the pause override key will toggle pausing the game.",
            getValue: () => config.EnablePauseOverride,
            setValue: value => config.EnablePauseOverride = value
        );

        configMenu.AddKeybind(
            mod: modManifest,
            name: () => "Override pause hotkey",
            tooltip: () => "(host only)\nSet as a key that you won't use for other purposes.",
            getValue: () => config.PauseOverrideHotkey,
            setValue: value => config.PauseOverrideHotkey = value
        );

        configMenu.AddBoolOption(
            mod: modManifest,
            name: () => "Any cutscene pauses",
            tooltip: () => "(host only)\nWhen enabled, time will pause if any player is in a cutscene.",
            getValue: () => config.AnyCutscenePauses,
            setValue: value => config.AnyCutscenePauses = value
        );

        return configMenu;
    }
}
