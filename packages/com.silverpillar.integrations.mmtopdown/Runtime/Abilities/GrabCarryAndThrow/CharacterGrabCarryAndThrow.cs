using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using SilverPillar.Core;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Integrations.MMTopDown
{
    public enum ConstraintVector
    {
        DontConstraintVector,
        ConstrainToXZ,
        ConstraintToXY,
        ConstraintToYZ
    }

    public struct VectorTools
    {
        public static Vector3 ApplyConstraint(Vector3 vector, ConstraintVector constraint)
        {
            switch (constraint)
            {
                case ConstraintVector.DontConstraintVector:
                    return vector;
                case ConstraintVector.ConstrainToXZ:
                    return new Vector3(vector.x, 0, vector.z);
                case ConstraintVector.ConstraintToXY:
                    return new Vector3(vector.x, vector.y, 0);
                case ConstraintVector.ConstraintToYZ:
                    return new Vector3(0, vector.y, vector.z);
                default:
                    break;
            }

            return vector;
        }
    }

    /// <summary>
    /// This class lets you grab, carry and throw objects with a GrabCarryAndThrowObject component.
    /// 
    /// Animation parameters :
    /// - Grabbing, boolean, triggered when an object is grabbed
    /// - Carrying : boolean, true if an object is being carried, false otherwise
    /// - CarryingID : int, set to whatever value is set on the carried object 
    /// - Throwing, boolean, triggered when an object gets thrown
    /// </summary>
    public class CharacterGrabCarryAndThrow : CharacterAbility
    {
        
        public override string HelpBoxText()
        {
            return "This class lets you grab, carry and throw objects with a GrabCarryAndThrowObject component." +
                                                      " In the Grab section you can define how you want the raycast that detects grabbable objects to work, " +
                                                      "in the Carry section you can set an optional child transform to attach carried objects to, and in the Throw section you can define how strong you want " +
                                                      "this Character's throw to be, and how much recoil it should get.";
        }

        [Header("Grab")]
        [MMReadOnly]
        [SerializeField, Tooltip("whether or not this Character is grabbing something right now")]
        private bool m_Grabbing = false;
        [SerializeField, Tooltip("Some objects require a minimum grip strength to be carried. If the object can resist being carried, it will weaken the grip strength as time goes on.")]
        private float m_MaxGripStrength;
        public float CurrentGripStrength { get; private set; }

        public void ResistGripStrength(float byHowMuch)
        {
            CurrentGripStrength -= byHowMuch;
            m_OnGrabbedObjectResisting?.Invoke();
        }
        public void RecoverGripStrength()
        {
            CurrentGripStrength = m_MaxGripStrength;
        }

        [Header("Carry")]

        /// a Transform used to attach carried objects to
        [SerializeField, Tooltip("a Transform used to attach carried objects to")]
        private Transform m_CarryParent;
        public Transform CarryParent { get { return m_CarryParent; } }
        /// whether or not this Character is carrying an object this frame
        [MMReadOnly]
        [SerializeField, Tooltip("whether or not this Character is carrying an object this frame")]
        private bool m_Carrying = false;
        /// the ID of the object being carried
        [MMReadOnly]
        [SerializeField, Tooltip("the ID of the object being carried")]
        private int m_CarryingID = -1;
        /// a reference to the object being carried
        [MMReadOnly]
        [SerializeField, Tooltip("a reference to the object being carried")]
        private GrabCarryAndThrowObject m_CarriedObject = null;

        [MMReadOnly]
        [SerializeField, Tooltip("a reference to the object being carried")]
        private GrabCarryAndThrowObject m_PossibleCarriedObject = null;

        [Header("Throw")]

        [SerializeField, Tooltip("the transform to reference for calculating throw direction. Vector from this to other object will be throw direction. If null will be self.")]
        private Transform m_ThrowReferenceTransform;

        [SerializeField]
        private ConstraintVector m_ConstraintThrowVector;

        /// the force to apply when throwing
        [SerializeField, Tooltip("the force to apply when throwing")]
        private float m_ThrowForce = 1f;
        [SerializeField]
        private ForceApplier m_ThrowForceApplier;

        /// whether or not this Character is throwing something this frame
        [MMReadOnly]
        [SerializeField, Tooltip("whether or not this Character is throwing something this frame")]
        private bool m_Throwing = false;


        [Header("Throw Recoil")]
        /// a modifier to apply to the recoil set on the object
        [SerializeField, Tooltip("a modifier to apply to the recoil set on the object")]
        private float m_ThrowRecoilForce = 1f;
        [SerializeField]
        private ForceApplier m_ThrowRecoilForceApplier;


        [Header("Escaping Force")]

        [SerializeField]
        private float m_EscapeForce = 1f;

        [SerializeField]
        private ForceApplier m_EscapeForceApplier;

        [SerializeField, Tooltip("If you want to constraint the escape vector from carrier throw transform to self.")]
        private ConstraintVector m_ConstraintEscapeVector;


        [Header("Escaping Recoil")]
        [SerializeField]
        private float m_EscapeRecoil = 0f;
        [SerializeField]
        private ForceApplier m_EscapeRecoilApplier;


        [Header("Grabbing Events")]
        [SerializeField]
        private bool m_TriggerOnGrabbedTriggeredEvenIfAbilityIsNotAuthorized;
        [SerializeField, Tooltip("a UnityEvent triggered when the grabbed input gets triggered")]
        private UnityEvent m_OnGrabbedTriggered;
        /// a UnityEvent triggered when the object gets grabbed
        [SerializeField, Tooltip("a UnityEvent triggered when the object gets grabbed")]
        private UnityEvent m_OnGrabbedSuccessful;
        [SerializeField, Tooltip("a UnityEvent triggered when the object couldn't get grabbed because self didn't have enough grip strength")]
        private UnityEvent m_OnGrabbedUnsuccessful;
        /// a UnityEvent triggered when the object gets grabbed
        [SerializeField, Tooltip("a UnityEvent triggered when triggering grab input and not grabbing anything")]
        private UnityEvent m_OnGrabbedMiss;


        [Header("Throwing Events")]
        [SerializeField]
        private bool m_TriggerOnThrowTriggeredEvenIfAbilityIsNotAuthorized;
        /// a UnityEvent triggered when the object gets thrown
        [SerializeField, Tooltip("a UnityEvent triggered when the throw input is triggered")]
        private UnityEvent m_OnThrowTriggered;
        [SerializeField, Tooltip("a UnityEvent triggered when the object gets thrown")]
        private UnityEvent m_OnThrowSuccess;
        [SerializeField, Tooltip("a UnityEvent triggered when the throw input gets triggered but you don't have anything to throw")]
        private UnityEvent m_OnThrowNothing;


        [Header("Resisting Events")]
        [SerializeField, Tooltip("a UnityEvent triggered when the grabbed item is resisting")]
        private UnityEvent m_OnGrabbedObjectResisting;
        [SerializeField, Tooltip("a UnityEvent triggered when the grabbed item escaped.")]
        private UnityEvent m_OnGrabbedObjectEscaped;

        private Vector2 m_RecoilVector;


        private GameInputManager m_GameInputManager;

        // animation parameters
        private const string m_GrabbingAnimationParameterName = "Grabbing";
        private int m_GrabbingAnimationParameter;
        private const string m_CarryingAnimationParameterName = "Carrying";
        private int m_CarryingAnimationParameter;
        private const string m_CarryingIDAnimationParameterName = "CarryingID";
        private int m_CarryingIDAnimationParameter;
        private const string m_ThrowingAnimationParameterName = "Throwing";
        private int m_ThrowingAnimationParameter;
        private Vector3 m_ActualRaycastDirection;

        /// <summary>
        /// On init we set our CarryParent to the character transform if null
        /// </summary>
        protected override void Initialization()
        {
            base.Initialization();

            if (_character.CharacterType == Character.CharacterTypes.Player)
            {
                m_GameInputManager = InputManager.Instance as GameInputManager;

                if (m_GameInputManager == null)
                {
                    Debug.LogError($"{nameof(CharacterGrabCarryAndThrow)} component in {gameObject.name} needs the input manager to be of class {nameof(GameInputManager)}");
                }
            }

            if (m_CarryParent == null)
            {
                m_CarryParent = this.transform;
            }

            if (m_ThrowReferenceTransform == null)
            {
                m_ThrowReferenceTransform = transform;
            }

            m_ThrowRecoilForceApplier.SetGameObject(gameObject);
            m_EscapeRecoilApplier.SetGameObject(gameObject);

            RecoverGripStrength();
        }

        private void OnCollisionEnter(Collision collision)
        {
            GrabCarryAndThrowObject possibleCarriedObject;

            if (collision.gameObject != _character.gameObject && collision.gameObject.TryGetComponent(out possibleCarriedObject))
            {
                m_PossibleCarriedObject = possibleCarriedObject;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            GrabCarryAndThrowObject possibleCarriedObject;

            if (other.gameObject != _character.gameObject && other.gameObject.TryGetComponent(out possibleCarriedObject))
            {
                m_PossibleCarriedObject = possibleCarriedObject;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            GrabCarryAndThrowObject possibleCarriedObject;

            if (collision.gameObject != _character.gameObject && collision.gameObject.TryGetComponent(out possibleCarriedObject))
            {
                m_PossibleCarriedObject = possibleCarriedObject;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            GrabCarryAndThrowObject possibleCarriedObject;

            if (collision.gameObject != _character.gameObject && collision.gameObject.TryGetComponent(out possibleCarriedObject))
            {
                m_PossibleCarriedObject = possibleCarriedObject;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (m_PossibleCarriedObject != null && m_PossibleCarriedObject.gameObject == other.gameObject)
            {
                m_PossibleCarriedObject = null;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (m_PossibleCarriedObject != null && m_PossibleCarriedObject.gameObject == collision.gameObject)
            {
                m_PossibleCarriedObject = null;
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (m_PossibleCarriedObject != null && m_PossibleCarriedObject.gameObject == collision.gameObject)
            {
                m_PossibleCarriedObject = null;
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (m_PossibleCarriedObject != null && m_PossibleCarriedObject.gameObject == collision.gameObject)
            {
                m_PossibleCarriedObject = null;
            }
        }

        /// <summary>
        /// Looks for throw and grab inputs
        /// </summary>
        protected override void HandleInput()
        {
            if (m_GameInputManager.ThrowButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
            {
                if (m_Carrying)
                {
                    TryThrow();
                }
            }
            if (m_GameInputManager.GrabButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
            {
                if (!m_Carrying)
                {
                    GrabAttempt();
                }
            }
        }

        public void TryGrab(GameObject other)
        {
            GrabCarryAndThrowObject possibleCarriedObject;
            if (other != _character.gameObject && other.TryGetComponent(out possibleCarriedObject))
            {
                m_PossibleCarriedObject = possibleCarriedObject;
                GrabAttempt();
            }
        }

        /// <summary>
        /// Tries to grab by casting a raycast
        /// </summary>
        protected virtual void GrabAttempt()
        {
            if (!AbilityAuthorized
                || ((_condition.CurrentState != CharacterStates.CharacterConditions.Normal) && (_condition.CurrentState != CharacterStates.CharacterConditions.ControlledMovement)))
            {
                if (m_TriggerOnGrabbedTriggeredEvenIfAbilityIsNotAuthorized)
                {
                    m_OnGrabbedTriggered?.Invoke();
                }
                return;
            }


            m_OnGrabbedTriggered?.Invoke();

            m_CarriedObject = m_PossibleCarriedObject;

            if (m_CarriedObject != null)
            {
                Grab();
            }
            else
            {
                m_OnGrabbedMiss?.Invoke();
            }
        }

        /// <summary>
        /// Sets the ability in carrying mode
        /// </summary>
        protected virtual void Grab()
        {
            if (!AbilityAuthorized)
            {
                return;
            }

            m_ThrowForceApplier.SetGameObject(m_CarriedObject.gameObject);
            m_EscapeForceApplier.SetGameObject(m_CarriedObject.gameObject);

            if (m_CarriedObject.Grab(this))
            {
                m_OnGrabbedSuccessful?.Invoke();
                m_Carrying = true;
                m_CarryingID = m_CarriedObject.CarryingAnimationId;
                m_Grabbing = true;
                PlayAbilityStartFeedbacks();
            }
            else
            {
                m_OnGrabbedUnsuccessful?.Invoke();
            }
        }

        /// <summary>
        /// Throws the carried object
        /// </summary>
        public virtual void TryThrow()
        {
            if (!AbilityAuthorized)
            {
                if (m_TriggerOnThrowTriggeredEvenIfAbilityIsNotAuthorized)
                {
                    m_OnThrowTriggered?.Invoke();
                }
                return;
            }


            m_OnThrowTriggered?.Invoke();

            if (m_CarriedObject == null)
            {
                m_OnThrowNothing?.Invoke();
                return;
            }

            Vector3 direction = VectorTools.ApplyConstraint(m_CarriedObject.transform.position - m_ThrowReferenceTransform.position, m_ConstraintThrowVector);

            m_CarriedObject.Release();
            m_ThrowForceApplier.ApplyForce(direction.normalized * m_ThrowForce * m_CarriedObject.ThrowForceMultiplier);

            // apply recoil
            if (m_ThrowRecoilForce != 0f)
            {
                m_RecoilVector = direction.normalized;
                m_RecoilVector *= m_ThrowRecoilForce * m_CarriedObject.ThrowRecoilMultiplier;
                m_ThrowRecoilForceApplier.ApplyForce(-m_RecoilVector);
            }

            StopFeedbacks();
            m_CarriedObject = null;
            m_CarryingID = -1;
            m_Carrying = false;
            m_Throwing = true;
            m_OnThrowSuccess?.Invoke();
        }

        public void Escape()
        {
            if (m_CarriedObject == null)
            {
                return;
            }

            Vector3 direction = VectorTools.ApplyConstraint(m_CarriedObject.transform.position - m_ThrowReferenceTransform.position, m_ConstraintEscapeVector);

            Vector3 force = direction.normalized * m_EscapeForce * m_CarriedObject.EscapeForceMultiplier;

            m_EscapeForceApplier.ApplyForce(force);

            if (m_CarriedObject.EscapeRecoilMultiplier != 0f)
            {
                force = direction.normalized;
                force *= m_CarriedObject.EscapeRecoilMultiplier * m_EscapeRecoil;
                m_EscapeRecoilApplier.ApplyForce(-force);
            }

            m_Carrying = false;
            m_OnGrabbedObjectEscaped?.Invoke();
        }

      


        /// <summary>
        /// Stops all feedbacks
        /// </summary>
        protected virtual void StopFeedbacks()
        {
            if (_startFeedbackIsPlaying)
            {
                StopStartFeedbacks();
                PlayAbilityStopFeedbacks();
            }
        }

        /// <summary>
        /// On late update we reset our states
        /// </summary>
        protected virtual void LateUpdate()
        {
            m_Grabbing = false;
            m_Throwing = false;
        }

        /// <summary>
        /// Adds required animator parameters to the animator parameters list if they exist
        /// </summary>
        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter(m_GrabbingAnimationParameterName, AnimatorControllerParameterType.Bool, out m_GrabbingAnimationParameter);
            RegisterAnimatorParameter(m_CarryingAnimationParameterName, AnimatorControllerParameterType.Bool, out m_CarryingAnimationParameter);
            RegisterAnimatorParameter(m_CarryingIDAnimationParameterName, AnimatorControllerParameterType.Int, out m_CarryingIDAnimationParameter);
            RegisterAnimatorParameter(m_ThrowingAnimationParameterName, AnimatorControllerParameterType.Bool, out m_ThrowingAnimationParameter);
        }

        /// <summary>
        /// At the end of each cycle, we update our animator parameters with our current state
        /// </summary>
        public override void UpdateAnimator()
        {
            MMAnimatorExtensions.UpdateAnimatorBool(_animator, m_GrabbingAnimationParameterName, m_Grabbing);
            MMAnimatorExtensions.UpdateAnimatorBool(_animator, m_ThrowingAnimationParameterName, m_Throwing);
            MMAnimatorExtensions.UpdateAnimatorBool(_animator, m_CarryingAnimationParameterName, m_Carrying);
            MMAnimatorExtensions.UpdateAnimatorInteger(_animator, m_CarryingIDAnimationParameterName, m_CarryingID);
        }

        /// <summary>
        /// On reset ability, we cancel all the changes made
        /// </summary>
        public override void ResetAbility()
        {
            base.ResetAbility();
            TryThrow();
            if (_animator != null)
            {
                MMAnimatorExtensions.UpdateAnimatorBool(_animator, m_GrabbingAnimationParameterName, false);
                MMAnimatorExtensions.UpdateAnimatorBool(_animator, m_ThrowingAnimationParameterName, false);
                MMAnimatorExtensions.UpdateAnimatorBool(_animator, m_CarryingAnimationParameterName, false);
            }
        }
    }
}
