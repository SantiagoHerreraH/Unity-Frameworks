using System.Collections.Generic;
using UnityEngine;
using SilverPillar.Core;

namespace SilverPillar.Narrative
{
    public class GoalNode
    {
        public List<Entity> GoalTargets;
        public List<Goal> Goals;
    }

    public class Goal
    {
        public List<SaveableCondition> ConditionsToFulfillGoal = new();
    }

    public class Emotion
    {

    }

    public class EntityPersonality
    {

    }

    public class Entity
    {
        public string Name = "No Name";
        public GameObject GameObject;
        public List<Faction> Factions = new();
        public List<EntityPersonality> EntityPersonalities = new();
        public EntityMind EntityMind = new();
    }

    public class Situation
    {
        public List<Entity> Actors = new();
        public List<Entity> Receivers = new();
        public List<Entity> Perceivers = new();

    }

    public class SituationPerceiverThought //the perceiver can be either the actor, the receiver, or a another person
    {
        public Entity Actor;
        //public List<IAction> ActionsOfActor = new();
        public List<Emotion> EmotionsOfActor = new();

        public Entity Receiver;
        public List<Emotion> EmotionsOfReceiver= new();

        //All emotion calculations are ----> Emotion of the actor Stat modification =  emotion towards actor - emotion towards receiver 
        public List<Emotion> EmotionTowardsActor = new(); // at the time of the situation
        public List<Emotion> EmotionTowardsActorAction = new(); // at the time of the situation -> get it from stat change, not from the action itself

        public List<Emotion> EmotionTowardsReceiver = new(); // at the time of the situation

        public bool IsActor(Entity entity)
        {
            return Actor.GameObject == entity.GameObject;
        }
        public bool IsReceiver(Entity entity)
        {
            return Receiver.GameObject == entity.GameObject;
        }
    }

    public class EntityMemory
    {
        public List<SituationPerceiverThought> SituationPerceiverThoughts = new();
    }

    public class EntityMind //the entity who possesses the mind is the perceiver
    {
        public Dictionary<Entity, EntityMemory> Entities_To_EntityMemories = new();//entities can be either actors or reactors
    }

    //can be families, countries, organizations, etc
    public class Faction
    {
        //The first member is always the leader
        public List<Entity> Members = new();
    }

    

    public class NarrativeGenerationParameters
    {

    }

    public class NarrativeManager : MonoBehaviour
    {
        
    }
}

