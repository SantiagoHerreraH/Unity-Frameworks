using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace SilverPillar.Core
{
    [Serializable]
    public class SetSpriteInImage_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private Sprite m_Sprite;

        [SerializeField]
        private SelfType m_WhichRawImageToSetTextureIn;

        [SerializeField, ShowIf(nameof(m_WhichRawImageToSetTextureIn), SelfType.CustomGameObject)]
        private Image m_Image;

        private GameObject m_GameObject;

        public ICachedGameAction Clone()
        {
            return new SetSpriteInImage_CachedGameAction
            {
                m_Sprite = m_Sprite,
                m_WhichRawImageToSetTextureIn = m_WhichRawImageToSetTextureIn,
                m_Image = m_Image,
                m_GameObject = m_GameObject
            };
        }

        public void Execute()
        {
            Image image = GetSpriteRenderer();

            if (image == null)
                return;

            image.sprite = m_Sprite;
        }

        public GameObject GetGameObject()
        {
            Image image = GetSpriteRenderer();

            if (image != null)
                return image.gameObject;

            return m_GameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;

            if (m_WhichRawImageToSetTextureIn == SelfType.CustomGameObject)
                return m_Image != null;

            return GetSpriteRenderer() != null;
        }

        private Image GetSpriteRenderer()
        {
            switch (m_WhichRawImageToSetTextureIn)
            {
                case SelfType.ThisGameObject:
                    if (m_GameObject == null)
                        return null;

                    return m_GameObject.GetComponent<Image>();

                case SelfType.CustomGameObject:
                    return m_Image;

                default:
                    if (m_GameObject == null)
                        return null;

                    return m_GameObject.GetComponent<Image>();
            }
        }
    }
}
