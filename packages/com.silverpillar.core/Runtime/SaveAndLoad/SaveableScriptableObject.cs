using Pillar;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Base class for ScriptableObjects that must have a stable GUID for save/load references.
/// </summary>
public class SaveableScriptableObject : ScriptableObject
{
    [FoldoutGroup("Save Data")]
    [SerializeField, ReadOnly, Tooltip("Globally unique identifier (stable across saves/builds)")]
    private string m_Guid;

    public string Guid => m_Guid;

#if UNITY_EDITOR
    internal void RegenerateGuidEditorOnly()
    {
        m_Guid = System.Guid.NewGuid().ToString("N");
        EditorUtility.SetDirty(this);
    }

    private void OnValidate()
    {
        if (!EditorUtility.IsPersistent(this))
            return;

        // Let the registry be the final authority.
        ScriptableObjectRegistry.Instance.RegisterOrUpdateEditorOnly(this);
    }
#endif
}