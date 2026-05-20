using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class CreateLine_RaycastAction : IRaycastAction
    {
        [Title("References")]
        [SerializeField]
        private SelfType m_LineRendererFrom;

        [SerializeField, ShowIf(nameof(m_LineRendererFrom), SelfType.CustomGameObject)]
        private LineRenderer m_LineRenderer;

        private GameObject m_GameObject;

        public IRaycastAction Clone()
        {
            return new CreateLine_RaycastAction
            {
                m_LineRendererFrom = m_LineRendererFrom,
                m_LineRenderer = m_LineRenderer,
                m_GameObject = m_GameObject
            };
        }

        public void Execute(RaycastHit[] hits)
        {
            LineRenderer lineRenderer = GetLineRenderer();

            if (lineRenderer == null)
                return;

            if (hits == null || hits.Length == 0)
            {
                lineRenderer.positionCount = 0;
                return;
            }

            lineRenderer.positionCount = hits.Length;

            for (int i = 0; i < hits.Length; i++)
            {
                lineRenderer.SetPosition(i, hits[i].point);
            }
        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null)
                return false;

            m_GameObject = gameObj;

            if (m_LineRendererFrom == SelfType.ThisGameObject)
                return GetLineRenderer() != null;

            return true;
        }

        private LineRenderer GetLineRenderer()
        {
            switch (m_LineRendererFrom)
            {
                case SelfType.ThisGameObject:
                    if (m_GameObject == null)
                        return null;

                    return m_GameObject.GetComponent<LineRenderer>();

                case SelfType.CustomGameObject:
                    return m_LineRenderer;

                default:
                    return null;
            }
        }
    }
}
