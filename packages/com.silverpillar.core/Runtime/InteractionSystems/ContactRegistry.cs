using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public struct GeneralContactPoint
    {
        public Vector3 Position;
        public Vector3 Normal;
    }


    public struct ContactData
    {
        public IReadOnlyList<GeneralContactPoint> WorldContactPoints
        {
            get { return m_WorldContactPoints; }
        }

        public IReadOnlyList<GeneralContactPoint> LocalContactPoints
        {
            get { return m_LocalContactPoints; }
        }

        public GameObject GameObject
        {
            get { return m_GameObject; }
        }


        private List<GeneralContactPoint> m_WorldContactPoints;
        private List<GeneralContactPoint> m_LocalContactPoints;
        private GameObject m_GameObject;


        public GeneralContactPoint GetAverageWorldContactPoint()
        {
            return CalculateAverage(m_WorldContactPoints);
        }


        public GeneralContactPoint GetAverageLocalContactPoint()
        {
            return CalculateAverage(m_LocalContactPoints);
        }


        public GeneralContactPoint GetAverageLocalContactPointAsWorldPoint()
        {
            GeneralContactPoint localPoint =
                GetAverageLocalContactPoint();

            if (m_GameObject == null)
            {
                return new GeneralContactPoint();
            }

            Transform targetTransform =
                m_GameObject.transform;

            return new GeneralContactPoint
            {
                Position =
                    targetTransform.TransformPoint(
                        localPoint.Position),

                Normal =
                    LocalNormalToWorld(
                        targetTransform,
                        localPoint.Normal)
            };
        }


        public ContactData Clone()
        {
            ContactData clone =
                new ContactData
                {
                    m_GameObject = m_GameObject
                };

            if (m_WorldContactPoints != null)
            {
                clone.m_WorldContactPoints =
                    new List<GeneralContactPoint>(
                        m_WorldContactPoints);
            }

            if (m_LocalContactPoints != null)
            {
                clone.m_LocalContactPoints =
                    new List<GeneralContactPoint>(
                        m_LocalContactPoints);
            }

            return clone;
        }


        public void Calculate(Collider collider)
        {
            if (collider == null)
            {
                Reset();
                return;
            }

            Calculate(
                collider,
                collider.transform.position);
        }


        public void Calculate(
            Collider collider,
            Vector3 referenceWorldPosition)
        {
            Reset();

            if (collider == null)
                return;

            EnsureLists();

            m_GameObject =
                collider.gameObject;

            Vector3 worldPosition =
                collider.ClosestPoint(
                    referenceWorldPosition);

            Vector3 worldNormal =
                EstimateNormal(
                    worldPosition,
                    referenceWorldPosition,
                    collider.bounds.center,
                    collider.transform.up);

            AddWorldPoint(
                worldPosition,
                worldNormal);
        }


        public void Calculate(Collider2D collider)
        {
            if (collider == null)
            {
                Reset();
                return;
            }

            Calculate(
                collider,
                collider.transform.position);
        }


        public void Calculate(
            Collider2D collider,
            Vector3 referenceWorldPosition)
        {
            Reset();

            if (collider == null)
                return;

            EnsureLists();

            m_GameObject =
                collider.gameObject;

            Vector2 referencePosition =
                new Vector2(
                    referenceWorldPosition.x,
                    referenceWorldPosition.y);

            Vector2 closestPoint =
                collider.ClosestPoint(
                    referencePosition);

            Vector3 worldPosition =
                new Vector3(
                    closestPoint.x,
                    closestPoint.y,
                    collider.transform.position.z);

            Vector3 worldNormal =
                EstimateNormal(
                    worldPosition,
                    referenceWorldPosition,
                    collider.bounds.center,
                    collider.transform.up);

            worldNormal.z = 0f;

            if (worldNormal.sqrMagnitude >
                Mathf.Epsilon)
            {
                worldNormal.Normalize();
            }

            AddWorldPoint(
                worldPosition,
                worldNormal);
        }


        public void Calculate(Collision collision)
        {
            Reset();

            if (collision == null)
                return;

            EnsureLists();

            m_GameObject =
                collision.gameObject;

            int contactCount =
                collision.contactCount;

            for (int i = 0;
                 i < contactCount;
                 i++)
            {
                ContactPoint contact =
                    collision.GetContact(i);

                AddWorldPoint(
                    contact.point,
                    contact.normal);
            }
        }


        public void Calculate(Collision2D collision)
        {
            Reset();

            if (collision == null)
                return;

            EnsureLists();

            m_GameObject =
                collision.gameObject;

            int contactCount =
                collision.contactCount;

            for (int i = 0;
                 i < contactCount;
                 i++)
            {
                ContactPoint2D contact =
                    collision.GetContact(i);

                Vector3 worldPosition =
                    new Vector3(
                        contact.point.x,
                        contact.point.y,
                        collision.transform.position.z);

                Vector3 worldNormal =
                    new Vector3(
                        contact.normal.x,
                        contact.normal.y,
                        0f);

                AddWorldPoint(
                    worldPosition,
                    worldNormal);
            }
        }


        public void Reset()
        {
            m_WorldContactPoints?.Clear();
            m_LocalContactPoints?.Clear();

            m_GameObject = null;
        }


        private void EnsureLists()
        {
            m_WorldContactPoints ??=
                new List<GeneralContactPoint>();

            m_LocalContactPoints ??=
                new List<GeneralContactPoint>();
        }


        private void AddWorldPoint(
            Vector3 worldPosition,
            Vector3 worldNormal)
        {
            if (m_GameObject == null)
                return;

            EnsureLists();

            if (worldNormal.sqrMagnitude >
                Mathf.Epsilon)
            {
                worldNormal.Normalize();
            }

            m_WorldContactPoints.Add(
                new GeneralContactPoint
                {
                    Position = worldPosition,
                    Normal = worldNormal
                });

            Transform targetTransform =
                m_GameObject.transform;

            m_LocalContactPoints.Add(
                new GeneralContactPoint
                {
                    Position =
                        targetTransform.InverseTransformPoint(
                            worldPosition),

                    Normal =
                        WorldNormalToLocal(
                            targetTransform,
                            worldNormal)
                });
        }


        private static GeneralContactPoint CalculateAverage(
            List<GeneralContactPoint> points)
        {
            if (points == null ||
                points.Count == 0)
            {
                return new GeneralContactPoint();
            }

            Vector3 averagePosition =
                Vector3.zero;

            Vector3 averageNormal =
                Vector3.zero;

            foreach (GeneralContactPoint point
                     in points)
            {
                averagePosition +=
                    point.Position;

                averageNormal +=
                    point.Normal;
            }

            averagePosition /=
                points.Count;

            averageNormal /=
                points.Count;

            if (averageNormal.sqrMagnitude >
                Mathf.Epsilon)
            {
                averageNormal.Normalize();
            }

            return new GeneralContactPoint
            {
                Position = averagePosition,
                Normal = averageNormal
            };
        }


        private static Vector3 EstimateNormal(
            Vector3 closestPoint,
            Vector3 referencePosition,
            Vector3 colliderCenter,
            Vector3 fallbackNormal)
        {
            Vector3 normal =
                referencePosition -
                closestPoint;

            if (normal.sqrMagnitude >
                Mathf.Epsilon)
            {
                return normal.normalized;
            }

            normal =
                closestPoint -
                colliderCenter;

            if (normal.sqrMagnitude >
                Mathf.Epsilon)
            {
                return normal.normalized;
            }

            if (fallbackNormal.sqrMagnitude >
                Mathf.Epsilon)
            {
                return fallbackNormal.normalized;
            }

            return Vector3.up;
        }


        private static Vector3 WorldNormalToLocal(
            Transform transform,
            Vector3 worldNormal)
        {
            if (worldNormal.sqrMagnitude <=
                Mathf.Epsilon)
            {
                return Vector3.zero;
            }

            Vector3 localNormal =
                transform.localToWorldMatrix
                    .transpose
                    .MultiplyVector(
                        worldNormal);

            if (localNormal.sqrMagnitude >
                Mathf.Epsilon)
            {
                localNormal.Normalize();
            }

            return localNormal;
        }


        private static Vector3 LocalNormalToWorld(
            Transform transform,
            Vector3 localNormal)
        {
            if (localNormal.sqrMagnitude <=
                Mathf.Epsilon)
            {
                return Vector3.zero;
            }

            Vector3 worldNormal =
                transform.worldToLocalMatrix
                    .transpose
                    .MultiplyVector(
                        localNormal);

            if (worldNormal.sqrMagnitude >
                Mathf.Epsilon)
            {
                worldNormal.Normalize();
            }

            return worldNormal;
        }
    }


    public struct ContactInteraction
    {
        private GameObject m_Self;

        private ContactData m_OtherData;

        private List<IContactInteraction>
            m_Interactions;

        /*
         * Registration and execution are now separate concepts.
         *
         * A ContactInteraction may remain registered while its
         * IContactInteractions are stopped.
         */
        private bool m_InteractionsRunning;


        public GameObject Other()
        {
            return m_OtherData.GameObject;
        }


        public bool InteractionsRunning()
        {
            return m_InteractionsRunning;
        }


        public ContactData GetContactData()
        {
            return m_OtherData.Clone();
        }


        /// <summary>
        /// Initializes this registered contact and immediately starts its
        /// interactions.
        /// </summary>
        public void Start(
            GameObject self,
            ContactData otherData,
            List<IContactInteraction> interactions)
        {
            m_Self =
                self;

            m_OtherData =
                otherData.Clone();

            m_Interactions =
                CloneInteractions(
                    interactions);

            m_InteractionsRunning =
                false;

            StartInteractions();
        }


        /// <summary>
        /// Updates the stored contact information without replacing the
        /// interaction instances.
        /// </summary>
        public void UpdateContactData(
            ContactData otherData)
        {
            m_OtherData =
                otherData.Clone();
        }


        /// <summary>
        /// Starts the interaction instances if they aren't already running.
        /// </summary>
        public void StartInteractions()
        {
            if (m_InteractionsRunning)
                return;

            m_InteractionsRunning =
                true;

            if (m_Interactions == null)
                return;

            foreach (IContactInteraction interaction
                     in m_Interactions)
            {
                interaction?.Start(
                    m_Self,
                    m_OtherData);
            }
        }


        /// <summary>
        /// Ends the old interaction instances, replaces them and starts the
        /// newly supplied interactions.
        /// </summary>
        public void ChangeInteractionsAndRestart(
            List<IContactInteraction> interactions)
        {
            End();

            m_Interactions =
                CloneInteractions(
                    interactions);

            StartInteractions();
        }


        public void Update()
        {
            if (!m_InteractionsRunning ||
                m_Interactions == null)
            {
                return;
            }

            foreach (IContactInteraction interaction
                     in m_Interactions)
            {
                interaction?.Update();
            }
        }


        /// <summary>
        /// Restarts this contact's existing interaction instances without
        /// replacing them with the ContactRegistry's current definitions.
        /// </summary>
        public void Restart()
        {
            End();
            StartInteractions();
        }


        public void End()
        {
            if (!m_InteractionsRunning)
                return;

            m_InteractionsRunning =
                false;

            if (m_Interactions == null)
                return;

            foreach (IContactInteraction interaction
                     in m_Interactions)
            {
                interaction?.End();
            }
        }


        private static List<IContactInteraction>
            CloneInteractions(
                List<IContactInteraction> interactions)
        {
            List<IContactInteraction> result =
                new List<IContactInteraction>();

            if (interactions == null)
                return result;

            foreach (IContactInteraction interaction
                     in interactions)
            {
                if (interaction == null)
                    continue;

                IContactInteraction clone =
                    interaction.Clone();

                if (clone != null)
                {
                    result.Add(
                        clone);
                }
            }

            return result;
        }
    }


    public interface IContactInteraction
    {
        public void Start(
            GameObject self,
            ContactData otherData);

        public void Update();

        public void End();

        public IContactInteraction Clone();
    }


    public class ContactRegistry :
        SerializedMonoBehaviour
    {
        [Title("Contact Interactions")]
        [OdinSerialize, ShowInInspector]
        private List<IContactInteraction>
            m_Interactions;


        [Title("Register and Unregister")]

        [SerializeField,
         Tooltip(
             "Zero and below means infinite. " +
             "If a new contact is registered and the maximum is exceeded, " +
             "the oldest registered contact ends its interactions and is " +
             "deregistered.")]
        private int m_MaxContactsPermitted;


        [SerializeField]
        private bool
            m_RegisterInteractionsOnEnterContact;


        [SerializeField]
        private bool
            m_UnregisterInteractionsOnExitContact;


        [SerializeField]
        private bool
            m_UnregisterInteractionsOnDisable;


        [Title("Start and End")]

        [SerializeField]
        private bool
            m_StartInteractionsOnEnable;


        [SerializeField]
        private bool
            m_EndInteractionsOnExitContact;


        [SerializeField]
        private bool
            m_EndInteractionsOnDisable;


        /// <summary>
        /// Registered contacts.
        ///
        /// The order is important because index 0 represents the oldest
        /// registered contact for m_MaxContactsPermitted.
        /// </summary>
        private List<ContactInteraction>
            m_ContactInteractions;


        /// <summary>
        /// Tracks the number of colliders belonging to each GameObject
        /// currently physically touching this registry.
        ///
        /// This prevents a GameObject with multiple colliders from generating
        /// multiple logical registrations.
        /// </summary>
        private Dictionary<GameObject, int>
            m_ContactCounts;


        private void Awake()
        {
            EnsureRuntimeCollections();
        }


        private void OnEnable()
        {
            EnsureRuntimeCollections();

            if (m_StartInteractionsOnEnable)
            {
                StartCurrentInteractions();
            }
        }


        private void Update()
        {
            if (m_ContactInteractions == null)
                return;

            for (int i =
                    m_ContactInteractions.Count - 1;
                 i >= 0;
                 i--)
            {
                ContactInteraction interaction =
                    m_ContactInteractions[i];

                /*
                 * The contacted object may be destroyed without an exit
                 * callback being received.
                 */
                if (interaction.Other() == null)
                {
                    interaction.End();

                    m_ContactInteractions.RemoveAt(i);

                    continue;
                }

                interaction.Update();

                /*
                 * ContactInteraction is a struct, so mutations must be
                 * written back into the list.
                 */
                m_ContactInteractions[i] =
                    interaction;
            }

            RemoveDestroyedContactCounts();
        }


        private void OnDisable()
        {
            /*
             * Deregistering necessarily ends interactions because those
             * runtime interaction instances are about to be discarded.
             */
            if (m_UnregisterInteractionsOnDisable)
            {
                RemoveCurrentContacts();
                return;
            }

            /*
             * Contacts remain registered, but their interactions stop.
             *
             * This allows m_StartInteractionsOnEnable to start them again
             * when the registry is re-enabled.
             */
            if (m_EndInteractionsOnDisable)
            {
                EndCurrentInteractions();
            }
        }


        // ================================================================
        // INTERACTION CONFIGURATION
        // ================================================================

        /// <summary>
        /// Replaces the interaction definitions used by newly registered
        /// contacts.
        ///
        /// If resetAllContactInteractions is true, all currently registered
        /// contacts also replace their runtime instances and restart.
        /// </summary>
        public void ChangeInteractions(
            List<IContactInteraction> interactions,
            bool resetAllContactInteractions)
        {
            m_Interactions ??=
                new List<IContactInteraction>();

            m_Interactions.Clear();

            if (interactions != null)
            {
                foreach (IContactInteraction interaction
                         in interactions)
                {
                    if (interaction == null)
                        continue;

                    IContactInteraction clone =
                        interaction.Clone();

                    if (clone != null)
                    {
                        m_Interactions.Add(
                            clone);
                    }
                }
            }


            if (!resetAllContactInteractions ||
                m_ContactInteractions == null)
            {
                return;
            }


            for (int i = 0;
                 i < m_ContactInteractions.Count;
                 i++)
            {
                ContactInteraction contactInteraction =
                    m_ContactInteractions[i];

                contactInteraction
                    .ChangeInteractionsAndRestart(
                        m_Interactions);

                m_ContactInteractions[i] =
                    contactInteraction;
            }
        }


        public void CopyInteractionsFrom(
            ContactRegistry source,
            bool restartCurrentInteractions)
        {
            if (source == null ||
                source == this)
            {
                return;
            }

            ChangeInteractions(
                source.m_Interactions,
                restartCurrentInteractions);
        }


        // ================================================================
        // CURRENT CONTACT MANAGEMENT
        // ================================================================

        /// <summary>
        /// Starts every registered contact that is currently stopped.
        /// </summary>
        public void StartCurrentInteractions()
        {
            if (m_ContactInteractions == null)
                return;

            for (int i = 0;
                 i < m_ContactInteractions.Count;
                 i++)
            {
                ContactInteraction contactInteraction =
                    m_ContactInteractions[i];

                contactInteraction.StartInteractions();

                m_ContactInteractions[i] =
                    contactInteraction;
            }
        }


        /// <summary>
        /// Ends all currently running interactions while leaving the contacts
        /// registered.
        /// </summary>
        public void EndCurrentInteractions()
        {
            if (m_ContactInteractions == null)
                return;

            for (int i = 0;
                 i < m_ContactInteractions.Count;
                 i++)
            {
                ContactInteraction contactInteraction =
                    m_ContactInteractions[i];

                contactInteraction.End();

                m_ContactInteractions[i] =
                    contactInteraction;
            }
        }


        /// <summary>
        /// Restarts every registered contact using the interaction instances
        /// it already owns.
        /// </summary>
        public void RestartCurrentContacts()
        {
            if (m_ContactInteractions == null)
                return;

            for (int i = 0;
                 i < m_ContactInteractions.Count;
                 i++)
            {
                ContactInteraction contactInteraction =
                    m_ContactInteractions[i];

                contactInteraction.Restart();

                m_ContactInteractions[i] =
                    contactInteraction;
            }
        }


        /// <summary>
        /// Ends and deregisters every registered contact.
        /// </summary>
        public void RemoveCurrentContacts()
        {
            if (m_ContactInteractions != null)
            {
                for (int i =
                        m_ContactInteractions.Count - 1;
                     i >= 0;
                     i--)
                {
                    ContactInteraction interaction =
                        m_ContactInteractions[i];

                    interaction.End();
                }

                m_ContactInteractions.Clear();
            }

            m_ContactCounts?.Clear();
        }


        /// <summary>
        /// Copies registered contacts from another ContactRegistry.
        ///
        /// Existing destination contacts aren't automatically removed.
        /// Copied contacts are considered newly registered and therefore use
        /// this registry's current interaction definitions.
        /// </summary>
        public void CopyContactsFrom(
            ContactRegistry source)
        {
            if (source == null ||
                source == this)
            {
                return;
            }

            EnsureRuntimeCollections();
            source.EnsureRuntimeCollections();


            /*
             * Take a snapshot because registering contacts may enforce this
             * registry's maximum contact count and mutate its list.
             */
            List<ContactData> sourceContacts =
                new List<ContactData>();


            foreach (ContactInteraction interaction
                     in source.m_ContactInteractions)
            {
                ContactData data =
                    interaction.GetContactData();

                if (data.GameObject == null)
                    continue;

                sourceContacts.Add(
                    data);
            }


            foreach (ContactData contactData
                     in sourceContacts)
            {
                RegisterInteraction(
                    contactData);
            }
        }


        // ================================================================
        // MANUAL REGISTRATION
        // ================================================================

        public void RegisterInteraction(
            Collider collider)
        {
            if (collider == null)
                return;

            ContactData data =
                new ContactData();

            data.Calculate(
                collider,
                transform.position);

            RegisterInteraction(
                data);
        }


        public void RegisterInteraction(
            Collider2D collider)
        {
            if (collider == null)
                return;

            ContactData data =
                new ContactData();

            data.Calculate(
                collider,
                transform.position);

            RegisterInteraction(
                data);
        }


        public void RegisterInteraction(
            Collision collision)
        {
            if (collision == null)
                return;

            ContactData data =
                new ContactData();

            data.Calculate(
                collision);

            RegisterInteraction(
                data);
        }


        public void RegisterInteraction(
            Collision2D collision)
        {
            if (collision == null)
                return;

            ContactData data =
                new ContactData();

            data.Calculate(
                collision);

            RegisterInteraction(
                data);
        }


        public void RegisterInteraction(
            ContactData data)
        {
            if (data.GameObject == null)
                return;

            EnsureRuntimeCollections();


            /*
             * If this GameObject is already registered, don't create a second
             * ContactInteraction.
             *
             * Instead, refresh its contact data. If its interactions were
             * stopped (for example because they ended on exit), entering
             * contact again starts them.
             */
            int existingIndex =
                FindInteraction(
                    data.GameObject);

            if (existingIndex >= 0)
            {
                ContactInteraction existing =
                    m_ContactInteractions[
                        existingIndex];

                existing.UpdateContactData(
                    data);

                existing.StartInteractions();

                m_ContactInteractions[
                    existingIndex] =
                    existing;

                return;
            }


            ContactInteraction contactInteraction =
                new ContactInteraction();

            contactInteraction.Start(
                gameObject,
                data,
                m_Interactions);

            m_ContactInteractions.Add(
                contactInteraction);


            EnforceMaximumContactCount();
        }


        /// <summary>
        /// Ends and deregisters a specific GameObject.
        /// </summary>
        public void UnregisterInteraction(
            GameObject other)
        {
            if (other == null ||
                m_ContactInteractions == null)
            {
                return;
            }

            for (int i =
                    m_ContactInteractions.Count - 1;
                 i >= 0;
                 i--)
            {
                ContactInteraction interaction =
                    m_ContactInteractions[i];

                if (interaction.Other() != other)
                    continue;

                interaction.End();

                m_ContactInteractions.RemoveAt(i);
            }
        }


        // ================================================================
        // 3D TRIGGERS
        // ================================================================

        private void OnTriggerEnter(
            Collider other)
        {
            if (other == null)
                return;

            bool firstContact =
                RegisterPhysicalContact(
                    other.gameObject);

            if (!firstContact)
                return;


            if (m_RegisterInteractionsOnEnterContact)
            {
                RegisterInteraction(
                    other);
            }
        }


        private void OnTriggerExit(
            Collider other)
        {
            if (other == null)
                return;

            bool lastContact =
                UnregisterPhysicalContact(
                    other.gameObject);

            if (!lastContact)
                return;

            HandleExitContact(
                other.gameObject);
        }


        // ================================================================
        // 2D TRIGGERS
        // ================================================================

        private void OnTriggerEnter2D(
            Collider2D other)
        {
            if (other == null)
                return;

            bool firstContact =
                RegisterPhysicalContact(
                    other.gameObject);

            if (!firstContact)
                return;


            if (m_RegisterInteractionsOnEnterContact)
            {
                RegisterInteraction(
                    other);
            }
        }


        private void OnTriggerExit2D(
            Collider2D other)
        {
            if (other == null)
                return;

            bool lastContact =
                UnregisterPhysicalContact(
                    other.gameObject);

            if (!lastContact)
                return;

            HandleExitContact(
                other.gameObject);
        }


        // ================================================================
        // 3D COLLISIONS
        // ================================================================

        private void OnCollisionEnter(
            Collision collision)
        {
            if (collision == null)
                return;

            bool firstContact =
                RegisterPhysicalContact(
                    collision.gameObject);

            if (!firstContact)
                return;


            if (m_RegisterInteractionsOnEnterContact)
            {
                RegisterInteraction(
                    collision);
            }
        }


        private void OnCollisionExit(
            Collision collision)
        {
            if (collision == null)
                return;

            bool lastContact =
                UnregisterPhysicalContact(
                    collision.gameObject);

            if (!lastContact)
                return;

            HandleExitContact(
                collision.gameObject);
        }


        // ================================================================
        // 2D COLLISIONS
        // ================================================================

        private void OnCollisionEnter2D(
            Collision2D collision)
        {
            if (collision == null)
                return;

            bool firstContact =
                RegisterPhysicalContact(
                    collision.gameObject);

            if (!firstContact)
                return;


            if (m_RegisterInteractionsOnEnterContact)
            {
                RegisterInteraction(
                    collision);
            }
        }


        private void OnCollisionExit2D(
            Collision2D collision)
        {
            if (collision == null)
                return;

            bool lastContact =
                UnregisterPhysicalContact(
                    collision.gameObject);

            if (!lastContact)
                return;

            HandleExitContact(
                collision.gameObject);
        }


        // ================================================================
        // EXIT BEHAVIOUR
        // ================================================================

        private void HandleExitContact(
            GameObject other)
        {
            /*
             * Deregistration wins over simply ending because the interaction
             * instances have to be cleaned up before being discarded anyway.
             */
            if (m_UnregisterInteractionsOnExitContact)
            {
                UnregisterInteraction(
                    other);

                return;
            }


            /*
             * Keep the contact registered, but stop its interactions.
             *
             * If it physically contacts this registry again,
             * RegisterInteraction() will refresh the ContactData and start
             * the existing interaction instances again.
             */
            if (m_EndInteractionsOnExitContact)
            {
                EndInteraction(
                    other);
            }
        }


        // ================================================================
        // INTERNAL PHYSICAL CONTACT TRACKING
        // ================================================================

        private bool RegisterPhysicalContact(
            GameObject other)
        {
            if (other == null)
                return false;

            EnsureRuntimeCollections();

            if (m_ContactCounts.TryGetValue(
                other,
                out int contactCount))
            {
                m_ContactCounts[other] =
                    contactCount + 1;

                return false;
            }

            m_ContactCounts.Add(
                other,
                1);

            return true;
        }


        /// <summary>
        /// Returns true once the final collider belonging to this GameObject
        /// has stopped touching the registry.
        /// </summary>
        private bool UnregisterPhysicalContact(
            GameObject other)
        {
            if (other == null ||
                m_ContactCounts == null)
            {
                return true;
            }

            if (!m_ContactCounts.TryGetValue(
                other,
                out int contactCount))
            {
                return true;
            }

            contactCount--;


            if (contactCount <= 0)
            {
                m_ContactCounts.Remove(
                    other);

                return true;
            }


            m_ContactCounts[other] =
                contactCount;

            return false;
        }


        // ================================================================
        // MAXIMUM CONTACT COUNT
        // ================================================================

        private void EnforceMaximumContactCount()
        {
            /*
             * Zero and negative values mean unlimited.
             */
            if (m_MaxContactsPermitted <= 0 ||
                m_ContactInteractions == null)
            {
                return;
            }


            while (m_ContactInteractions.Count >
                   m_MaxContactsPermitted)
            {
                /*
                 * Index 0 is the oldest registered contact.
                 */
                ContactInteraction oldestInteraction =
                    m_ContactInteractions[0];

                oldestInteraction.End();

                m_ContactInteractions.RemoveAt(0);


                /*
                 * We deliberately do NOT remove it from m_ContactCounts here.
                 *
                 * m_ContactCounts represents actual physical collider
                 * overlap, not registration state.
                 *
                 * If the object exits later, Unity's exit callback still
                 * needs to decrement its physical contact count correctly.
                 */
            }
        }


        // ================================================================
        // INTERNAL REGISTERED-INTERACTION MANAGEMENT
        // ================================================================

        private int FindInteraction(
            GameObject other)
        {
            if (m_ContactInteractions == null)
                return -1;


            for (int i = 0;
                 i < m_ContactInteractions.Count;
                 i++)
            {
                if (m_ContactInteractions[i]
                        .Other() == other)
                {
                    return i;
                }
            }


            return -1;
        }


        /// <summary>
        /// Ends the interactions for a GameObject but leaves the contact
        /// registered.
        /// </summary>
        private void EndInteraction(
            GameObject other)
        {
            if (m_ContactInteractions == null)
                return;


            int index =
                FindInteraction(
                    other);

            if (index < 0)
                return;


            ContactInteraction interaction =
                m_ContactInteractions[index];

            interaction.End();

            m_ContactInteractions[index] =
                interaction;
        }


        private void RemoveDestroyedContactCounts()
        {
            if (m_ContactCounts == null ||
                m_ContactCounts.Count == 0)
            {
                return;
            }


            List<GameObject> objectsToRemove =
                null;


            foreach (KeyValuePair<GameObject, int> pair
                     in m_ContactCounts)
            {
                if (pair.Key != null)
                    continue;

                objectsToRemove ??=
                    new List<GameObject>();

                objectsToRemove.Add(
                    pair.Key);
            }


            if (objectsToRemove == null)
                return;


            foreach (GameObject gameObject
                     in objectsToRemove)
            {
                m_ContactCounts.Remove(
                    gameObject);
            }
        }


        private void EnsureRuntimeCollections()
        {
            m_ContactInteractions ??=
                new List<ContactInteraction>();

            m_ContactCounts ??=
                new Dictionary<GameObject, int>();
        }
    }
}