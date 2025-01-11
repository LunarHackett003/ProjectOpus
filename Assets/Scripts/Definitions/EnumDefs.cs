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
    public enum ScoreAwardingBehaviour
    {
        /// <summary>
        /// Damaging this entity does NOT award score.
        /// </summary>
        none = 0,
        /// <summary>
        /// Damaging this entity awards combat score to the damager - think a player shooting somebody. Things that heal should NOT use ReceiveDamage.
        /// </summary>
        sourceCombat = 1,
        /// <summary>
        /// Damaging this entity awards support score to the damager - shooting an enemy device to disable it 
        /// </summary>
        sourceSupport = 2,
        /// <summary>
        /// In some cases, shooting something might award the owner combat score. Not sure what any of these cases ARE, but it wouldn't hurt to have this anyway.
        /// </summary>
        ownerCombat = 3,
        /// <summary>
        /// Objects such as shields or barricades might want to award the owner with support score when using them.
        /// </summary>
        ownerSupport = 4,
    }
    /// <summary>
    /// How an attack behaves when pressed
    /// </summary>
    public enum MeleeBehaviour
    {
        /// <summary>
        /// No melee attack behaviour
        /// </summary>
        none = 0,
        /// <summary>
        /// Attacks when pressed
        /// </summary>
        pressAttack = 1,
        /// <summary>
        /// Attacks when pressed, loops while held
        /// </summary>
        holdAttack = 2,
        /// <summary>
        /// Charges up an attack over time
        /// </summary>
        chargeAttack = 3
    }
    /// <summary>
    /// The player's current move state - how are they moving?
    /// </summary>
    public enum MovementState
    {
        /// <summary>
        /// The player has no move state. Not sure how we should ever be in this position.
        /// </summary>
        none = 0,
        /// <summary>
        /// The player is on the ground & standing still or moving normally.
        /// </summary>
        walking = 1,
        /// <summary>
        /// The player is sliding along the ground.
        /// </summary>
        sliding = 2,
        /// <summary>
        /// The player is not on the ground.
        /// </summary>
        airborne = 3
    }

}
