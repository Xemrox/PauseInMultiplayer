
using System;

using GenericModConfigMenu;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using PauseInMultiplayer.Feature;
using PauseInMultiplayer.GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;

using StardewValley;

namespace PauseInMultiplayer;

public sealed class PauseInMultiplayer : Mod
{
    private ModState ModState { get; } = new();
    private ModConfig config = new();

    // these apply only to the main player
    private bool extraTimeAdded = false;
    private int _timeInterval = -100;

    public override void Entry(IModHelper helper)
    {
        config = Helper.ReadConfig<ModConfig>();

        bool skullElevatorMod = Helper.ModRegistry.Get("SkullCavernElevator") is not null;
        if (skullElevatorMod)
        {
            Monitor.Log("DisableSkullShaftFix set to true due to SkullCavernElevator mod.", LogLevel.Debug);
            config.DisableSkullShaftFix = true;
        }

        Helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
        Helper.Events.GameLoop.UpdateTicking += GameLoop_UpdateTicking;
        Helper.Events.GameLoop.Saving += GameLoop_Saving;
        Helper.Events.GameLoop.DayEnding += GameLoop_DayEnding;
        Helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;

        Helper.Events.Multiplayer.ModMessageReceived += Multiplayer_ModMessageReceived;
        Helper.Events.Multiplayer.PeerConnected += Multiplayer_PeerConnected;
        Helper.Events.Multiplayer.PeerDisconnected += Multiplayer_PeerDisconnected;

        Helper.Events.Display.RenderedHud += Display_Rendered;

        Helper.Events.Input.ButtonPressed += Input_ButtonPressed;
    }

