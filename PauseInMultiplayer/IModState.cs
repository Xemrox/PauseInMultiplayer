using System.Collections.Generic;

namespace PauseInMultiplayer;

internal interface IModState
{
    bool PauseOverride { get; set; }
    bool VotePause { get; set; }
    bool InEvent { get; set; }
    bool LastInEvent { get; set; }
    bool InSkull { get; set; }
    int LastSkullLevel { get; set; }
    bool Pause { get; set; }
    bool LastPause { get; set; }
    bool PauseCommand { get; set; }
    Dictionary<string, int> BuffDurations { get; set; }
    bool LockMonsters { get; set; }
    int? HealthLock { get; set; }
}
