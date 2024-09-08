using StardewModdingAPI;

using StardewValley;

namespace PauseInMultiplayer.Feature;

internal static class AnimalAndCharacterFeature
{
    public static void CheckCharactersAndAnimals(bool shouldPause)
    {
        if (!Context.IsMainPlayer || !shouldPause)
            return;

        //pause all Characters
        foreach (GameLocation location in Game1.locations)
        {
            //I don't know if the game stores null locations, and at this point I'm too afraid to ask
            if (location is null)
                continue;

            //pause all NPCs, doesn't seem to work for animals or monsters
            foreach (Character character in location.characters)
            {
                character.movementPause = 1;
            }

            //pause all farm animals
            if (location is Farm farm)
                foreach (FarmAnimal animal in farm.getAllFarmAnimals())
                    animal.pauseTimer = 100;
            else if (location is AnimalHouse animalHouse)
                foreach (FarmAnimal animal in animalHouse.animals.Values)
                    animal.pauseTimer = 100;
        }
    }
}