    private void GameLoop_GameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        //GMCM support
        var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is null)
            return;

        configMenu.RegisterConfigMenu(ModManifest, Helper, config);
    }

    private void Input_ButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (e.Button == config.VotePauseHotkey)
        {
            VotePauseToggle();
        }
        else if (e.Button == config.PauseOverrideHotkey && config.EnablePauseOverride)
        {
            OverridePauseToggle();
        }
    }

    private void GameLoop_DayEnding(object? sender, DayEndingEventArgs e)
    {
        //reset invincibility settings while saving to help prevent future potential errors if the mod is disabled later
        //redundant with Saving to handle farmhand inconsistency
        Game1.player.temporaryInvincibilityTimer = 0;
        Game1.player.currentTemporaryInvincibilityDuration = 0;
        Game1.player.temporarilyInvincible = false;
    }

    private void GameLoop_Saving(object? sender, SavingEventArgs e)
    {
        //reset invincibility settings while saving to help prevent future potential errors if the mod is disabled later
        //redundant with DayEnding to handle farmhand inconsistency
        Game1.player.temporaryInvincibilityTimer = 0;
        Game1.player.currentTemporaryInvincibilityDuration = 0;
        Game1.player.temporarilyInvincible = false;
    }

    private void Display_Rendered(object? sender, RenderedHudEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        //draw X over time indicator
        if (config.ShowPauseX && Game1.displayHUD && ShouldPause())
        {
            Game1.PushUIMode();

            Game1.spriteBatch.Draw(
                Game1.mouseCursors,
                UpdatePosition(),
                new Rectangle(269, 471, 15, 15),
                new Color(0, 0, 0, 64),
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                0.91f);

            Game1.PopUIMode();
        }
    }

    private void Multiplayer_PeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
    {
        if (!Context.IsMainPlayer)
        {
            return;
        }

        ModState.RemoveFarmer(e.Peer.PlayerID);
    }

    private void Multiplayer_PeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        CheckForSameVersion(e);

        ModState.AddFarmer(e.Peer.PlayerID);

        //send current pause state
        Helper.Multiplayer.SendMessage(ModState.Current.LastPause, NetworkConstants.PauseCommand, new[] { ModManifest.UniqueID }, new[] { e.Peer.PlayerID });

        //send message denoting whether or not monsters will be locked
        Helper.Multiplayer.SendMessage(ModState.Current.LockMonsters, NetworkConstants.LockMonstersCommand, modIDs: new[] { ModManifest.UniqueID }, playerIDs: new[] { e.Peer.PlayerID });

        //sets the joined player's vote to pause as the host's if enabled
        if (config.JoinVoteMatchesHost && config.EnableVotePause)
        {
            bool votePause = ModState.ForFarmer(e.Peer.PlayerID).VotePause = ModState.Current.VotePause;

            //sync votePause to local client for joining player
            Helper.Multiplayer.SendMessage(votePause, NetworkConstants.SetPauseVoteCommand, new[] { ModManifest.UniqueID }, new[] { e.Peer.PlayerID });

            if (votePause)
            {
                string message = $"{Game1.getFarmer(e.Peer.PlayerID).Name} joined with a vote to pause. ({ModState.PositiveVotes}/{ModState.TotalFarmers})";
                Helper.Multiplayer.SendMessage(message, NetworkConstants.ChatInfoCommand, new[] { ModManifest.UniqueID });
                Game1.chatBox.addInfoMessage(message);
            }
            else
            {
                //seems unnecessary to display message for joining without a votepause via host
                //this.Helper.Multiplayer.SendMessage<string>($"{Game1.getFarmer(e.Peer.PlayerID).Name} joined with a vote to unpause. ({votedYes}/{Game1.getOnlineFarmers().Count})", "info", new[] { this.ModManifest.UniqueID });
                //Game1.chatBox.addInfoMessage($"{Game1.getFarmer(e.Peer.PlayerID).Name} joined with a vote to unpause. ({votedYes}/{Game1.getOnlineFarmers().Count})");
            }

        }

    }

    private void CheckForSameVersion(PeerConnectedEventArgs e)
    {
        //check for version match
        IMultiplayerPeerMod? pauseMod = e.Peer.GetMod(ModManifest.UniqueID);
        if (pauseMod is null)
            Game1.chatBox.addErrorMessage("Farmhand " + Game1.getFarmer(e.Peer.PlayerID).Name + " does not have Pause in Multiplayer mod.");
        else if (!pauseMod.Version.Equals(ModManifest.Version))
        {
            Game1.chatBox.addErrorMessage("Farmhand " + Game1.getFarmer(e.Peer.PlayerID).Name + " has mismatched Pause in Multiplayer version.");
            Game1.chatBox.addErrorMessage($"Host Version: {ModManifest.Version} | {Game1.getFarmer(e.Peer.PlayerID).Name} Version: {pauseMod.Version}");
        }
    }

    private void Multiplayer_ModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != ModManifest.UniqueID)
            return;

        if (e.Type == NetworkConstants.ChatInfoCommand)
        {
            if (config.DisplayVotePauseMessages)
                Game1.chatBox.addInfoMessage(e.ReadAs<string>());

            return;
        }

        if (Context.IsMainPlayer)
        {
            HandleMainPlayerMessage(e);
        }
        else
        {
            HandleRemotePlayerMessage(e);
        }
    }

    private void HandleRemotePlayerMessage(ModMessageReceivedEventArgs e)
    {
        if (e.Type == NetworkConstants.PauseCommand)
        {
            ModState.Current.PauseCommand = e.ReadAs<bool>();
        }
        else if (e.Type == NetworkConstants.LockMonstersCommand)
        {
            ModState.Current.LockMonsters = e.ReadAs<bool>();
        }
        else if (e.Type == NetworkConstants.SetPauseVoteCommand)
        {
            ModState.Current.VotePause = e.ReadAs<bool>();
        }
    }

    private void HandleMainPlayerMessage(ModMessageReceivedEventArgs e)
    {
        if (e.Type == NetworkConstants.PauseTimeCommand)
        {
            ModState.ForFarmer(e.FromPlayerID).Pause = e.ReadAs<bool>();
        }
        else if (e.Type == NetworkConstants.InSkullCommand)
        {
            ModState.ForFarmer(e.FromPlayerID).InSkull = e.ReadAs<bool>();
        }
        else if (e.Type == NetworkConstants.VotePauseCommand)
        {
            if (!config.EnableVotePause)
                return;

            bool voteValue = e.ReadAs<bool>();
            ModState.ForFarmer(e.FromPlayerID).VotePause = voteValue;

            if (voteValue)
            {
                string message = $"{Game1.getFarmer(e.FromPlayerID).Name} voted to pause the game. ({ModState.PositiveVotes}/{ModState.TotalFarmers})";
                Helper.Multiplayer.SendMessage(message, NetworkConstants.ChatInfoCommand, new[] { ModManifest.UniqueID });
                Game1.chatBox.addInfoMessage(message);
            }
            else
            {
                string message = $"{Game1.getFarmer(e.FromPlayerID).Name} voted to unpause the game. ({ModState.PositiveVotes}/{ModState.TotalFarmers})";
                Helper.Multiplayer.SendMessage(message, NetworkConstants.ChatInfoCommand, new[] { ModManifest.UniqueID });
                Game1.chatBox.addInfoMessage(message);
            }
        }
        else if (e.Type == NetworkConstants.EventUpCommand)
        {
            ModState.ForFarmer(e.FromPlayerID).InEvent = e.ReadAs<bool>();
        }
    }

    private void GameLoop_SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        //only the main player will use this dictionary
        if (!Context.IsMainPlayer)
        {
            return;
        }

        ModState.ResetFarmer(Game1.player);

        //setup lockMonsters for main player
        ModState.Current.LockMonsters = config.LockMonsters;
    }

    private void GameLoop_UpdateTicking(object? sender, UpdateTickingEventArgs e)
    {
        //this mod does nothing if a game isn't running
        if (!Context.IsWorldReady)
            return;

        //start with checking for events
        CheckEventState();

        CheckAndFixSkullLogic();

        CheckPauseState();

        //handle pause time data
        bool shouldPauseNow = ShouldPause();

        AnimalAndCharacterFeature.CheckCharactersAndAnimals(shouldPauseNow);

        MonsterFeature.CheckMonsters(ModState, shouldPauseNow);

        ApplyShaftFix();

        BuffFeature.CheckBuffs(ModState, shouldPauseNow);

        HealthlockFeature.CheckHealthLock(ModState, shouldPauseNow);

        ApplyPauseState(shouldPauseNow);
    }

    private void OverridePauseToggle()
    {
        if (!Context.IsMainPlayer)
            return;

        ModState.Current.PauseOverride = !ModState.Current.PauseOverride;

        if (!config.DisplayVotePauseMessages)
            return;

        if (ModState.Current.PauseOverride)
        {
            Helper.Multiplayer.SendMessage("The host has paused via override.", NetworkConstants.ChatInfoCommand, new[] { ModManifest.UniqueID });
            Game1.chatBox.addInfoMessage("The host has paused via override.");
        }
        else
        {
            Helper.Multiplayer.SendMessage("The host has unpaused their override.", NetworkConstants.ChatInfoCommand, new[] { ModManifest.UniqueID });
            Game1.chatBox.addInfoMessage("The host has unpaused their override.");
        }
    }

    private void ApplyPauseState(bool shouldPauseNow)
    {
        if (!Context.IsMainPlayer)
            return;

        //this logic only applies for the main player to control the state of the world
        if (shouldPauseNow)
        {
            //save the last time interval, if it's not already saved
            if (Game1.gameTimeInterval >= 0)
                _timeInterval = Game1.gameTimeInterval;

            Game1.gameTimeInterval = -100;
        }
        else
        {
            //reset time interval if it hasn't been fixed from the last pause
            if (Game1.gameTimeInterval < 0)
            {

                Game1.gameTimeInterval = _timeInterval;
                _timeInterval = -100;
            }
        }

        if (shouldPauseNow != ModState.Current.LastPause)
            Helper.Multiplayer.SendMessage(shouldPauseNow, NetworkConstants.PauseCommand, new[] { ModManifest.UniqueID });

        ModState.Current.LastPause = shouldPauseNow;
    }

    private void CheckPauseState()
    {
        //set the pause time data to whether or not time should be paused for this player
        bool shouldPause = !Context.IsPlayerFree;

        if (!Context.CanPlayerMove)
            shouldPause = true;

        //time should not be paused when using a tool
        if (Game1.player.UsingTool)
            shouldPause = false;

        //checks to see if the fishing rod has been cast. If this is true but the player is in the fishing minigame, the next if statement will pause - otherwise it won't
        if (Game1.player.CurrentItem is StardewValley.Tools.FishingRod fishingRod && fishingRod.isFishing)
            shouldPause = false;

        if (Game1.activeClickableMenu is StardewValley.Menus.BobberBar)
            shouldPause = true;

        if (Game1.currentMinigame is not null)
            shouldPause = true;

        if (ModState.Current.Pause != shouldPause)
        {
            ModState.Current.Pause = shouldPause;

            if (!Context.IsMainPlayer)
            {
                Helper.Multiplayer.SendMessage(shouldPause, NetworkConstants.PauseTimeCommand, modIDs: new[] { ModManifest.UniqueID }, playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
            }
        }
    }

    private void CheckEventState()
    {
        if (ModState.Current.LastInEvent == Game1.eventUp)
        {
            return; // state did not change
        }

        //host
        if (Context.IsMainPlayer)
        {
            ModState.Current.InEvent = Game1.eventUp;
        }
        //client
        else
        {
            Helper.Multiplayer.SendMessage(Game1.eventUp, NetworkConstants.EventUpCommand, modIDs: new[] { ModManifest.UniqueID }, playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
        }

        ModState.Current.LastInEvent = Game1.eventUp;
    }

    private void ApplyShaftFix()
    {
        bool inSkull = ModState.Current.InSkull;

        //check if the player has jumped down a Skull Cavern Shaft
        if (config.DisableSkullShaftFix || !inSkull)
            return;

        if (Game1.player.currentLocation is not StardewValley.Locations.MineShaft mineshaft)
            return;

        int levelJump = mineshaft.mineLevel - ModState.Current.LastSkullLevel;

        if (levelJump > 1 && ModState.Current.HealthLock is not null)
        {
            Game1.player.health = Math.Max(1, Game1.player.health - levelJump * 3);
            ModState.Current.HealthLock = Game1.player.health;
        }

        ModState.Current.LastSkullLevel = mineshaft.mineLevel;
    }

    private void CheckAndFixSkullLogic()
    {
        //skip skull cavern fix logic if the main player has it disabled, or if is not multiplayer
        if (!Game1.IsMultiplayer)
            return;
        if (Context.IsMainPlayer && !config.FixSkullTime)
            return;

        bool inSkull = false;
        //check status to see if player is in Skull Cavern

        if (Game1.player.currentLocation is StardewValley.Locations.MineShaft mineshaftLocation
            && mineshaftLocation.getMineArea() > 120)
        {
            inSkull = true;
        }

        if (inSkull != ModState.Current.InSkull)
        {
            if (Context.IsMainPlayer)
            {
                ModState.Current.InSkull = inSkull;
            }
            else
            {
                Helper.Multiplayer.SendMessage(inSkull, NetworkConstants.InSkullCommand, modIDs: new[] { ModManifest.UniqueID }, playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
            }
        }

        if (!Context.IsMainPlayer)
            return;

        //apply the logic to remove 2000 from the time interval if everyone is in the skull cavern and this hasn't been done yet per this 10 minute day period
        if (Game1.gameTimeInterval > 6000 && ModState.InSkullAll)
        {
            if (!extraTimeAdded)
            {
                extraTimeAdded = true;
                Game1.gameTimeInterval -= 2000;
            }
        }

        if (Game1.gameTimeInterval < 10)
            extraTimeAdded = false;
    }

    private void VotePauseToggle()
    {
        bool votePause = ModState.Current.VotePause = !ModState.Current.VotePause;

        if (Context.IsMainPlayer)
        {
            if (!config.EnableVotePause)
                return;

            if (votePause)
            {
                string message = $"{Game1.player.Name} voted to pause the game. ({ModState.PositiveVotes}/{ModState.TotalFarmers})";
                Helper.Multiplayer.SendMessage(message, NetworkConstants.ChatInfoCommand, new[] { ModManifest.UniqueID });
                Game1.chatBox.addInfoMessage(message);
            }
            else
            {
                string message = $"{Game1.player.Name} voted to unpause the game. ({ModState.PositiveVotes}/{ModState.TotalFarmers})";
                Helper.Multiplayer.SendMessage(message, NetworkConstants.ChatInfoCommand, new[] { ModManifest.UniqueID });
                Game1.chatBox.addInfoMessage(message);
            }
        }
        else
        {
            Helper.Multiplayer.SendMessage(votePause, NetworkConstants.VotePauseCommand, modIDs: new[] { ModManifest.UniqueID }, playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
        }
    }

    private bool ShouldPause()
    {
        try
        {
            if (Context.IsMainPlayer)
            {
                //override
                if (config.EnablePauseOverride && ModState.Current.PauseOverride)
                    return true;

                //votes
                if (config.EnableVotePause && ModState.VotePauseAll)
                {
                    return true;
                }

                //events
                if (config.AnyCutscenePauses && ModState.InEventAny)
                {
                    return true;
                }

                //normal pause logic (terminates via false)
                return ModState.PauseAll;
            }
            else
            {
                return ModState.Current.PauseCommand;
            }
        }
        catch (Exception ex)
        {
            Monitor.Log("Reinitializing pauseCommand.", LogLevel.Debug);
            Monitor.Log(ex.Message, LogLevel.Debug);
            Monitor.Log(ex.StackTrace ?? string.Empty, LogLevel.Debug);

            ModState.Current.PauseCommand = false;
            return false;
        }


    }

    private static Vector2 UpdatePosition()
    {
        var position = new Vector2(Game1.uiViewport.Width - 300, 8f);
        if (Game1.isOutdoorMapSmallerThanViewport())
        {
            position = new Vector2(Math.Min(position.X, -Game1.uiViewport.X + Game1.currentLocation.map.Layers[0].LayerWidth * 64 - 300), 8f);
        }

        position.X += 23;
        position.Y += 55;

        Utility.makeSafe(ref position, 60, 60);

        return position;
    }
}
