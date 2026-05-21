using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SilverPillar.XR
{
    [CreateAssetMenu(fileName = "ImageTrackingPrefabData", menuName = "SilverPillar/XR/ImageTrackingPrefabData")]
    public class ImageTrackingPrefabData : SaveableScriptableObject
    {
        [Serializable]
        public struct XRPrefabData
        {
            public Texture2D ReferenceImage;
            public string XRReferenceImageName;
            public GameObject Prefab;
        }

        [BoxGroup("Refill Names From Images")]
        [SerializeField]
        private XRReferenceImageLibrary m_SerializedLibrary;

        [BoxGroup("Refill Names From Images")]
        [Button(ButtonSizes.Medium)]
        private void AutoFillInNames()
        {
            if (m_SerializedLibrary == null)
            {
                Debug.LogError($"No XRReferenceImageLibrary assigned in {name}.", this);
                return;
            }

            for (int i = 0; i < m_PrefabData.Count; i++)
            {
                XRPrefabData data = m_PrefabData[i];

                if (data.ReferenceImage == null)
                {
                    Debug.LogWarning($"PlacementData at index {i} has no ReferenceImage assigned.", this);
                    continue;
                }

                bool found = false;

                for (int j = 0; j < m_SerializedLibrary.count; j++)
                {
                    XRReferenceImage referenceImage = m_SerializedLibrary[j];

                    if (referenceImage.texture == data.ReferenceImage)
                    {
                        data.XRReferenceImageName = referenceImage.name;
                        m_PrefabData[i] = data;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Debug.LogWarning(
                        $"Reference image '{data.ReferenceImage.name}' was not found in library '{m_SerializedLibrary.name}'.",
                        this
                    );
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }


        [BoxGroup("Check Validity")]
        [SerializeField, Tooltip("There will always be a message if there is an error")]
        private bool m_MessageOnAllGood;

        [BoxGroup("Check Validity")]
        [Button(ButtonSizes.Medium)]
        private void CheckValidity()
        {
            if (Initialize() && m_MessageOnAllGood)
            {
                Debug.Log($"{name}'s data is all good.");
            }
        }



        [BoxGroup("Data")]
        [SerializeField]
        private List<XRPrefabData> m_PrefabData = new();

        private Dictionary<string, GameObject> m_ReferenceImageName_To_PrefabData = new();
        private bool m_Initialized = false;

        public List<XRPrefabData> PrefabData { get { return m_PrefabData; } }

        public void CheckInitialize()
        {
            if (!m_Initialized)
            {
                Initialize();
                m_Initialized = true;
            }

        }

        private bool Initialize()
        {
            m_ReferenceImageName_To_PrefabData.Clear();

            bool allGood = true;

            for (int i = 0; i < m_PrefabData.Count; i++)
            {
                if (m_PrefabData[i].ReferenceImage == null)
                    continue;

                if (string.IsNullOrEmpty(m_PrefabData[i].XRReferenceImageName))
                    continue;

                if (m_ReferenceImageName_To_PrefabData.ContainsKey(m_PrefabData[i].XRReferenceImageName))
                {
                    Debug.LogError("Can't repeat image reference name " +
                        m_PrefabData[i].XRReferenceImageName +
                        " in ScriptableObject " + name);

                    allGood = false;
                }
                else
                {

                    m_ReferenceImageName_To_PrefabData.Add(
                        m_PrefabData[i].XRReferenceImageName,
                        m_PrefabData[i].Prefab
                    );
                }
            }

            return allGood;
        }

        public bool HasReferenceImage(ARTrackedImage referenceImage)
        {
            CheckInitialize();
            return m_ReferenceImageName_To_PrefabData.ContainsKey(referenceImage.referenceImage.name);
        }

#nullable enable
        public GameObject? GetPrefab(ARTrackedImage referenceImage)
        {
            CheckInitialize();
            GameObject? instance = null;
            if (m_ReferenceImageName_To_PrefabData.ContainsKey(referenceImage.referenceImage.name))
            {
                instance = m_ReferenceImageName_To_PrefabData[referenceImage.referenceImage.name];
            }

            return instance;
        }
    }

}
