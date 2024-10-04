using StardewModdingAPI;

namespace PauseInMultiplayer;

internal sealed class ModConfig
{
    public bool ShowPauseX { get; set; } = true;
    public bool FixSkullTime { get; set; } = true;
    public bool DisableSkullShaftFix { get; set; } = false;
    public bool LockMonsters { get; set; } = true;
    public bool AnyCutscenePauses { get; set; } = false;
    public bool EnableVotePause { get; set; } = true;
    public bool JoinVoteMatchesHost { get; set; } = true;
    public bool EnablePauseOverride { get; set; } = true;
    public bool DisplayVotePauseMessages { get; set; } = true;
    public SButton PauseOverrideHotkey { get; set; } = SButton.Scroll;
    public SButton VotePauseHotkey { get; set; } = SButton.Pause;
}
