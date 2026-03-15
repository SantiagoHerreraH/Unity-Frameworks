using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace Pillar
{
    //change this based on interaction data
    public class InteractionMachine : MonoBehaviour
    {
        public class InternalInteractionData
        {
            public bool IsInteracting;
            public CollisionInteractionData2D OnBeginCollisionInteractionData = null;
            public CollisionInteractionData2D OnEndCollisionInteractionData = null;
            public List<Coroutine> Coroutines = new();
            public List<Coroutine> OnStayCoroutines = new();
        }

        [Header("Interaction On Touch")]
        [SerializeField, Tooltip("Interaction targets are still registered on touch independently of triggering start interaction")]
        private bool m_StartInteractionOnStartTouch = false;
        [SerializeField]
        private bool m_EndInteractionOnEndTouch = false;

        [Header("Interaction Data")]
        [SerializeField, Tooltip("It takes into account 2d collision and trigger")]
        private List<Interaction2DData> m_Interactions = new();

        [Header("Interaction Events")]
        [SerializeField]
        private UnityEvent<GameObject> m_OnStartInteraction = new(); 
        [SerializeField]
        private UnityEvent<GameObject> m_OnStayInteraction = new();
        [SerializeField]
        private UnityEvent<GameObject> m_OnEndInteraction = new();

        private Dictionary<GameObject, InternalInteractionData> m_Target_To_InteractionData = new();

        private void OnDestroy()
        {
            StopAllInteractions();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            CollisionInteractionData2D data = new();
            collision.GetContacts(data.ContactPoints);
            StartInteraction(collision.gameObject, data, true);

        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            CollisionInteractionData2D data = new();
            collision.GetContacts(data.ContactPoints);
            StartInteraction(collision.gameObject, data, true);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            CollisionInteractionData2D data = new();
            collision.GetContacts(data.ContactPoints);
            EndInteraction(collision.gameObject, data, true, true);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            CollisionInteractionData2D data = new();
            collision.GetContacts(data.ContactPoints);
            EndInteraction(collision.gameObject, data, true, true);
        }

        public bool IsInteracting(GameObject target)
        {
            if (!m_Target_To_InteractionData.ContainsKey(target))
            {
                return false;
            }

            return m_Target_To_InteractionData[target].IsInteracting;
        }

        public void StartInteraction(GameObject target)
        {
            CollisionInteractionData2D collisionData = null;

            if (!m_Target_To_InteractionData.ContainsKey(target))
            {
                collisionData = m_Target_To_InteractionData[target].OnBeginCollisionInteractionData;
            }

            StartInteraction(target, collisionData, false);
        }

        public void EndInteraction(GameObject target)
        {
            CollisionInteractionData2D collisionData = null;

            if (!m_Target_To_InteractionData.ContainsKey(target))
            {
                collisionData = m_Target_To_InteractionData[target].OnEndCollisionInteractionData;
            }

            EndInteraction(target, collisionData, true, false);
        }

        private void StartInteraction(GameObject target, CollisionInteractionData2D collisionData, bool isCalledFromCollision)
        {
            if (!m_Target_To_InteractionData.ContainsKey(target))
            {
                m_Target_To_InteractionData.Add(target, new());
            }

            InternalInteractionData interactionData = m_Target_To_InteractionData[target];
            interactionData.IsInteracting = true;
            interactionData.OnBeginCollisionInteractionData = collisionData;

            if (!isCalledFromCollision || isCalledFromCollision == m_StartInteractionOnStartTouch)
            {
                m_OnStartInteraction.Invoke(target);
                Interact(target, collisionData, interactionData, InteractionType.StartInteraction);
                StartOnStayInteractions(target, collisionData, interactionData);
            }
        }

        private void EndInteraction(GameObject target, CollisionInteractionData2D collisionData, bool alsoEndOnStayInteractions, bool isCalledFromCollision)
        {
            if (!m_Target_To_InteractionData.ContainsKey(target))
            {
                return;
            }

            InternalInteractionData interactionData = m_Target_To_InteractionData[target];
            interactionData.OnEndCollisionInteractionData = collisionData;

            if (!isCalledFromCollision || isCalledFromCollision == m_EndInteractionOnEndTouch)
            {
                if (alsoEndOnStayInteractions)
                {
                    StopOnStayInteractions(target);
                }

                Interact(target, collisionData, interactionData, InteractionType.StartInteraction);

                m_OnEndInteraction.Invoke(target);

                interactionData.IsInteracting = false;
            }

            //for now won't remove target upon end interaction
        }

        private void Interact(GameObject target, CollisionInteractionData2D collisionData, InternalInteractionData interactionData, InteractionType interactionType)
        {
            foreach (var interactionTypeData in m_Interactions)
            {
                if (interactionTypeData.InteractionTypeData.InteractionType == interactionType)
                {
                    foreach (var interaction in interactionTypeData.Interactable2DScriptableObjects)
                    {
                        switch (interactionTypeData.InteractionTypeData.InteractionLifetimeDependsOn)
                        {
                            case TargetType.Self:
                                interactionData.Coroutines.Add(StartCoroutine(interaction.Interact(gameObject, target, collisionData)));

                                break;
                            case TargetType.Other:

                                InteractionMachine interactionOnTouch = target.GetComponent<InteractionMachine>();
                                if (interactionOnTouch == null)
                                {
                                    interactionOnTouch = target.AddComponent<InteractionMachine>();
                                }
                                interactionOnTouch.InteractWhereSelfIsTarget(gameObject, interaction, collisionData, interactionTypeData.InteractionTypeData.InteractionType);

                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        private void InteractWhereSelfIsTarget(GameObject other, IInteract2D interaction, CollisionInteractionData2D collisionData, InteractionType interactionType)
        {
            if (!m_Target_To_InteractionData.ContainsKey(other))
            {
                m_Target_To_InteractionData.Add(other, new());
            }

            var interactionData = m_Target_To_InteractionData[other];

            switch (interactionType)
            {
                case InteractionType.StartInteraction:
                case InteractionType.EndInteraction:
                    interactionData.Coroutines.Add(StartCoroutine(interaction.Interact(other, gameObject, collisionData)));

                    break;
                case InteractionType.StayInteraction:
                    interactionData.OnStayCoroutines.Add(StartCoroutine(interaction.Interact(other, gameObject, collisionData)));

                    break;
                default:
                    break;
            }
        }

        private void StartOnStayInteractions(GameObject target, CollisionInteractionData2D data,  InternalInteractionData internalInteractionData)
        {
            foreach (var interactionData in m_Interactions)
            {
                if (interactionData.InteractionTypeData.InteractionType == InteractionType.StayInteraction)
                {
                    switch (interactionData.InteractionTypeData.InteractionLifetimeDependsOn)
                    {
                        case TargetType.Self:

                            foreach (var interaction in interactionData.Interactable2DScriptableObjects)
                            {
                                internalInteractionData.OnStayCoroutines.Add(StartCoroutine(StartOnStayInteractions(target, data, interactionData, internalInteractionData)));
                            }


                            break;
                        case TargetType.Other:

                            foreach (var interaction in interactionData.Interactable2DScriptableObjects)
                            {
                                InteractionMachine interactionOnTouch = target.GetComponent<InteractionMachine>(); 
                                if (interactionOnTouch == null)
                                {
                                    interactionOnTouch = target.AddComponent<InteractionMachine>();
                                }
                                interactionOnTouch.InteractWhereSelfIsTarget(gameObject, interaction, data, InteractionType.StayInteraction);
                            }

                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void StopOnStayInteractions(GameObject target)
        {
            if (!m_Target_To_InteractionData.ContainsKey(target))
            {
                return;
            }
            var data = m_Target_To_InteractionData[target];
            foreach (var onStayCoroutine in data.OnStayCoroutines)
            {
                StopCoroutine(onStayCoroutine);
            }

            data.OnStayCoroutines.Clear();

            if (target == gameObject)
            {
                return;
            }

            InteractionMachine interactionOnTouch = target.GetComponent<InteractionMachine>();

            if (interactionOnTouch)
            {
                interactionOnTouch.StopOnStayInteractions(gameObject);
            }
        }

        private void StopAllInteractions()
        {
            var data = m_Target_To_InteractionData.Values;

            foreach (var item in data)
            {
                foreach (var onStayCoroutine in item.OnStayCoroutines)
                {
                    StopCoroutine(onStayCoroutine);
                }

                foreach (var coroutine in item.Coroutines)
                {
                    StopCoroutine(coroutine);
                }

                item.OnStayCoroutines.Clear();
                item.Coroutines.Clear();    
            }
        }

        private IEnumerator StartOnStayInteractions(GameObject target, CollisionInteractionData2D data, Interaction2DData interactionData, InternalInteractionData internalInteractionData)
        {
            float seconds = interactionData.InteractionTypeData.SecondsBetweenStayInteractionInteraction;

            do
            {
                switch (interactionData.InteractionTypeData.InteractionLifetimeDependsOn)
                {
                    case TargetType.Self:

                        foreach (var interaction in interactionData.Interactable2DScriptableObjects)
                        {
                            internalInteractionData.OnStayCoroutines.Add(StartCoroutine(interaction.Interact(gameObject, target, data)));
                        }


                        break;
                    case TargetType.Other:

                        foreach (var interaction in interactionData.Interactable2DScriptableObjects)
                        {
                            InteractionMachine interactionOnTouch = target.GetComponent<InteractionMachine>();
                            if (interactionOnTouch == null)
                            {
                                interactionOnTouch = target.AddComponent<InteractionMachine>();
                            }
                            interactionOnTouch.InteractWhereSelfIsTarget(gameObject, interaction, data, InteractionType.StayInteraction);
                        }


                        break;
                    default:
                        break;
                }

                m_OnStayInteraction.Invoke(target);

                yield return new WaitForSeconds(seconds);

            } while (internalInteractionData.IsInteracting);
        }
    }
}


