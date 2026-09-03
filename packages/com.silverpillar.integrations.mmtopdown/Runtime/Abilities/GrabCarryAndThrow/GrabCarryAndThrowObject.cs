using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using static SilverPillar.Integrations.MMTopDown.CharacterGrabCarryAndThrow;

namespace SilverPillar.Integrations.MMTopDown
{
    /// <summary>
    /// Add this component to an object and it'll become carryable by a Character
    /// with the appropriate ability (CharacterGrabCarryAndThrow).
    /// Uses Unity's 3D Rigidbody physics.
    /// </summary>
    public class GrabCarryAndThrowObject : MonoBehaviour
    {
        /// <summary>
        /// The possible ways this object can be carried:
        /// - Parent : the object gets parented to the Character, optionally under a specified transform
        /// - Follow : the object uses an additional MMFollowTarget component to smoothly follow the Character
        /// </summary>
        public enum CarryMethods
        {
            Parent,
            Follow
        }

        [Header("Carry")]

        [SerializeField, Tooltip("Minimum Grip strength to be carried")]
        private float m_MinimumGripStrengthToBeCarried = 0;

        /// the selected carry method
        [SerializeField, Tooltip("the selected carry method")]
        private CarryMethods m_CarryMethod = CarryMethods.Parent;

        /// the offset to apply when attaching the object to the character
        [SerializeField, Tooltip("the offset to apply when attaching the object to the character")]
        private Vector3 m_CarryOffset = Vector3.zero;

        /// the layer the object should be moved to while carried
        [SerializeField, Tooltip("the layer the object should be moved to while carried")]
        private string m_CarryLayerMask = "Projectiles";

        /// an ID that will get passed to the Character's animator when this object is carried
        [SerializeField, Tooltip("an ID that will get passed to the Character's animator when this object is carried")]
        private int m_CarryingAnimationID = 0;
        public int CarryingAnimationId { get { return m_CarryingAnimationID; } }

        /// whether this object is being carried this frame or not
        [SerializeField, Tooltip("whether this object is being carried this frame or not"), MMReadOnly()]
        private bool m_Carried = false;


        [Header("Throw")]

        /// the force at which this object should be thrown
        [SerializeField, Tooltip("the multiplier for thrown force.Throwing force is controlled by character ability.")]
        private float m_ThrowForceMultiplier = 1f;
        public float ThrowForceMultiplier { get { return m_ThrowForceMultiplier; } }

        /// the cooldown after which to reset the layer and scale of the object after a throw
        [SerializeField, Tooltip("the cooldown (in seconds) after which to reset the layer and scale of our object after a throw")]
        private float m_ThrowColliderCooldown = 0.2f;

        /// the recoil to apply to the throwing Character after a throw
        [SerializeField, Tooltip("the recoil to apply to the throwing Character after a throw")]
        private float m_ThrowRecoilMultiplier = 1f;
        public float ThrowRecoilMultiplier { get { return m_ThrowRecoilMultiplier; } }

        [Header("Resist and Escape")]

        [SerializeField]
        private bool m_CanResistAndEscape;

        [SerializeField, Min(0), MMCondition(nameof(m_CanResistAndEscape), true), 
            Tooltip("This variable reduces the character grab component grip strength by a certain amount per second. When the grip strength is zero or less, it releases itself.")]
        private float m_ResistAmountPerSecond;

        [SerializeField, MMCondition(nameof(m_CanResistAndEscape), true)]
        private float m_EscapeForceMultiplier = 1f;
        public float EscapeForceMultiplier { get { return m_EscapeForceMultiplier; } }

        [SerializeField, MMCondition(nameof(m_CanResistAndEscape), true)]
        private float m_EscapeColliderCooldown = 0.2f;

        [SerializeField, MMCondition(nameof(m_CanResistAndEscape), true)]
        private float m_EscapeRecoilMultiplier = 1f;
        public float EscapeRecoilMultiplier { get { return m_EscapeRecoilMultiplier; } }

