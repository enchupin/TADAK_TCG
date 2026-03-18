using System;
using System.Collections.Generic;

[Serializable]
public class PlayerProfileSave
{
    public const int CurrentProfileVersion = 1;

    public int profileVersion = CurrentProfileVersion;
    public string playerId = string.Empty;
    public string updatedAtUtc = string.Empty;
    public List<CharacterDeckLibrarySave> characters = new();

    public CharacterDeckLibrarySave FindLibrary(int characterId)
    {
        if (characters == null)
        {
            return null;
        }

        for (int i = 0; i < characters.Count; i++)
        {
            CharacterDeckLibrarySave library = characters[i];
            if (library == null)
            {
                continue;
            }

            if (library.characterId == characterId)
            {
                return library;
            }
        }

        return null;
    }

    public void TouchUpdatedAtUtc()
    {
        updatedAtUtc = DateTime.UtcNow.ToString("o");
    }
}
