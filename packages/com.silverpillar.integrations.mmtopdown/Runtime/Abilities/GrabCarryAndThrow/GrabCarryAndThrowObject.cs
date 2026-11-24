using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

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

        public bool CanBeStolen => m_CanBeStolen;
        public CharacterGrabCarryAndThrow Carrier => m_Carrier;

        /// the selected carry method
        [SerializeField, Tooltip("the selected carry method")]
        private CarryMethods m_CarryMethod = CarryMethods.Parent;

        /// the offset to apply when attaching the object to the character
        [SerializeField, Tooltip("the offset to apply when attaching the object to the character")]
        private Vector3 m_CarryOffset = Vector3.zero;

        /// the layer the object should be moved to while carried
        [SerializeField, Tooltip("the layer the object should be moved to while carried")]
        private LayerMask m_CarryLayerMask;

        /// an ID that will get passed to the Character's animator when this object is carried
        [SerializeField, Tooltip("an ID that will get passed to the Character's animator when this object is carried")]
        private int m_CarryingAnimationID = 0;
        public int CarryingAnimationId { get { return m_CarryingAnimationID; } }

        /// whether this object is being carried this frame or not
        [SerializeField, Tooltip("whether this object is being carried this frame or not"), MMReadOnly()]
        private bool m_Carried = false;
        public bool IsBeingCarried
        {
            get { return m_Carried; }
        }

        [Header("Being Stolen")]

        [SerializeField, Tooltip("Whether another character can take this object while it is carried.")]
        private bool m_CanBeStolen = true;


        [Header("Throw")]

        /// the force at which this object should be thrown
        [SerializeField, Tooltip("the multiplier for thrown force.Throwing force is controlled by character ability.")]
        private float m_ThrowForceMultiplier = 1f;
        public float ThrowForceMultiplier { get { return m_ThrowForceMultiplier; } }

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
        private float m_EscapeRecoilMultiplier = 1f;
        public float EscapeRecoilMultiplier { get { return m_EscapeRecoilMultiplier; } }

        private enum DeactivationProtocol
        {
            EscapeAndUnparentOnEnable,
            EscapeButDontUnparentOnEnable,
            DontEscape
        }

        [Header("Side Cases")]

        [SerializeField, Tooltip("What happens if the gameobject is disabled while being carried. " +
            "It can't be unparented on disabled because unity locks the gameobject's hierarchy on disable." +
            "The collision layer will always be reset on disable and the local scale will be reset when unparented.")]
        private DeactivationProtocol m_DeactivationProtocol;

        [Header("Events")]

        /// a UnityEvent triggered when the object gets grabbed
        [SerializeField, Tooltip("a UnityEvent triggered when the object gets grabbed")]
        private UnityEvent<GameObject> m_OnGrabbed;

        /// a UnityEvent triggered when the object gets thrown
        [SerializeField, Tooltip("a UnityEvent triggered when the object gets thrown")]
        private UnityEvent<GameObject> m_OnRelease;

        [SerializeField, Tooltip("a UnityEvent triggered when the object is resisting")]
        private UnityEvent<GameObject> m_OnResisting;

        [SerializeField, Tooltip("a UnityEvent triggered when the object escapes")]
        private UnityEvent<GameObject> m_OnEscaped;

        [SerializeField, ShowIf(nameof(m_CanBeStolen)), Tooltip("Invoked with the new carrier when this object is stolen from another character.")]
        private UnityEvent<GameObject> m_OnStolen;

        private TopDownController m_TopDownController = null;
        private Rigidbody m_Rigidbody;
        private WaitForSeconds m_ResistWaitForSeconds;
        private int m_OriginalLayer;
        private MMFollowTarget m_FollowTarget;
        private bool m_WasKinematic;
        private bool m_TopDownControllerWasEnabled;
        private CharacterGrabCarryAndThrow m_Carrier;
        private Coroutine m_ResistCoroutine;
        private bool m_UnparentOnEnable = false;

        /// <summary>
        /// On Awake we initialize our object.
        /// </summary>
        protected virtual void Awake()
        {
            Initialization();
        }

        private void OnEnable()
        {
            if (m_UnparentOnEnable)
            {
                transform.SetParent(null);
                m_UnparentOnEnable = false;
            }
        }

        /// <summary>
        /// Grabs and stores components.
        /// </summary>
        protected virtual void Initialization()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            TryGetComponent(out m_TopDownController);

            m_ResistWaitForSeconds = new WaitForSeconds(1);

            m_FollowTarget = GetComponent<MMFollowTarget>();
        }

        /// <summary>
        /// Triggered when this object starts being carried.
        /// Attaches it to the grabber and sets its state.
        /// </summary>
        public virtual bool Grab(CharacterGrabCarryAndThrow carrier)
        {
            if (carrier == null || !isActiveAndEnabled ||
                m_MinimumGripStrengthToBeCarried > carrier.CurrentGripStrength)
            {
                return false;
            }

            if (m_Carrier == carrier)
            {
                return m_Carried;
            }

            bool wasStolen = m_Carrier != null;

            if (wasStolen)
            {
                if (!m_CanBeStolen)
                {
                    return false;
                }

                CharacterGrabCarryAndThrow previousCarrier = m_Carrier;
                StopResisting();
                previousCarrier.OnCarriedObjectStolen(this);
                // A listener may have changed ownership during the notification.
                if (m_Carrier != previousCarrier)
                {
                    return false;
                }
                previousCarrier.RecoverGripStrength();
                // Keep the original physics/layer snapshots; do not restore them
                // during an ownership transfer.
            }
            else
            {
                m_OriginalLayer = gameObject.layer;
                if (m_Rigidbody != null)
                {
                    m_WasKinematic = m_Rigidbody.isKinematic;
                }
                if (m_TopDownController != null)
                {
                    m_TopDownControllerWasEnabled = m_TopDownController.enabled;
                }
            }

            m_Carrier = carrier;
            m_UnparentOnEnable = false;

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

            gameObject.layer = m_CarryLayerMask;

            if (m_Rigidbody != null)
            {
                if (!m_Rigidbody.isKinematic)
                {
                    // Prevent residual movement while held
                    m_Rigidbody.linearVelocity = Vector3.zero;
                    m_Rigidbody.angularVelocity = Vector3.zero;
                }

                // Equivalent to RigidbodyType2D.Kinematic
                m_Rigidbody.isKinematic = true;
            }
            if (m_TopDownController != null)
            {
                m_TopDownController.enabled = false;
            }

            m_Carried = true;

            if (m_CanResistAndEscape)
            {
                if (m_ResistCoroutine != null)
                {
                    StopCoroutine(m_ResistCoroutine);
                }
                m_ResistCoroutine = StartCoroutine(Resist());
            }

            m_OnGrabbed?.Invoke(carrier.gameObject);

            if (!m_Carried || m_Carrier != carrier)
            {
                return false;
            }

            if (wasStolen)
            {
                m_OnStolen?.Invoke(carrier.gameObject);
            }

            return m_Carried && m_Carrier == carrier;
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
            if (!m_Carried)
            {
                return;
            }

            StopResisting();
            gameObject.layer = m_OriginalLayer;

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

            CharacterGrabCarryAndThrow releasedBy = m_Carrier;
            m_Carried = false;
            m_ResistCoroutine = null;
            m_Carrier = null;
            releasedBy?.RecoverGripStrength();
            m_OnRelease?.Invoke(releasedBy != null ? releasedBy.gameObject : null);
        }

        public void Escape()
        {
            if (m_Carrier == null)
            {
                return;
            }

            gameObject.layer = m_OriginalLayer;

            EscapeImplementation(false);
        }

        private void EscapeImmediately()
        {

            if (m_Carrier == null)
            {
                return;
            }

            gameObject.layer = m_OriginalLayer;

            EscapeImplementation(true);

            StopResisting();
        }


        private void EscapeImplementation(bool calledOnDisable)
        {
            StopResisting();
            m_Carrier.RecoverGripStrength();

            switch (m_CarryMethod)
            {
                case CarryMethods.Parent:

                    if (calledOnDisable)
                    {
                        switch (m_DeactivationProtocol)
                        {
                            case DeactivationProtocol.EscapeAndUnparentOnEnable:
                                m_UnparentOnEnable = true;
                                break;
                            case DeactivationProtocol.EscapeButDontUnparentOnEnable:
                                break;
                            case DeactivationProtocol.DontEscape:
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        transform.SetParent(null);
                    }

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

            m_OnEscaped?.Invoke(m_Carrier.gameObject);

            m_Carrier.Escape();
            m_Carrier = null;
        }

        private void OnDestroy()
        {
            EscapeImmediately();
        }

        private void OnDisable()
        {
            switch (m_DeactivationProtocol)
            {
                case DeactivationProtocol.EscapeAndUnparentOnEnable:
                case DeactivationProtocol.EscapeButDontUnparentOnEnable:
                    EscapeImmediately();
                    break;
                case DeactivationProtocol.DontEscape:
                    break;
                default:
                    break;
            }

        }

        private void StopResisting()
        {
            if (m_ResistCoroutine != null)
            {
                StopCoroutine(m_ResistCoroutine);
                m_ResistCoroutine = null;
            }
        }

        protected virtual IEnumerator Resist()
        {
            while (m_Carrier != null && m_Carrier.CurrentGripStrength > 0)
            {
                yield return m_ResistWaitForSeconds;

                CharacterGrabCarryAndThrow carrier = m_Carrier;
                if (carrier == null) break;
                carrier.ResistGripStrength(m_ResistAmountPerSecond);
                if (m_Carrier == carrier) m_OnResisting?.Invoke(carrier.gameObject);
            }

            Escape();
            m_ResistCoroutine = null;
        }
    }
}