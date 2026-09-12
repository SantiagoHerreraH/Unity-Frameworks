using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class CopyCollisionRegistry_CachedGameAction :
        ICachedGameAction
    {
        private enum CopyInteractionProtocol
        {
            DontCopyInteractions,

            CopyInteractionsToJustAffectNewContacts,

            CopyInteractionsToAffectNewAndOldContacts
        }


        private enum PreviousContactsProtocol
        {
            LeavePreviousContactsAlone,

            RestartPreviousContacts,

            RemovePreviousContacts
        }


        [Title("Copy Operation")]
        [SerializeField]
        private CopyType m_CopyType;


        [SerializeField]
        private ContactRegistry
            m_CustomCollisionRegistry;


        [SerializeField]
        private CopyInteractionProtocol
            m_CopyInteractionProtocol;


        [SerializeField]
        private PreviousContactsProtocol
            m_PreviousContactsProtocol;


        [SerializeField]
        private bool
            m_CopyContacts;


        [Title("Debug")]
        [SerializeField]
        private bool
            m_PrintMessageIfSelfHasNoCollisionRegistry = true;


        private GameObject m_Self;

        private ContactRegistry
            m_SelfCollisionRegistry;


        public ICachedGameAction Clone()
        {
            return new CopyCollisionRegistry_CachedGameAction
            {
                m_CopyType =
                    m_CopyType,

                m_CustomCollisionRegistry =
                    m_CustomCollisionRegistry,

                m_CopyInteractionProtocol =
                    m_CopyInteractionProtocol,

                m_PreviousContactsProtocol =
                    m_PreviousContactsProtocol,

                m_CopyContacts =
                    m_CopyContacts,

                m_PrintMessageIfSelfHasNoCollisionRegistry =
                    m_PrintMessageIfSelfHasNoCollisionRegistry
            };
        }


        public void Execute()
        {
            if (m_SelfCollisionRegistry == null)
            {
                if (m_PrintMessageIfSelfHasNoCollisionRegistry)
                {
                    Debug.LogWarning(
                        $"{nameof(CopyCollisionRegistry_CachedGameAction)}: " +
                        $"'{m_Self?.name ?? "null"}' doesn't have a " +
                        $"{nameof(ContactRegistry)}.");
                }

                return;
            }


            if (m_CustomCollisionRegistry == null)
            {
                Debug.LogWarning(
                    $"{nameof(CopyCollisionRegistry_CachedGameAction)}: " +
                    $"No custom {nameof(ContactRegistry)} has been assigned.");

                return;
            }


            ContactRegistry source;
            ContactRegistry destination;


            switch (m_CopyType)
            {
                case CopyType.SelfToCustom:

                    source =
                        m_SelfCollisionRegistry;

                    destination =
                        m_CustomCollisionRegistry;

                    break;


                case CopyType.CustomToSelf:

                    source =
                        m_CustomCollisionRegistry;

                    destination =
                        m_SelfCollisionRegistry;

                    break;


                default:
                    return;
            }


            if (source == destination)
                return;


            ExecuteCopyProtocols(
                source,
                destination);


            /*
             * Copied contacts are treated as new contacts on the destination.
             *
             * Therefore they use whatever interaction definitions the
             * destination has after ExecuteCopyProtocols().
             */
            if (m_CopyContacts)
            {
                destination.CopyContactsFrom(
                    source);
            }
        }


        /// <summary>
        /// Handles:
        ///
        /// 1. Whether interaction definitions are copied.
        /// 2. Whether those definitions affect existing contacts.
        /// 3. What happens to the destination's previous contacts.
        /// </summary>
        private void ExecuteCopyProtocols(
            ContactRegistry source,
            ContactRegistry destination)
        {
            bool previousContactsAlreadyRestarted =
                false;


            // ============================================================
            // COPY INTERACTION DEFINITIONS
            // ============================================================

            switch (m_CopyInteractionProtocol)
            {
                case CopyInteractionProtocol
                    .DontCopyInteractions:

                    break;


                case CopyInteractionProtocol
                    .CopyInteractionsToJustAffectNewContacts:

                    /*
                     * Replace the registry's interaction definitions without
                     * touching already-running ContactInteractions.
                     *
                     * Future/copied contacts use the new interaction set.
                     * Existing contacts retain the interaction instances they
                     * were originally created with.
                     */
                    destination.CopyInteractionsFrom(
                        source,
                        false);

                    break;


                case CopyInteractionProtocol
                    .CopyInteractionsToAffectNewAndOldContacts:

                    /*
                     * If previous contacts are about to be removed, there's no
                     * reason to restart them first.
                     */
                    bool restartExistingContacts =
                        m_PreviousContactsProtocol !=
                        PreviousContactsProtocol
                            .RemovePreviousContacts;

                    destination.CopyInteractionsFrom(
                        source,
                        restartExistingContacts);

                    previousContactsAlreadyRestarted =
                        restartExistingContacts;

                    break;
            }


            // ============================================================
            // HANDLE DESTINATION'S PREVIOUS CONTACTS
            // ============================================================

            switch (m_PreviousContactsProtocol)
            {
                case PreviousContactsProtocol
                    .LeavePreviousContactsAlone:

                    break;


                case PreviousContactsProtocol
                    .RestartPreviousContacts:

                    /*
                     * If CopyInteractionsToAffectNewAndOldContacts was used,
                     * those contacts were already restarted with the new
                     * interaction definitions.
                     */
                    if (!previousContactsAlreadyRestarted)
                    {
                        /*
                         * RestartCurrentContacts() restarts the interaction
                         * instances each contact already owns.
                         *
                         * This is important for:
                         *
                         * CopyInteractionsToJustAffectNewContacts
                         *
                         * because restarting previous contacts should NOT
                         * suddenly give them the newly copied definitions.
                         */
                        destination.RestartCurrentContacts();
                    }

                    break;


                case PreviousContactsProtocol
                    .RemovePreviousContacts:

                    destination.RemoveCurrentContacts();

                    break;
            }
        }


        public GameObject GetGameObject()
        {
            return m_Self;
        }


        public bool SetGameObject(
            GameObject gameObj)
        {
            m_Self =
                gameObj;

            m_SelfCollisionRegistry =
                null;


            if (m_Self == null)
                return false;


            bool found =
                m_Self.TryGetComponent<ContactRegistry>(
                    out m_SelfCollisionRegistry);


            if (!found &&
                m_PrintMessageIfSelfHasNoCollisionRegistry)
            {
                Debug.LogWarning(
                    $"{nameof(CopyCollisionRegistry_CachedGameAction)}: " +
                    $"'{m_Self.name}' doesn't have a " +
                    $"{nameof(ContactRegistry)}.");
            }


            return found;
        }
    }
}