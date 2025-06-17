namespace CardboardBox.Anime.Bot.Dnd;

/// <summary>
/// The various types of damage that can be inflicted in a game.
/// </summary>
public enum DamageType
{
    /// <summary>
    /// Damage type is unknown or not specified.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Corrosive damage, like from a black pudding or a flask of acid.
    /// </summary>
    Acid = 1,
    /// <summary>
    /// Damage from blunt force, like a mace or falling.
    /// </summary>
    Bludgeoning = 2,
    /// <summary>
    /// Damage from extreme cold, like an Ice Devil's breath.
    /// </summary>
    Cold = 3,
    /// <summary>
    /// Damage from flames, like a dragon's breath or a fireball spell. 
    /// </summary>
    Fire = 4,
    /// <summary>
    /// Pure magical energy, often from spells like magic missile.
    /// </summary>
    Force = 5,
    /// <summary>
    /// Damage from electricity, like a lightning bolt spell.
    /// </summary>
    Lightning = 6,
    /// <summary>
    /// Damage that withers and decays, often associated with undead.
    /// </summary>
    Necrotic = 7,
    /// <summary>
    /// Damage from sharp, pointed objects, like spears or arrows.
    /// </summary>
    Piercing = 8,
    /// <summary>
    /// Damage from toxins, like a green dragon's breath or a poison dart. 
    /// </summary>
    Poison = 9,
    /// <summary>
    /// Damage from mental attacks, like a mind flayer's psionic blast. 
    /// </summary>
    Psychic = 10,
    /// <summary>
    /// Damage from bright, searing light, like a sunbeam spell. 
    /// </summary>
    Radiant = 11,
    /// <summary>
    /// Damage from sharp edges, like swords or claws.
    /// </summary>
    Slashing = 12,
    /// <summary>
    /// Damage from concussive sound, like a thunder-wave spell.
    /// </summary>
    Thunder = 13,
}
