using UnityEngine;
using Edgar.Unity;
using SilverPillar.Notifier;

#if SILVERPILLAR_NOTIFIER

namespace SilverPillar.Integrations.Edgar
{
    [CreateAssetMenu(menuName = "Edgar/Custom/NotifyChannelPostProcess", fileName = "NotifyChannelPostProcess")]
    public class NotifyChannelPostProcess : DungeonGeneratorPostProcessingGrid2D
    {
        [SerializeField]
        private Channel m_ChannelToNotify;

        public override void Run(DungeonGeneratorLevelGrid2D level)
        {
            ChannelNotifier.NotifyChannel(m_ChannelToNotify);
        }
    }
}
#endif