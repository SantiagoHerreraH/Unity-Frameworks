using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace SilverPillar.Core
{
    [Serializable]
    public class SetTexture_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private Texture m_Texture;

        [SerializeField]
        private SelfType m_WhichRawImageToSetTextureIn;

        [SerializeField, ShowIf(nameof(m_WhichRawImageToSetTextureIn), SelfType.CustomGameObject)]
        private RawImage m_RawImage;

        private GameObject m_GameObject;

        public ICachedGameAction Clone()
        {
            return new SetTexture_CachedGameAction
            {
                m_Texture = m_Texture,
                m_WhichRawImageToSetTextureIn = m_WhichRawImageToSetTextureIn,
                m_RawImage = m_RawImage,
                m_GameObject = m_GameObject
            };
        }

        public void Execute()
        {
            RawImage rawImage = GetRawImage();

            if (rawImage == null)
                return;

            rawImage.texture = m_Texture;
        }

        public GameObject GetGameObject()
        {
            RawImage rawImage = GetRawImage();

            if (rawImage != null)
                return rawImage.gameObject;

            return m_GameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;

            if (m_WhichRawImageToSetTextureIn == SelfType.CustomGameObject)
                return m_RawImage != null;

            return GetRawImage() != null;
        }

        private RawImage GetRawImage()
        {
            switch (m_WhichRawImageToSetTextureIn)
            {
                case SelfType.ThisGameObject:
                    if (m_GameObject == null)
                        return null;

                    return m_GameObject.GetComponent<RawImage>();

                case SelfType.CustomGameObject:
                    return m_RawImage;

                default:
                    if (m_GameObject == null)
                        return null;

                    return m_GameObject.GetComponent<RawImage>();
            }
        }
    }
}