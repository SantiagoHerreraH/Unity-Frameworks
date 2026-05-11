using MoreMountains.CorgiEngine;
using SilverPillar.Modules;
using SilverPillar.Stats;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.Corgi
{
    [Serializable]
    public class RecoilVariables
    {
        public BoolModuleType ApplyRecoil;
        public StatValueRange RecoilForceGrounded;
        public StatValueRange RecoilForceAirborne;
    }

    public class WeaponVariables : MonoBehaviour
    {
        [BoxGroup("References")]
        [SerializeField, HideIf("@this.GetType() != typeof(WeaponVariables)"), Tooltip("If null will try to get from self")]
        private Weapon m_Weapon;
        [BoxGroup("References")]
        [SerializeField, Tooltip("If null will try to get from self")]
        protected StatController m_StatController;
        [BoxGroup("References")]
        [SerializeField, Tooltip("If null will try to get from self")]
        protected BoolModuleController m_BoolModuleController;

        [BoxGroup("Activation")]
        [SerializeField]
        private BoolModuleType m_AutoTriggerMode;
        [BoxGroup("Activation")]
        [SerializeField]
        private BoolModuleType m_Interruptable;
        [BoxGroup("Activation")]
        [SerializeField]
        private StatValueRange m_DelayBeforeUse = new();
        [BoxGroup("Activation")]
        [SerializeField]
        private StatValueRange m_TimeBetweenUses = new();
        [BoxGroup("Activation")]
        [SerializeField]
        private StatValueRange m_CooldownDuration = new();

        [BoxGroup("Burst Mode")]
        [SerializeField]
        private BoolModuleType m_UseBurstMode;
        [BoxGroup("Burst Mode")]
        [SerializeField]
        private StatValueRange m_BurstLength = new();
        [BoxGroup("Burst Mode")]
        [SerializeField]
        private StatValueRange m_BurstTimeBetweenShots = new();

        [BoxGroup("Magazine Based")]
        [SerializeField]
        private BoolModuleType m_MagazineBased;
        [BoxGroup("Magazine Based")]
        [SerializeField]
        private BoolModuleType m_AutoReload;
        [BoxGroup("Magazine Based")]
        [SerializeField]
        private StatValueRange m_MagazineSize = new();
        [BoxGroup("Magazine Based")]
        [SerializeField]
        private StatValueRange m_ReloadTime = new();
        [BoxGroup("Magazine Based")]
        [SerializeField]
        private StatValueRange m_AmmoConsumedPerShot = new();

        [BoxGroup("Movement Modifiers")]
        [SerializeField]
        private BoolModuleType m_ModifyMovementWhileEquipped;
        [BoxGroup("Movement Modifiers")]
        [SerializeField]
        private StatValueRange m_MovementModifierWhileEquipped;
        [BoxGroup("Movement Modifiers")]
        [SerializeField]
        private BoolModuleType m_ModifyMovementWhileAttacking;
        [BoxGroup("Movement Modifiers")]
        [SerializeField]
        private StatValueRange m_MovementModifierWhileAttacking;
        [BoxGroup("Movement Modifiers")]
        [SerializeField]
        private BoolModuleType m_DisableGravityWhileInUse;

        [BoxGroup("Recoil")]
        [SerializeField]
        private RecoilVariables m_RecoilOnUse;
        [BoxGroup("Recoil")]
        [SerializeField]
        private RecoilVariables m_RecoilOnHitDamageable;
        [BoxGroup("Recoil")]
        [SerializeField]
        private RecoilVariables m_RecoilOnHitNonDamageable;
        [BoxGroup("Recoil")]
        [SerializeField]
        private RecoilVariables m_RecoilOnMiss;
        [BoxGroup("Recoil")]
        [SerializeField]
        private RecoilVariables m_RecoilOnKill;

        protected void Awake()
        {
            if (ReferencesAreCorrect())
            {
                CreateVariables();
                SubscribeToEvents();
            }
        }

        protected bool ReferencesAreCorrect()
        {
            bool allGood = true;

            if (m_Weapon == null)
            {
                if (!gameObject.TryGetComponent(out m_Weapon))
                {
                    Debug.LogError($"{nameof(m_Weapon)} is NULL in {nameof(WeaponVariables)} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }

            if (m_StatController == null)
            {
                if (!gameObject.TryGetComponent(out m_StatController))
                {
                    Debug.LogError($"{nameof(m_StatController)} is NULL in {nameof(WeaponVariables)} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }

            if (m_BoolModuleController == null)
            {
                if (!gameObject.TryGetComponent(out m_BoolModuleController))
                {
                    Debug.LogError($"{nameof(m_BoolModuleController)} is NULL in {nameof(WeaponVariables)} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }

            // Activation
            allGood &= IsStatTypeValid(m_DelayBeforeUse.StatTypeToGetValueFrom.Get(), nameof(m_DelayBeforeUse));
            allGood &= IsStatTypeValid(m_TimeBetweenUses.StatTypeToGetValueFrom.Get(), nameof(m_TimeBetweenUses));
            allGood &= IsStatTypeValid(m_CooldownDuration.StatTypeToGetValueFrom.Get(), nameof(m_CooldownDuration));
            allGood &= IsBoolModuleValid(m_AutoTriggerMode, nameof(m_AutoTriggerMode));
            allGood &= IsBoolModuleValid(m_Interruptable, nameof(m_Interruptable));

            // Burst Mode
            allGood &= IsStatTypeValid(m_BurstLength.StatTypeToGetValueFrom.Get(), nameof(m_BurstLength));
            allGood &= IsStatTypeValid(m_BurstTimeBetweenShots.StatTypeToGetValueFrom.Get(), nameof(m_BurstTimeBetweenShots));
            allGood &= IsBoolModuleValid(m_UseBurstMode, nameof(m_UseBurstMode));

            // Magazine Based
            allGood &= IsStatTypeValid(m_MagazineSize.StatTypeToGetValueFrom.Get(), nameof(m_MagazineSize));
            allGood &= IsStatTypeValid(m_ReloadTime.StatTypeToGetValueFrom.Get(), nameof(m_ReloadTime));
            allGood &= IsStatTypeValid(m_AmmoConsumedPerShot.StatTypeToGetValueFrom.Get(), nameof(m_AmmoConsumedPerShot));
            allGood &= IsBoolModuleValid(m_MagazineBased, nameof(m_MagazineBased));
            allGood &= IsBoolModuleValid(m_AutoReload, nameof(m_AutoReload));

            // Movement Modifiers
            allGood &= IsStatTypeValid(m_MovementModifierWhileEquipped.StatTypeToGetValueFrom.Get(), nameof(m_MovementModifierWhileEquipped));
            allGood &= IsStatTypeValid(m_MovementModifierWhileAttacking.StatTypeToGetValueFrom.Get(), nameof(m_MovementModifierWhileAttacking));
            allGood &= IsBoolModuleValid(m_ModifyMovementWhileEquipped, nameof(m_ModifyMovementWhileEquipped));
            allGood &= IsBoolModuleValid(m_ModifyMovementWhileAttacking, nameof(m_ModifyMovementWhileAttacking));
            allGood &= IsBoolModuleValid(m_DisableGravityWhileInUse, nameof(m_DisableGravityWhileInUse));

            // Recoil
            allGood &= IsRecoilVariablesValid(m_RecoilOnUse, nameof(m_RecoilOnUse));
            allGood &= IsRecoilVariablesValid(m_RecoilOnHitDamageable, nameof(m_RecoilOnHitDamageable));
            allGood &= IsRecoilVariablesValid(m_RecoilOnHitNonDamageable, nameof(m_RecoilOnHitNonDamageable));
            allGood &= IsRecoilVariablesValid(m_RecoilOnMiss, nameof(m_RecoilOnMiss));
            allGood &= IsRecoilVariablesValid(m_RecoilOnKill, nameof(m_RecoilOnKill));

            return allGood;
        }

        protected void CreateVariables()
        {
            // Activation
            m_StatController.CreateStatType(m_DelayBeforeUse.StatTypeToGetValueFrom.Get());
            m_StatController.CreateStatType(m_TimeBetweenUses.StatTypeToGetValueFrom.Get());
            m_StatController.CreateStatType(m_CooldownDuration.StatTypeToGetValueFrom.Get());
            m_BoolModuleController.CreateBoolModule(m_AutoTriggerMode, false);
            m_BoolModuleController.CreateBoolModule(m_Interruptable, false);

            // Burst Mode
            m_StatController.CreateStatType(m_BurstLength.StatTypeToGetValueFrom.Get());
            m_StatController.CreateStatType(m_BurstTimeBetweenShots.StatTypeToGetValueFrom.Get());
            m_BoolModuleController.CreateBoolModule(m_UseBurstMode, false);

            // Magazine Based
            m_StatController.CreateStatType(m_MagazineSize.StatTypeToGetValueFrom.Get());
            m_StatController.CreateStatType(m_ReloadTime.StatTypeToGetValueFrom.Get());
            m_StatController.CreateStatType(m_AmmoConsumedPerShot.StatTypeToGetValueFrom.Get());
            m_BoolModuleController.CreateBoolModule(m_MagazineBased, false);
            m_BoolModuleController.CreateBoolModule(m_AutoReload, false);

            // Movement Modifiers
            m_StatController.CreateStatType(m_MovementModifierWhileEquipped.StatTypeToGetValueFrom.Get());
            m_StatController.CreateStatType(m_MovementModifierWhileAttacking.StatTypeToGetValueFrom.Get());
            m_BoolModuleController.CreateBoolModule(m_ModifyMovementWhileEquipped, false);
            m_BoolModuleController.CreateBoolModule(m_ModifyMovementWhileAttacking, false);
            m_BoolModuleController.CreateBoolModule(m_DisableGravityWhileInUse, false);

            // Recoil
            CreateRecoilVariables(m_RecoilOnUse);
            CreateRecoilVariables(m_RecoilOnHitDamageable);
            CreateRecoilVariables(m_RecoilOnHitNonDamageable);
            CreateRecoilVariables(m_RecoilOnMiss);
            CreateRecoilVariables(m_RecoilOnKill);
        }

        protected void SubscribeToEvents()
        {
            // Activation
            m_StatController.SubscribeOnCurrentStatChange(m_DelayBeforeUse.StatTypeToGetValueFrom.Get(), UpdateDelayBeforeUse);
            m_StatController.SubscribeOnCurrentStatChange(m_TimeBetweenUses.StatTypeToGetValueFrom.Get(), UpdateTimeBetweenUses);
            m_StatController.SubscribeOnCurrentStatChange(m_CooldownDuration.StatTypeToGetValueFrom.Get(), UpdateCooldownDuration);
            m_BoolModuleController.SubscribeOnSetState(m_AutoTriggerMode, UpdateTriggerMode);
            m_BoolModuleController.SubscribeOnSetState(m_Interruptable, UpdateInterruptable);

            // Burst Mode
            m_StatController.SubscribeOnCurrentStatChange(m_BurstLength.StatTypeToGetValueFrom.Get(), UpdateBurstLength);
            m_StatController.SubscribeOnCurrentStatChange(m_BurstTimeBetweenShots.StatTypeToGetValueFrom.Get(), UpdateBurstTimeBetweenShots);
            m_BoolModuleController.SubscribeOnSetState(m_UseBurstMode, UpdateUseBurstMode);

            // Magazine Based
            m_StatController.SubscribeOnCurrentStatChange(m_MagazineSize.StatTypeToGetValueFrom.Get(), UpdateMagazineSize);
            m_StatController.SubscribeOnCurrentStatChange(m_ReloadTime.StatTypeToGetValueFrom.Get(), UpdateReloadTime);
            m_StatController.SubscribeOnCurrentStatChange(m_AmmoConsumedPerShot.StatTypeToGetValueFrom.Get(), UpdateAmmoConsumedPerShot);
            m_BoolModuleController.SubscribeOnSetState(m_MagazineBased, UpdateMagazineBased);
            m_BoolModuleController.SubscribeOnSetState(m_AutoReload, UpdateAutoReload);

            // Movement Modifiers
            m_StatController.SubscribeOnCurrentStatChange(m_MovementModifierWhileEquipped.StatTypeToGetValueFrom.Get(), UpdateMovementModifierWhileEquipped);
            m_StatController.SubscribeOnCurrentStatChange(m_MovementModifierWhileAttacking.StatTypeToGetValueFrom.Get(), UpdateMovementModifierWhileAttacking);
            m_BoolModuleController.SubscribeOnSetState(m_ModifyMovementWhileEquipped, UpdateModifyMovementWhileEquipped);
            m_BoolModuleController.SubscribeOnSetState(m_ModifyMovementWhileAttacking, UpdateModifyMovementWhileAttacking);
            m_BoolModuleController.SubscribeOnSetState(m_DisableGravityWhileInUse, UpdateDisableGravityWhileInUse);

            // Recoil
            SubscribeRecoilVariables(m_RecoilOnUse, UpdateRecoilOnUse);
            SubscribeRecoilVariables(m_RecoilOnHitDamageable, UpdateRecoilOnHitDamageable);
            SubscribeRecoilVariables(m_RecoilOnHitNonDamageable, UpdateRecoilOnHitNonDamageable);
            SubscribeRecoilVariables(m_RecoilOnMiss, UpdateRecoilOnMiss);
            SubscribeRecoilVariables(m_RecoilOnKill, UpdateRecoilOnKill);
        }

        // Activation
        private void UpdateDelayBeforeUse(float pastStat, float currentStat)
        {
            m_Weapon.DelayBeforeUse = m_DelayBeforeUse.GetValue(currentStat);
        }

        private void UpdateTimeBetweenUses(float pastStat, float currentStat)
        {
            m_Weapon.TimeBetweenUses = m_TimeBetweenUses.GetValue(currentStat);
        }

        private void UpdateCooldownDuration(float pastStat, float currentStat)
        {
            m_Weapon.CooldownDuration = m_CooldownDuration.GetValue(currentStat);
        }

        private void UpdateTriggerMode(bool state)
        {
            m_Weapon.TriggerMode = state ? Weapon.TriggerModes.Auto : Weapon.TriggerModes.SemiAuto;
        }

        private void UpdateInterruptable(bool state)
        {
            m_Weapon.Interruptable = state;
        }

        // Burst Mode
        private void UpdateBurstLength(float pastStat, float currentStat)
        {
            m_Weapon.BurstLength = (int)m_BurstLength.GetValue(currentStat);
        }

        private void UpdateBurstTimeBetweenShots(float pastStat, float currentStat)
        {
            m_Weapon.BurstTimeBetweenShots = m_BurstTimeBetweenShots.GetValue(currentStat);
        }

        private void UpdateUseBurstMode(bool state)
        {
            m_Weapon.UseBurstMode = state;
        }

        // Magazine Based
        private void UpdateMagazineSize(float pastStat, float currentStat)
        {
            m_Weapon.MagazineSize = (int)m_MagazineSize.GetValue(currentStat);
        }

        private void UpdateReloadTime(float pastStat, float currentStat)
        {
            m_Weapon.ReloadTime = m_ReloadTime.GetValue(currentStat);
        }

        private void UpdateAmmoConsumedPerShot(float pastStat, float currentStat)
        {
            m_Weapon.AmmoConsumedPerShot = (int)m_AmmoConsumedPerShot.GetValue(currentStat);
        }

        private void UpdateMagazineBased(bool state)
        {
            m_Weapon.MagazineBased = state;
        }

        private void UpdateAutoReload(bool state)
        {
            m_Weapon.AutoReload = state;
        }

        // Movement Modifiers
        private void UpdateMovementModifierWhileEquipped(float pastStat, float currentStat)
        {
            m_Weapon.PermanentMovementMultiplier = m_MovementModifierWhileEquipped.GetValue(currentStat);
        }

        private void UpdateMovementModifierWhileAttacking(float pastStat, float currentStat)
        {
            m_Weapon.MovementMultiplier = m_MovementModifierWhileAttacking.GetValue(currentStat);
        }

        private void UpdateModifyMovementWhileEquipped(bool state)
        {
            m_Weapon.ModifyMovementWhileEquipped = state;
        }

        private void UpdateModifyMovementWhileAttacking(bool state)
        {
            m_Weapon.ModifyMovementWhileAttacking = state;
        }

        private void UpdateDisableGravityWhileInUse(bool state)
        {
            m_Weapon.DisableGravityWhileInUse = state;
        }

        // Recoil

        private void UpdateRecoilOnUse(bool applyRecoil, float groundedForce, float airborneForce)
        {
            m_Weapon.ApplyRecoilOnUse = applyRecoil;
            m_Weapon.RecoilOnUseProperties.RecoilForceGrounded = groundedForce;
            m_Weapon.RecoilOnUseProperties.RecoilForceAirborne = airborneForce;
        }

        private void UpdateRecoilOnHitDamageable(bool applyRecoil, float groundedForce, float airborneForce)
        {
            m_Weapon.ApplyRecoilOnHitDamageable = applyRecoil;
            m_Weapon.RecoilOnHitDamageableProperties.RecoilForceGrounded = groundedForce;
            m_Weapon.RecoilOnHitDamageableProperties.RecoilForceAirborne = airborneForce;
        }

        private void UpdateRecoilOnHitNonDamageable(bool applyRecoil, float groundedForce, float airborneForce)
        {
            m_Weapon.ApplyRecoilOnHitNonDamageable = applyRecoil;
            m_Weapon.RecoilOnHitNonDamageableProperties.RecoilForceGrounded = groundedForce;
            m_Weapon.RecoilOnHitNonDamageableProperties.RecoilForceAirborne = airborneForce;
        }

        private void UpdateRecoilOnMiss(bool applyRecoil, float groundedForce, float airborneForce)
        {
            m_Weapon.ApplyRecoilOnMiss = applyRecoil;
            m_Weapon.RecoilOnMissProperties.RecoilForceGrounded = groundedForce;
            m_Weapon.RecoilOnMissProperties.RecoilForceAirborne = airborneForce;
        }

        private void UpdateRecoilOnKill(bool applyRecoil, float groundedForce, float airborneForce)
        {
            m_Weapon.ApplyRecoilOnKill = applyRecoil;
            m_Weapon.RecoilOnKillProperties.RecoilForceGrounded = groundedForce;
            m_Weapon.RecoilOnKillProperties.RecoilForceAirborne = airborneForce;
        }

        // Recoil Helpers
        private bool IsRecoilVariablesValid(RecoilVariables recoil, string variableName)
        {
            bool allGood = true;
            allGood &= IsBoolModuleValid(recoil.ApplyRecoil, $"{variableName}.{nameof(recoil.ApplyRecoil)}");
            allGood &= IsStatTypeValid(recoil.RecoilForceGrounded.StatTypeToGetValueFrom.Get(), $"{variableName}.{nameof(recoil.RecoilForceGrounded)}");
            allGood &= IsStatTypeValid(recoil.RecoilForceAirborne.StatTypeToGetValueFrom.Get(), $"{variableName}.{nameof(recoil.RecoilForceAirborne)}");
            return allGood;
        }

        private void CreateRecoilVariables(RecoilVariables recoil)
        {
            m_BoolModuleController.CreateBoolModule(recoil.ApplyRecoil, false);
            m_StatController.CreateStatType(recoil.RecoilForceGrounded.StatTypeToGetValueFrom.Get());
            m_StatController.CreateStatType(recoil.RecoilForceAirborne.StatTypeToGetValueFrom.Get());
        }

        private void SubscribeRecoilVariables(RecoilVariables recoil, Action<bool, float, float> onUpdate)
        {
            bool applyRecoil = false;
            float groundedForce = 0f;
            float airborneForce = 0f;

            m_BoolModuleController.SubscribeOnSetState(recoil.ApplyRecoil, state =>
            {
                applyRecoil = state;
                onUpdate(applyRecoil, groundedForce, airborneForce);
            });

            m_StatController.SubscribeOnCurrentStatChange(recoil.RecoilForceGrounded.StatTypeToGetValueFrom.Get(), (past, current) =>
            {
                groundedForce = recoil.RecoilForceGrounded.GetValue(current);
                onUpdate(applyRecoil, groundedForce, airborneForce);
            });

            m_StatController.SubscribeOnCurrentStatChange(recoil.RecoilForceAirborne.StatTypeToGetValueFrom.Get(), (past, current) =>
            {
                airborneForce = recoil.RecoilForceAirborne.GetValue(current);
                onUpdate(applyRecoil, groundedForce, airborneForce);
            });
        }

        protected bool IsStatTypeValid(StatType statType, string variableName)
        {
            if (statType == null)
            {
                Debug.LogError($"StatType is NULL in {variableName} in {nameof(WeaponVariables)} in gameobject {gameObject.name}.");
                return false;
            }

            return true;
        }

        private bool IsBoolModuleValid(BoolModuleType type, string variableName)
        {
            if (type == null)
            {
                Debug.LogError($"BoolModule is NULL in {variableName} in {nameof(WeaponVariables)} in gameobject {gameObject.name}.");
                return false;
            }

            return true;
        }
    }
}