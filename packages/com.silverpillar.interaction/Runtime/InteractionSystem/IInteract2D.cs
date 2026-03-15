using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pillar
{
    public enum TargetType
    {
        Other,
        Self
    }

    [Serializable]
    public class CollisionInteractionData2D
    {
        public ContactPoint2D[] ContactPoints;
    }

    public enum InteractionType
    {
        StartInteraction,
        StayInteraction,
        EndInteraction
    }

    [Serializable]
    public class InteractionTypeData
    {
        public TargetType InteractionLifetimeDependsOn;
        public InteractionType InteractionType;
        [EnableIf("InteractionType", InteractionType.StayInteraction)]
        public float SecondsBetweenStayInteractionInteraction = 1;
        [EnableIf("InteractionType", InteractionType.StayInteraction)]
        public bool EndInteractionWhenExitingTouch = true;
    }

    public interface IInteract2D
    {
        public IEnumerator Interact(GameObject self, GameObject other, CollisionInteractionData2D collisionData);

    }

    public abstract class Interactable2DScriptableObject : SaveableScriptableObject, IInteract2D
    {
        public abstract IEnumerator Interact(GameObject self, GameObject other, CollisionInteractionData2D collisionData);
    }

    [Serializable]
    public class Interaction2DData
    {
        public InteractionTypeData InteractionTypeData;
        public List<Interactable2DScriptableObject> Interactable2DScriptableObjects;
    }

}