        [Header("Events")]

        /// a UnityEvent triggered when the object gets grabbed
        [SerializeField, Tooltip("a UnityEvent triggered when the object gets grabbed")]
        private UnityEvent m_OnGrabbed;

        /// a UnityEvent triggered when the object gets thrown
        [SerializeField, Tooltip("a UnityEvent triggered when the object gets thrown")]
        private UnityEvent m_OnRelease;

        [SerializeField, Tooltip("a UnityEvent triggered when the object is resisting")]
        private UnityEvent m_OnResisting;

        [SerializeField, Tooltip("a UnityEvent triggered when the object escapes")]
        private UnityEvent m_OnEscaped;

        private TopDownController m_TopDownController = null;
        private Rigidbody m_Rigidbody;
        private Vector3 m_ForceVector;
        private WaitForSeconds m_EscapeColliderCooldownWaitForSeconds;
        private WaitForSeconds m_ThrowColliderCooldownWaitForSeconds;
        private WaitForSeconds m_ResistWaitForSeconds;
        private int m_OriginalLayer;
        private Vector3 m_OriginalScale;
        private MMFollowTarget m_FollowTarget;
        private bool m_WasKinematic;
        private bool m_TopDownControllerWasEnabled;
        private CharacterGrabCarryAndThrow m_Carrier;
        private Coroutine m_ResistCoroutine;

        /// <summary>
        /// On Awake we initialize our object.
        /// </summary>
        protected virtual void Awake()
        {
            Initialization();
        }

        /// <summary>
        /// Grabs and stores components.
        /// </summary>
        protected virtual void Initialization()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            TryGetComponent(out m_TopDownController);

            m_ThrowColliderCooldownWaitForSeconds = new WaitForSeconds(m_ThrowColliderCooldown);
            m_EscapeColliderCooldownWaitForSeconds = new WaitForSeconds(m_EscapeColliderCooldown); 
            m_ResistWaitForSeconds = new WaitForSeconds(1);

            m_FollowTarget = GetComponent<MMFollowTarget>();
        }

        /// <summary>
        /// Triggered when this object starts being carried.
        /// Attaches it to the grabber and sets its state.
        /// </summary>
        public virtual bool Grab(CharacterGrabCarryAndThrow carrier)
        {
            if (m_MinimumGripStrengthToBeCarried > carrier.CurrentGripStrength)
            {
                return false;
            }

            m_Carrier?.RecoverGripStrength();
            m_Carrier = carrier;
            m_OriginalScale = transform.localScale;
            m_OriginalLayer = gameObject.layer;

            switch (m_CarryMethod)
            {
                case CarryMethods.Parent:

                    transform.SetParent(carrier.CarryParent.transform);
                    transform.localPosition = m_CarryOffset;

                    break;

                case CarryMethods.Follow:

                    if (m_FollowTarget != null)
                    {
                        m_FollowTarget.Target = carrier.CarryParent.transform;
                        m_FollowTarget.Initialization();
                        m_FollowTarget.StartFollowing();
                    }

                    break;
            }

            int carryLayer = LayerMask.NameToLayer(m_CarryLayerMask);

            if (carryLayer >= 0)
            {
                gameObject.layer = carryLayer;
            }

            if (m_Rigidbody != null)
            {
                if (!m_Rigidbody.isKinematic)
                {
                    // Prevent residual movement while held
                    m_Rigidbody.linearVelocity = Vector3.zero;
                    m_Rigidbody.angularVelocity = Vector3.zero;
                }

                m_WasKinematic = m_Rigidbody.isKinematic;
                // Equivalent to RigidbodyType2D.Kinematic
                m_Rigidbody.isKinematic = true;
            }
            if (m_TopDownController != null)
            {
                m_TopDownControllerWasEnabled = m_TopDownController.enabled;
                m_TopDownController.enabled = false;
            }

            m_OnGrabbed?.Invoke();

            m_Carried = true;

            if (m_CanResistAndEscape)
            {
                if (m_ResistCoroutine != null)
                {
                    StopCoroutine(m_ResistCoroutine);
                }
                m_ResistCoroutine = StartCoroutine(Resist());
            }

            return true;
        }

