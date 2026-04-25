using System.Collections.Generic;

public static class CharacterIdentitySkillRegistry
{
    public static Dictionary<Character, CharacterIdentitySkill> Create()
    {
        return new Dictionary<Character, CharacterIdentitySkill>
        {
            { Character.Isla, new IslaIdentitySkill() },
            { Character.Jack, new JackIdentitySkill() },
            { Character.Cream, new CreamIdentitySkill() },
            { Character.Ignia, new IgniaIdentitySkill() },
            { Character.Vanessa, new VanessaIdentitySkill() },
            { Character.Merel, new MerelIdentitySkill() },
            { Character.Mio, new MioIdentitySkill() },
            { Character.Polar, new PolarIdentitySkill() },
            { Character.Rune, new RuneIdentitySkill() },
            { Character.Khan, new KhanIdentitySkill() }
        };
    }
}
