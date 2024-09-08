
using System.Collections.Generic;

using StardewValley;

namespace PauseInMultiplayer.Feature;

internal static class BuffFeature
{
    public static void CheckBuffs(ModState modState, bool shouldPauseNow)
    {
        //pause food and drink buff durations must be run for each player independently
        //handle health locks on a per player basis
        var buffDurations = modState.Current.BuffDurations;
        if (shouldPauseNow)
        {
            //set temporary duration locks if it has just become paused and/or update Duration if new food is consumed during pause
            foreach (KeyValuePair<string, Buff> buff in Game1.player.buffs.AppliedBuffs)
            {
                if (!buffDurations.ContainsKey(buff.Key))
                {
                    buffDurations.Add(buff.Key, buff.Value.millisecondsDuration);
                }
                else
                {
                    if (buff.Value.millisecondsDuration < buffDurations[buff.Key])
                    {
                        buff.Value.millisecondsDuration = buffDurations[buff.Key];
                    }
                }
            }
        }
        else
        {
            buffDurations.Clear();
        }
    }
}
