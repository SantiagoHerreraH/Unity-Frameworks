using MoreMountains.CorgiEngine;
using SilverPillar.Stats;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SilverPillar.Integrations.Corgi
{
    public class ChargeWeaponVariables : WeaponVariables
    {
        [BoxGroup("References")]
        private ChargeWeapon m_ChargeWeapon;


        [BoxGroup("Charge Weapon")]
        private StatValueRange m_ChargeDuration;

    }
}
