using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Core
{
    [Serializable]
    public class ShootRaycast_CachedGameAction : ICachedGameAction
    {
        public enum RaycastType
        {
            Raycast,
            RaycastAll,
            RaycastNonAlloc
        }

        public enum WhereToGetLocationFrom
        {
            Transform,
            Position
        }

        public enum WhereToShoot
        {
            Transform,
            Position,
            VectorOffset,
            VectorDirection,
            RotationDirection
        }

        [Serializable]
        public struct WhereToGetTransformDataFrom
        {
            public SelfType WhereToGetTransformData;

            [ShowIf(nameof(WhereToGetTransformData), SelfType.CustomGameObject)]
            public GameObject GameObject;
        }

        [Serializable]
        public struct DirectionData
        {
            public Vector3 Vector;

            [OdinSerialize, ShowInInspector]
            public ICachedScore Distance;

            public DirectionData Clone()
            {
                return new DirectionData
                {
                    Vector = Vector,
                    Distance = Distance?.Clone()
                };
            }

            public bool SetGameObject(GameObject gameObject)
            {
                if (Distance == null)
                    return false;

                return Distance.SetGameObject(gameObject);
            }

            public float CalculateDistance()
            {
                if (Distance == null)
                    return 0f;

                return Distance.CalculateScore();
            }
        }

        [Serializable]
        public struct PositionData
        {
            public Space Space;
            public Vector3 Position;

            [ShowIf(nameof(Space), Space.Self)]
            public WhereToGetTransformDataFrom WhereToGetLocalPositionFromData;
        }

        [Serializable]
        public struct TransformationData
        {
            public Space TransformationSpace;
            [ShowIf(nameof(TransformationSpace), Space.Self)]
            public WhereToGetTransformDataFrom Source;
        }


        [Title("Settings")]
        [SerializeField] private LayerMask m_LayerMask = ~0;
        [SerializeField] private RaycastType m_RaycastType = RaycastType.Raycast;
        [SerializeField] private QueryTriggerInteraction m_QueryTriggerInteraction = QueryTriggerInteraction.UseGlobal;

        [ShowIf(nameof(m_RaycastType), RaycastType.RaycastNonAlloc)]
        [SerializeField, Min(1)] private int m_NonAllocSize = 8;

        [Title("From")]
        [SerializeField] private WhereToGetLocationFrom m_From;

        [SerializeField, ShowIf(nameof(m_From), WhereToGetLocationFrom.Position)]
        private PositionData m_FromPosition;

        [SerializeField, ShowIf(nameof(m_From), WhereToGetLocationFrom.Transform)]
        private WhereToGetTransformDataFrom m_FromTransform;

        [Title("To")]
        [SerializeField] private WhereToShoot m_WhereToShoot;

        [SerializeField, ShowIf(nameof(m_WhereToShoot), WhereToShoot.Transform)]
        private WhereToGetTransformDataFrom m_ToTransform;

        [SerializeField, ShowIf(nameof(m_WhereToShoot), WhereToShoot.Position)]
        private PositionData m_ToPosition;

        [SerializeField, ShowIf(nameof(m_WhereToShoot), WhereToShoot.VectorOffset)]
        private Vector3 m_VectorOffset = Vector3.forward;

        [SerializeField, ShowIf(nameof(m_WhereToShoot), WhereToShoot.VectorDirection)]
        private DirectionData m_VectorDirection = new DirectionData
        {
            Vector = Vector3.forward,
            Distance = new Constant_CachedScore(100f)
        };

        [SerializeField, ShowIf(nameof(m_WhereToShoot), WhereToShoot.RotationDirection)]
        private DirectionData m_LocalRotationDirection = new DirectionData
        {
            Vector = Vector3.forward,
            Distance = new Constant_CachedScore(100f)
        };


        [SerializeField, HideIf(nameof(m_WhereToShoot), WhereToShoot.Transform)]
        private TransformationData m_TransformationData;


        [Title("Raycast Actions")]
        [OdinSerialize, ShowInInspector]
        private List<IRaycastAction> m_RaycastActions;

        [Title("Events")]
        [SerializeField] private UnityEvent<RaycastHit> m_OnHit;
        [SerializeField] private UnityEvent<GameObject> m_OnHitGameObject;
        [SerializeField] private UnityEvent m_OnMiss;

        private GameObject m_GameObject;
        private RaycastHit[] m_NonAllocHits;

        public RaycastHit? LastHit { get; private set; }
        public RaycastHit[] LastHits { get; private set; }
        public int LastHitCount { get; private set; }

        private float m_MaxDistance = 100f;

        public ICachedGameAction Clone()
        {
            List<IRaycastAction> clonedActions = null;

            if (m_RaycastActions != null)
            {
                clonedActions = new List<IRaycastAction>(m_RaycastActions.Count);

                for (int i = 0; i < m_RaycastActions.Count; i++)
                {
                    clonedActions.Add(m_RaycastActions[i]?.Clone());
                }
            }

            return new ShootRaycast_CachedGameAction
            {
                m_LayerMask = m_LayerMask,
                m_RaycastType = m_RaycastType,
                m_QueryTriggerInteraction = m_QueryTriggerInteraction,
                m_NonAllocSize = m_NonAllocSize,

                m_From = m_From,
                m_FromPosition = m_FromPosition,
                m_FromTransform = m_FromTransform,

                m_WhereToShoot = m_WhereToShoot,
                m_ToTransform = m_ToTransform,
                m_ToPosition = m_ToPosition,
                m_VectorOffset = m_VectorOffset,
                m_VectorDirection = m_VectorDirection.Clone(),
                m_LocalRotationDirection = m_LocalRotationDirection.Clone(),
                m_TransformationData = m_TransformationData,

                m_RaycastActions = clonedActions,

                m_OnHit = m_OnHit,
                m_OnHitGameObject = m_OnHitGameObject,
                m_OnMiss = m_OnMiss,

                m_GameObject = m_GameObject,
                m_MaxDistance = m_MaxDistance
            };
        }

        public void Execute()
        {
            LastHit = null;
            LastHits = Array.Empty<RaycastHit>();
            LastHitCount = 0;

            Vector3 origin = GetOrigin();
            Vector3 direction = GetDirectionAndUpdateDistance(origin);

            if (direction.sqrMagnitude <= Mathf.Epsilon || m_MaxDistance <= 0f)
            {
                m_OnMiss?.Invoke();
                return;
            }

            direction.Normalize();

            switch (m_RaycastType)
            {
                case RaycastType.Raycast:
                    ExecuteSingleRaycast(origin, direction);
                    break;

                case RaycastType.RaycastAll:
                    ExecuteRaycastAll(origin, direction);
                    break;

                case RaycastType.RaycastNonAlloc:
                    ExecuteRaycastNonAlloc(origin, direction);
                    break;
            }
        }

        private void ExecuteRaycastActions()
        {
            if (m_RaycastActions == null)
                return;

            for (int i = 0; i < m_RaycastActions.Count; i++)
            {
                if (m_RaycastActions[i] == null)
                    continue;

                m_RaycastActions[i].Execute(LastHits);
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

            bool result = true;

            if (m_VectorDirection.Distance != null)
                result &= m_VectorDirection.SetGameObject(gameObj);

            if (m_LocalRotationDirection.Distance != null)
                result &= m_LocalRotationDirection.SetGameObject(gameObj);

            if (m_RaycastActions != null)
            {
                for (int i = 0; i < m_RaycastActions.Count; i++)
                {
                    if (m_RaycastActions[i] == null)
                        continue;

                    result &= m_RaycastActions[i].SetGameObject(gameObj);
                }
            }

            return result;
        }

        private void ExecuteSingleRaycast(Vector3 origin, Vector3 direction)
        {
            if (Physics.Raycast(
                    origin,
                    direction,
                    out RaycastHit hit,
                    m_MaxDistance,
                    m_LayerMask,
                    m_QueryTriggerInteraction))
            {
                LastHit = hit;
                LastHits = new[] { hit };
                LastHitCount = 1;

                InvokeHit(hit);
                ExecuteRaycastActions();
            }
            else
            {
                LastHits = Array.Empty<RaycastHit>();
                m_OnMiss?.Invoke();
                ExecuteRaycastActions();
            }
        }
        private void ExecuteRaycastAll(Vector3 origin, Vector3 direction)
        {
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                direction,
                m_MaxDistance,
                m_LayerMask,
                m_QueryTriggerInteraction);

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            LastHits = hits;
            LastHitCount = hits.Length;

            if (hits.Length == 0)
            {
                m_OnMiss?.Invoke();
                ExecuteRaycastActions();
                return;
            }

            LastHit = hits[0];

            foreach (RaycastHit hit in hits)
                InvokeHit(hit);

            ExecuteRaycastActions();
        }

        private void ExecuteRaycastNonAlloc(Vector3 origin, Vector3 direction)
        {
            if (m_NonAllocHits == null || m_NonAllocHits.Length != m_NonAllocSize)
                m_NonAllocHits = new RaycastHit[m_NonAllocSize];

            int count = Physics.RaycastNonAlloc(
                origin,
                direction,
                m_NonAllocHits,
                m_MaxDistance,
                m_LayerMask,
                m_QueryTriggerInteraction);

            LastHitCount = count;

            if (count == 0)
            {
                LastHits = Array.Empty<RaycastHit>();
                m_OnMiss?.Invoke();
                ExecuteRaycastActions();
                return;
            }

            LastHits = new RaycastHit[count];
            Array.Copy(m_NonAllocHits, LastHits, count);

            Array.Sort(LastHits, (a, b) => a.distance.CompareTo(b.distance));

            LastHit = LastHits[0];

            for (int i = 0; i < LastHits.Length; i++)
                InvokeHit(LastHits[i]);

            ExecuteRaycastActions();
        }

        private Vector3 GetOrigin()
        {
            return m_From switch
            {
                WhereToGetLocationFrom.Transform => GetTransformPosition(m_FromTransform),
                WhereToGetLocationFrom.Position => GetPosition(m_FromPosition),
                _ => Vector3.zero
            };
        }
        private Vector3 GetDirectionAndUpdateDistance(Vector3 origin)
        {
            Vector3 delta;

            switch (m_WhereToShoot)
            {
                case WhereToShoot.Transform:
                    delta = GetTransformPosition(m_ToTransform) - origin;
                    m_MaxDistance = delta.magnitude;
                    return delta;

                case WhereToShoot.Position:
                    Vector3 targetPosition = ApplyTransformationToPosition(m_ToPosition.Position);
                    delta = targetPosition - origin;
                    m_MaxDistance = delta.magnitude;
                    return delta;

                case WhereToShoot.VectorOffset:
                    Vector3 offset = ApplyTransformationToVector(m_VectorOffset);
                    m_MaxDistance = offset.magnitude;
                    return offset;

                case WhereToShoot.VectorDirection:
                    Vector3 direction = ApplyTransformationToDirection(m_VectorDirection.Vector);
                    m_MaxDistance = m_VectorDirection.CalculateDistance();
                    return direction;

                case WhereToShoot.RotationDirection:
                    Vector3 rotationDirection = ApplyTransformationToDirection(m_LocalRotationDirection.Vector);
                    m_MaxDistance = m_LocalRotationDirection.CalculateDistance();
                    return rotationDirection;

                default:
                    m_MaxDistance = 0f;
                    return Vector3.zero;
            }
        }

        private Vector3 ApplyTransformationToPosition(Vector3 position)
        {
            if (m_TransformationData.TransformationSpace == Space.World)
                return position;

            Transform source = GetTransformationSourceTransform();

            if (source == null)
                return position;

            return source.TransformPoint(position);
        }

        private Vector3 ApplyTransformationToVector(Vector3 vector)
        {
            if (m_TransformationData.TransformationSpace == Space.World)
                return vector;

            Transform source = GetTransformationSourceTransform();

            if (source == null)
                return vector;

            return source.TransformVector(vector);
        }

        private Vector3 ApplyTransformationToDirection(Vector3 direction)
        {
            if (m_TransformationData.TransformationSpace == Space.World)
                return direction;

            Transform source = GetTransformationSourceTransform();

            if (source == null)
                return direction;

            return source.TransformDirection(direction);
        }

        private Transform GetTransformationSourceTransform()
        {
            GameObject source = GetGameObjectFromData(m_TransformationData.Source);
            return source != null ? source.transform : null;
        }

        private Vector3 GetPosition(PositionData data)
        {
            if (data.Space == Space.World)
                return data.Position;

            GameObject target = GetGameObjectFromData(data.WhereToGetLocalPositionFromData);

            if (target == null)
                return data.Position;

            return target.transform.TransformPoint(data.Position);
        }

        private Vector3 GetTransformPosition(WhereToGetTransformDataFrom data)
        {
            GameObject target = GetGameObjectFromData(data);

            if (target == null)
                return Vector3.zero;

            return target.transform.position;
        }

        private GameObject GetGameObjectFromData(WhereToGetTransformDataFrom data)
        {
            return data.WhereToGetTransformData switch
            {
                SelfType.ThisGameObject => m_GameObject,
                SelfType.CustomGameObject => data.GameObject,
                _ => null
            };
        }

        private void InvokeHit(RaycastHit hit)
        {
            m_OnHit?.Invoke(hit);

            if (hit.collider != null)
                m_OnHitGameObject?.Invoke(hit.collider.gameObject);
        }
    }
}