        /// <summary>
        /// Triggered when this object gets thrown.
        /// </summary>
        /// <param name="direction">
        /// Positive values throw in the normal X direction.
        /// Negative values reverse the X direction.
        /// </param>
        /// <param name="forceMagnitude">Multiplier applied to the throw force.</param>
        public virtual void Release()
        {
            StartCoroutine(ThrowResetCollisions());

            switch (m_CarryMethod)
            {
                case CarryMethods.Parent:

                    transform.SetParent(null);

                    break;

                case CarryMethods.Follow:

                    if (m_FollowTarget != null)
                    {
                        m_FollowTarget.Target = null;
                        m_FollowTarget.StopFollowing();
                    }

                    break;
            }

            if (m_TopDownController != null)
            {
                m_TopDownController.enabled = m_TopDownControllerWasEnabled;
            }
            if (m_Rigidbody != null)
            {
                m_Rigidbody.isKinematic = m_WasKinematic;
            }

            m_OnRelease?.Invoke();

            m_Carried = false;
            m_ResistCoroutine = null;
            m_Carrier?.RecoverGripStrength();
            m_Carrier = null;
        }

        public void Escape()
        {
            if (m_Carrier == null)
            {
                return;
            }

            StartCoroutine(EscapeResetCollisions());

            EscapeImplementation();
        }

        private void EscapeImmediately()
        {

            if (m_Carrier == null)
            {
                return;
            }

            gameObject.layer = m_OriginalLayer;
            transform.localScale = m_OriginalScale;

            EscapeImplementation();

            if (m_ResistCoroutine != null)
            {
                StopCoroutine(m_ResistCoroutine);
            }
        }

        private void EscapeImplementation()
        {
            m_Carrier.RecoverGripStrength();

            switch (m_CarryMethod)
            {
                case CarryMethods.Parent:

                    transform.SetParent(null);

                    break;

                case CarryMethods.Follow:

                    if (m_FollowTarget != null)
                    {
                        m_FollowTarget.Target = null;
                        m_FollowTarget.StopFollowing();
                    }

                    break;
            }

            if (m_TopDownController != null)
            {
                m_TopDownController.enabled = m_TopDownControllerWasEnabled;
            }
            if (m_Rigidbody != null)
            {
                m_Rigidbody.isKinematic = m_WasKinematic;
            }

            m_Carried = false;

            m_OnEscaped?.Invoke();

            m_Carrier.Escape();
            m_Carrier = null;
        }

        private void OnDestroy()
        {
            EscapeImmediately();
        }

        private void OnDisable()
        {
            EscapeImmediately();
        }

        /// <summary>
        /// Resets the object's collision layer and scale after throwing.
        /// </summary>
        protected virtual IEnumerator ThrowResetCollisions()
        {
            yield return m_ThrowColliderCooldownWaitForSeconds;

            gameObject.layer = m_OriginalLayer;
            transform.localScale = m_OriginalScale;
        }

        protected virtual IEnumerator EscapeResetCollisions()
        {
            yield return m_EscapeColliderCooldownWaitForSeconds;

            gameObject.layer = m_OriginalLayer;
            transform.localScale = m_OriginalScale;
        }

        protected virtual IEnumerator Resist()
        {
            while (m_Carrier != null && m_Carrier.CurrentGripStrength > 0)
            {
                yield return m_ResistWaitForSeconds;

                m_Carrier.ResistGripStrength(m_ResistAmountPerSecond);
                m_OnResisting?.Invoke();
            }

            Escape();
            m_ResistCoroutine = null;
        }
    }
}