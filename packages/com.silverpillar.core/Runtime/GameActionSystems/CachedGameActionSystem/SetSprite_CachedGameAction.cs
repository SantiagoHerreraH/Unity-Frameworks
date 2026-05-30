using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class SetSprite_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private Sprite m_Sprite;

        [SerializeField]
        private SelfType m_WhichRawImageToSetTextureIn;

        [SerializeField, ShowIf(nameof(m_WhichRawImageToSetTextureIn), SelfType.CustomGameObject)]
        private SpriteRenderer m_SpriteRenderer;

        private GameObject m_GameObject;

        public ICachedGameAction Clone()
        {
            return new SetSprite_CachedGameAction
            {
                m_Sprite = m_Sprite,
                m_WhichRawImageToSetTextureIn = m_WhichRawImageToSetTextureIn,
                m_SpriteRenderer = m_SpriteRenderer,
                m_GameObject = m_GameObject
            };
        }

        public void Execute()
        {
            SpriteRenderer spriteRenderer = GetSpriteRenderer();

            if (spriteRenderer == null)
                return;

            spriteRenderer.sprite = m_Sprite;
        }

        public GameObject GetGameObject()
        {
            SpriteRenderer spriteRenderer = GetSpriteRenderer();

            if (spriteRenderer != null)
                return spriteRenderer.gameObject;

            return m_GameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;

            if (m_WhichRawImageToSetTextureIn == SelfType.CustomGameObject)
                return m_SpriteRenderer != null;

            return GetSpriteRenderer() != null;
        }

        private SpriteRenderer GetSpriteRenderer()
        {
            switch (m_WhichRawImageToSetTextureIn)
            {
                case SelfType.ThisGameObject:
                    if (m_GameObject == null)
                        return null;

                    return m_GameObject.GetComponent<SpriteRenderer>();

                case SelfType.CustomGameObject:
                    return m_SpriteRenderer;

                default:
                    if (m_GameObject == null)
                        return null;

                    return m_GameObject.GetComponent<SpriteRenderer>();
            }
        }
    }
}