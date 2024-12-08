using UnityEngine;

namespace Opus
{
    /// <summary>
    /// <b>HITBOXES</b><br></br>
    /// The damage this hitbox receives.<para></para>
    /// <b>WEAPONS</b><br></br>
    /// The damage dealt by this weapon
    /// </summary>
    public enum DamageType
    {
        /// <summary>
        /// Typically the body of a humanoid character
        /// </summary>
        HumanRegular = 0,
        /// <summary>
        /// Typically the head of a humanoid character
        /// </summary>
        HumanCritical = 1,
        /// <summary>
        /// Typically the body of a mech or robot
        /// </summary>
        MechRegular = 2,
        /// <summary>
        /// Typically the cockpit or the power cell on a mech or robot.
        /// </summary>
        MechCritical = 4
    }
    /// <summary>
    /// The state the player is currently in.
    /// </summary>
    public enum PlayerState
    {
        /// <summary>
        /// We should, in theory, never be in a "none" state once the game has loaded.
        /// </summary>
        none = 0,
        /// <summary>
        /// The player is not in a vehicle of any kind.
        /// </summary>
        onFoot = 1,
        /// <summary>
        /// The player is in a vehicle
        /// </summary>
        mounted = 2,
    }
}
