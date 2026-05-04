using UnityEngine;

namespace SilverPillar.Core
{
    public class TransformTools: MonoBehaviour
    {
        private Vector3 m_AwakeWorldPosition;
        private Vector3 m_AwakeLocalPosition;

        private void Awake()
        {
            m_AwakeLocalPosition = transform.localPosition;
            m_AwakeWorldPosition = transform.position;
        }

        public void CopyWorldTransform(GameObject from)
        {
            Transform t = GetTransform(from);
            if (t == null) return;

            CopyWorldPosition(from);
            CopyWorldRotation(from);
            CopyWorldScale(from);
        }

        public void CopyWorldPosition(GameObject from)
        {
            Transform t = GetTransform(from);
            if (t == null) return;

            transform.position = t.position;
        }

        public void CopyWorldRotation(GameObject from)
        {
            Transform t = GetTransform(from);
            if (t == null) return;

            transform.rotation = t.rotation;
        }

        public void CopyWorldScale(GameObject from)
        {
            Transform t = GetTransform(from);
            if (t == null) return;

            SetWorldScale(t.lossyScale);
        }

        public void CopyLocalTransform(GameObject from)
        {
            Transform t = GetTransform(from);
            if (t == null) return;

            CopyLocalPosition(from);
            CopyLocalRotation(from);
            CopyLocalScale(from);
        }

        public void CopyLocalPosition(GameObject from)
        {
            Transform t = GetTransform(from);
            if (t == null) return;

            transform.localPosition = t.localPosition;
        }

        public void CopyLocalRotation(GameObject from)
        {
            Transform t = GetTransform(from);
            if (t == null) return;

            transform.localRotation = t.localRotation;
        }

        public void CopyLocalScale(GameObject from)
        {
            Transform t = GetTransform(from);
            if (t == null) return;

            transform.localScale = t.localScale;
        }

        public void SetAwakeWorldPosition()
        {
            transform.position = m_AwakeWorldPosition;
        }

        public void SetAwakeLocalPosition()
        {
            transform.localPosition = m_AwakeLocalPosition;
        }

        public void MakeRoot()
        {
            transform.SetParent(null);
        }

        // -------------------------
        // Helpers
        // -------------------------

        private Transform GetTransform(GameObject go)
        {
            return go != null ? go.transform : null;
        }

        private void SetWorldScale(Vector3 targetWorldScale)
        {
            Transform parent = transform.parent;

            if (parent == null)
            {
                transform.localScale = targetWorldScale;
                return;
            }

            Vector3 parentScale = parent.lossyScale;

            transform.localScale = new Vector3(
                SafeDivide(targetWorldScale.x, parentScale.x),
                SafeDivide(targetWorldScale.y, parentScale.y),
                SafeDivide(targetWorldScale.z, parentScale.z)
            );
        }

        private float SafeDivide(float value, float divisor)
        {
            return Mathf.Approximately(divisor, 0f) ? 0f : value / divisor;
        }
    }
}