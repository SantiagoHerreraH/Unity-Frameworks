using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace SilverPillar.Animation
{
    [Serializable]
    public class AnimationClipData
    {
        [BoxGroup("Clip Settings")]
        public AnimationClip Clip;
        [BoxGroup("Clip Settings"), Min(0)]
        public float TransitionTime = 0.2f;
        [BoxGroup("Actions"), OdinSerialize, ShowInInspector]
        public IAction Action;
        [BoxGroup("Events")]
        public UnityEvent OnAnimationStart;
        [BoxGroup("Events")]
        public UnityEvent OnAnimationEnd;
    }

    [RequireComponent(typeof(Animator))]
    public class AnimationClipPlayer : MonoBehaviour
    {
        private PlayableGraph m_Graph;
        private AnimationLayerMixerPlayable m_Mixer;
        private Animator m_Animator;
        private bool m_Initialized = false;
        private Coroutine m_CurrentAnimationRoutine;

        void Awake()
        {
            Initialize();
        }

        public void PlayOneShot(AnimationClip clip)
        {
            Initialize();

            // 4. Creamos el clip "jugable"
            var clipPlayable = AnimationClipPlayable.Create(m_Graph, clip);

            // 5. Reemplazamos la conexión en la entrada 0
            m_Graph.Disconnect(m_Mixer, 0);
            m_Graph.Connect(clipPlayable, 0, m_Mixer, 0);

            // Reiniciamos el tiempo del clip
            clipPlayable.SetTime(0);
            m_Mixer.SetInputWeight(0, 1f);
        }

        public void PlayOneShot(AnimationClipData data)
        {
            if (data == null || data.Clip == null) return;

            // Cancelar animación previa para evitar solapamiento de IActions
            if (m_CurrentAnimationRoutine != null)
            {
                StopCoroutine(m_CurrentAnimationRoutine);
            }

            m_CurrentAnimationRoutine = StartCoroutine(ExecuteAnimationData(data));
        }

        private IEnumerator ExecuteAnimationData(AnimationClipData data)
        {
            IAction action = null;

            if (data.Action != null)
            {
                action = data.Action.Clone();
                action.SetGameObject(gameObject);
            }

            // Crear el nodo del clip dentro del grafo
            var clipPlayable = AnimationClipPlayable.Create(m_Graph, data.Clip);
            m_Graph.Connect(clipPlayable, 0, m_Mixer, 1);

            // Inicio: Eventos y StartAction
            data.OnAnimationStart?.Invoke();
            action?.StartAction();

            float duration = data.Clip.length;
            float elapsed = 0f;
            float transitionTime = data.TransitionTime;

            while (elapsed < duration)
            {
                // Gestión de interpolación de pesos (Blending)
                float currentWeight = 1f;

                if (elapsed < transitionTime)
                {
                    currentWeight = elapsed / transitionTime;
                }
                else if (elapsed > duration - transitionTime)
                {
                    currentWeight = (duration - elapsed) / transitionTime;
                }

                m_Mixer.SetInputWeight(1, currentWeight);
                m_Mixer.SetInputWeight(0, 1f - currentWeight);

                // Lógica Frame a Frame del IAction
                action?.UpdateAction();

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Asegurar retorno total a la capa base
            m_Mixer.SetInputWeight(1, 0f);
            m_Mixer.SetInputWeight(0, 1f);

            // Fin: Eventos y EndAction
            action?.EndAction();
            data.OnAnimationEnd?.Invoke();

            // Limpieza de nodos para liberar memoria del grafo
            if (clipPlayable.IsValid())
            {
                m_Graph.Disconnect(m_Mixer, 1);
                clipPlayable.Destroy();
            }

            m_CurrentAnimationRoutine = null;
        }

        void OnDestroy()
        {
            // 6. Limpieza vital para evitar fugas de memoria
            if (m_Graph.IsValid()) m_Graph.Destroy();
        }

        private void Initialize()
        {
            if (!m_Initialized)
            {
                m_Animator = GetComponent<Animator>();

                // 1. Creamos el grafo una sola vez
                m_Graph = PlayableGraph.Create("DinoGraph");
                m_Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

                // 2. Creamos un mezclador para poder conectar clips dinámicamente
                m_Mixer = AnimationLayerMixerPlayable.Create(m_Graph, 1);

                // 3. Conectamos el mezclador a la salida del Animator
                var output = AnimationPlayableOutput.Create(m_Graph, "AnimationOutput", m_Animator);
                output.SetSourcePlayable(m_Mixer);

                m_Graph.Play();

                m_Initialized = true;
            }
        }
    }
}
