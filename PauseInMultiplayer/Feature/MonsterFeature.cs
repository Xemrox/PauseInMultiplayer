using System.Linq;

using StardewModdingAPI;

using StardewValley;
using StardewValley.Monsters;

namespace PauseInMultiplayer.Feature;

internal static class MonsterFeature
{
    public static void CheckMonsters(ModState modState, bool shouldPause)
    {
        if (!Context.IsMainPlayer)
            return;

        if (!modState.Current.LockMonsters)
            return;

        if (shouldPause)
        {
            var _monsterLocks = modState.MonsterLocks;
            //pause all Monsters
            foreach (GameLocation location in Game1
                .getOnlineFarmers()
                .Select(x => x.currentLocation)
                .Where(x => x is not null)
                )
            {
                foreach (Monster monster in location.characters.OfType<Monster>())
                {
                    if (!_monsterLocks.ContainsKey(monster))
                        _monsterLocks.Add(monster, monster.Position);
                    monster.Position = _monsterLocks[monster];
                    monster.stunTime.Set(100);
                    monster.movementPause = 100;
                }
            }
        }
        else
        {
            //reset monsterLocks
            modState.MonsterLocks.Clear();
        }
    }
}
