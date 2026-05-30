using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.XR
{
    [Serializable]
    public class SetCurrentPrefabToPlace_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private GameObject m_Prefab;

        [SerializeField]
        private SelfType m_OnWhichTapToPlacwAnchored;

        [SerializeField, ShowIf(nameof(m_OnWhichTapToPlacwAnchored), SelfType.CustomGameObject)]
        private TapToPlaceAnchored_NewInputSystem m_TapToPlaceAnchored;

        private GameObject m_GameObject;

        public ICachedGameAction Clone()
        {
            return new SetCurrentPrefabToPlace_CachedGameAction
            {
                m_Prefab = m_Prefab,
                m_OnWhichTapToPlacwAnchored = m_OnWhichTapToPlacwAnchored,
                m_TapToPlaceAnchored = m_TapToPlaceAnchored,
                m_GameObject = m_GameObject
            };
        }

        public void Execute()
        {
            TapToPlaceAnchored_NewInputSystem tapToPlace = GetTapToPlaceAnchored();

            if (tapToPlace == null)
                return;

            tapToPlace.SetPrefabToPlace(m_Prefab);
        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;
            return GetTapToPlaceAnchored() != null;
        }

        private TapToPlaceAnchored_NewInputSystem GetTapToPlaceAnchored()
        {
            switch (m_OnWhichTapToPlacwAnchored)
            {
                case SelfType.ThisGameObject:

                    if (m_TapToPlaceAnchored == null)
                    {
                        m_TapToPlaceAnchored = m_GameObject.GetComponent<TapToPlaceAnchored_NewInputSystem>();
                    }
                    return m_TapToPlaceAnchored;


                case SelfType.CustomGameObject:
                    return m_TapToPlaceAnchored;

                default:
                    if (m_TapToPlaceAnchored == null)
                    {
                        m_TapToPlaceAnchored = m_GameObject.GetComponent<TapToPlaceAnchored_NewInputSystem>();
                    }
                    return m_TapToPlaceAnchored;
            }
        }
    }
}