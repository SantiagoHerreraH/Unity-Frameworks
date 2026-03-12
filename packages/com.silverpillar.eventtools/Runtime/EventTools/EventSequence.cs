using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventSequence : MonoBehaviour
{
    [SerializeField]
    private UnityEvent m_EventTriggeredOnStart = new();

    [SerializeField]
    private List<UnityEvent> m_EventSequence = new();
    private int m_CurrentIndex = 0;

    private void Start()
    {
        m_EventTriggeredOnStart.Invoke();
    }

    public void TriggerNextEvent()
    {
        if (m_EventSequence.Count > 0)
        {
            m_EventSequence[m_CurrentIndex].Invoke();
            m_CurrentIndex = (m_CurrentIndex + 1) % m_EventSequence.Count;
        }
    }

    public void EndSequence()
    {
        if (m_EventSequence.Count > 0)
        {
            m_CurrentIndex = m_EventSequence.Count - 1;
            m_EventSequence[m_CurrentIndex].Invoke();
            m_CurrentIndex = (m_CurrentIndex + 1) % m_EventSequence.Count;
        }
    }

}
