#if UNITY_EDITOR
using UnityEditor;


namespace SilverPillar.Core
{
    /// <summary>
    /// Batches AssetDatabase.SaveAssets() so many OnValidate calls only trigger one save.
    /// Put this in an Editor folder if you prefer; leaving it wrapped is fine too.
    /// </summary>
    internal static class EditorSaveScheduler
    {
        private static bool s_SaveQueued;

        public static void QueueSaveAssetsOnce()
        {
            if (s_SaveQueued)
                return;

            s_SaveQueued = true;
            EditorApplication.delayCall += DoSave;
        }

        private static void DoSave()
        {
            EditorApplication.delayCall -= DoSave;
            s_SaveQueued = false;

            AssetDatabase.SaveAssets();
        }
    }
}

#endif