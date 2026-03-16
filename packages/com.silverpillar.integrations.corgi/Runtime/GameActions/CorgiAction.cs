using MoreMountains.Tools;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SilverPillar.Core;

namespace SilverPillar.Integrations.Corgi
{
    public class CorgiAction : IGameAction
    {
        [OdinSerialize, ValueDropdown(nameof(EnemyAITypeDropdown))]
        private Type m_CorgiActionType;

        private static IEnumerable<ValueDropdownItem<Type>> EnemyAITypeDropdown()
        {
            foreach (var t in TypeCache.GetTypesDerivedFrom<AIAction>())
            {
                if (t.IsAbstract || t.IsGenericType) continue;

                // Nice label in the dropdown
                yield return new ValueDropdownItem<Type>(t.FullName, t);
            }
        }
        public bool CanExecute(GameObject gameObj)
        {
            return gameObj.GetComponent(m_CorgiActionType) != null;
        }

        public IEnumerator Execute(GameObject gameObj)
        {
            var corgiAction = gameObj.GetComponent(m_CorgiActionType) as AIAction;

            corgiAction.Initialization();
            corgiAction.PerformAction();

            yield return null;
        }
    }
}

