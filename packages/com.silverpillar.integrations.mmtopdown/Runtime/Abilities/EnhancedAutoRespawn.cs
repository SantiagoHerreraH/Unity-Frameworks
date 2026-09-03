using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    public class EnhancedAutoRespawn : AutoRespawn
    {
        private Rigidbody2D _rigidbody2D;
        private Rigidbody _rigidbody;
        private Collider _collider;
        private bool _previousKinematicType;
        private RigidbodyType2D _previousBodyType;

        /// <summary>
        /// On Start we grab our various components
        /// </summary>
        protected override void Start()
        {
            base.Start();
            _collider = GetComponent<Collider>();
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        /// <summary>
        /// Kills this object, turning its parts off based on the settings set in the inspector
        /// </summary>
        public override void Kill()
        {
            if (AutoRespawnDuration <= 0f)
            {
                // object is turned inactive to be able to reinstate it at respawn
                if (DisableGameObjectOnKill)
                {
                    gameObject.SetActive(false);
                }
            }
            else
            {
                if (DisableAllComponentsOnKill)
                {
                    foreach (MonoBehaviour component in _otherComponents)
                    {
                        if (component != this)
                        {
                            component.enabled = false;
                        }
                    }
                }

                if (_rigidbody != null)
                {
                    _previousKinematicType = _rigidbody.isKinematic;
                    _rigidbody.isKinematic = true;
                }

                if (_rigidbody2D != null)
                {
                    _previousBodyType = _rigidbody2D.bodyType;
                    _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
                }

                if (_collider != null) { _collider.enabled = false; }
                
                if (_collider2D != null) { _collider2D.enabled = false; }
                if (_renderer != null) { _renderer.enabled = false; }
                _reviving = true;
                _timeOfDeath = Time.time;
            }
        }

        /// <summary>
		/// Revives this object, turning its parts back on again
		/// </summary>
		public override void Revive()
        {
            if (AutoRespawnDuration <= 0f)
            {
                // object is turned inactive to be able to reinstate it at respawn
                gameObject.SetActive(true);
            }
            else
            {
                if (DisableAllComponentsOnKill)
                {
                    foreach (MonoBehaviour component in _otherComponents)
                    {
                        component.enabled = true;
                    }
                }

                if (_rigidbody != null)
                {
                    _rigidbody.isKinematic = _previousKinematicType;
                }

                if (_rigidbody2D != null)
                {
                    _rigidbody2D.bodyType = _previousBodyType;
                }

                if (_collider != null) { _collider.enabled = true; }

                if (_collider2D != null) { _collider2D.enabled = true; }

                if (_renderer != null) { _renderer.enabled = true; }

                InstantiateRespawnEffect();
                PlayRespawnSound();
            }
            if (_health != null)
            {
                _health.Revive();
            }
            if (_aiBrain != null)
            {
                _aiBrain.ResetBrain();
            }
            OnRevive?.Invoke();
            if (OnReviveEvent != null)
            {
                OnReviveEvent.Invoke();
            }
        }
    }
}
