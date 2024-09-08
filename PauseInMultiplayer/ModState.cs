using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

using StardewModdingAPI;

using StardewValley;
using StardewValley.Monsters;

namespace PauseInMultiplayer;

internal sealed class ModState
{
    private readonly Dictionary<long, FarmerState> _states = new();
    public IModState Current { get => _states[Game1.player.UniqueMultiplayerID]; }

    private class FarmerState : IModState
    {
        private bool _pauseOverride = false;
        public bool PauseOverride
        {
            get => _pauseOverride; set
            {
                if (Context.IsMainPlayer)
                    _pauseOverride = value;
            }
        }
        public bool VotePause { get; set; } = false;

        public bool InEvent { get; set; } = false;
        public bool LastInEvent { get; set; } = false;

        public bool InSkull { get; set; } = false;
        public int LastSkullLevel { get; set; } = 121;

        public bool Pause { get; set; } = false;
        public bool LastPause { get; set; } = false;
        public bool PauseCommand { get; set; } = false;
        public Dictionary<string, int> BuffDurations { get; set; } = new();
        public bool LockMonsters { get; set; } = false;
        public int? HealthLock { get; set; } = null;
    }

    public IModState ForFarmer(Farmer farmer)
    {
        return _states[farmer.UniqueMultiplayerID];
    }

    public IModState ForFarmer(long farmerId)
    {
        return _states[farmerId];
    }

    public void AddFarmer(long farmerId)
    {
        if (!_states.ContainsKey(farmerId))
            _states[farmerId] = new FarmerState();
    }

    public void RemoveFarmer(long farmerId)
    {
        if (_states.ContainsKey(farmerId))
            _states.Remove(farmerId);
    }

    public void ResetFarmer(Farmer farmer)
    {
        RemoveFarmer(farmer.UniqueMultiplayerID);
        AddFarmer(farmer.UniqueMultiplayerID);
    }

    public int TotalFarmers { get => _states.Count; }

    public bool VotePauseAll { get => _states.Values.All(x => x.VotePause); }
    public int PositiveVotes { get => _states.Values.Count(x => x.VotePause); }
    public bool InEventAny { get => _states.Values.Any(x => x.InEvent); }
    public bool InSkullAll { get => _states.Values.Any(x => x.InSkull); }
    public bool PauseAll { get => _states.Values.Any(x => x.Pause); }

    public readonly Dictionary<Monster, Vector2> MonsterLocks = new();
}
