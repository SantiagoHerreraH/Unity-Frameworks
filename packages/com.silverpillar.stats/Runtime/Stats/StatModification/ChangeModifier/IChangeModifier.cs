
using SilverPillar.Core;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{
    public interface IChangeModifier
    {
        public bool ChangeModifier(IStatModifier modifier);
    }

}