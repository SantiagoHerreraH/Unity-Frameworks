using UnityEngine;
using Edgar.Unity;

namespace SilverPillar.Integrations.Edgar
{
    [CreateAssetMenu(menuName = "SilverPillar/Integrations/Edgar/Set Layer Post-process", fileName = "SetLayerPostProcess")]
    public class SetLayerPostProcess : DungeonGeneratorPostProcessingGrid2D
    {
        [SerializeField, Tooltip("The name of the Layer you want to assign to the generated level.")]
        private string m_TargetLayerName = "Default";

        public override void Run(DungeonGeneratorLevelGrid2D level)
        {
            // Convert the string name to the internal Unity Layer ID
            int layerId = LayerMask.NameToLayer(m_TargetLayerName);

            // Check if the layer actually exists in your project settings
            if (layerId == -1)
            {
                Debug.LogError($"Layer '{m_TargetLayerName}' does not exist in the project settings. Please check your Layer names.");
                return;
            }

            var tilemaps = level.GetSharedTilemaps();

            foreach (var tileMap in tilemaps)
            {
                tileMap.gameObject.layer = layerId;
            }

        }
    }

}

