using StardewValley;

namespace PauseInMultiplayer.Feature;

internal static class HealthlockFeature
{
    public static void CheckHealthLock(ModState modState, bool shouldPauseNow)
    {
        if (shouldPauseNow)
        {
            if (!modState.Current.LockMonsters)
                return;
            //health lock
            if (modState.Current.HealthLock is null)
                modState.Current.HealthLock = Game1.player.health;
            //catch edge cases where health has increased but asynchronously will not be applied before locking
            if (Game1.player.health > modState.Current.HealthLock)
                modState.Current.HealthLock = Game1.player.health;

            Game1.player.health = modState.Current.HealthLock.Value;

            Game1.player.temporarilyInvincible = true;
            Game1.player.temporaryInvincibilityTimer = -1000000000;
        }
        else
        {
            modState.Current.HealthLock = null;

            if (Game1.player.temporaryInvincibilityTimer < -100000000)
            {
                Game1.player.temporaryInvincibilityTimer = 0;
                Game1.player.currentTemporaryInvincibilityDuration = 0;
                Game1.player.temporarilyInvincible = false;
            }
        }
    }
}
