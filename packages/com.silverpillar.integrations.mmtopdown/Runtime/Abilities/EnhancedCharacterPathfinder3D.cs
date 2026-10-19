using MoreMountains.TopDownEngine;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    public class EnhancedCharacterPathfinder3D : CharacterPathfinder3D
    {
        /// <summary>
		/// Sets a new destination the character will pathfind to
		/// </summary>
		/// <param name="destinationTransform"></param>
		public void SetNewDestination(GameObject target)
        {
            if (target == null)
            {
                Target = null;
                return;
            }
            Target = target.transform;
            TargetPositionSet = true;
            ShouldMoveToTarget = true;
            DeterminePath(this.transform.position, Target.position, true);
        }
    }
}
