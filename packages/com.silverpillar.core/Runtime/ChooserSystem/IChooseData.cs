using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public struct DataFromChoosing<TOption>
    {
        private List<TOption> m_ChosenData;
        private List<TOption> m_NotChosenData;
        public List<TOption> ChosenData 
        {  get 
            {
                m_ChosenData ??= new();
                return m_ChosenData;
            }
        }
        public List<TOption> NotChosenData 
        { get 
            {
                m_NotChosenData ??= new();
                return m_NotChosenData;
            }
        }

        private void Init()
        {
            m_ChosenData ??= new();
            m_NotChosenData ??= new();
        }

        public List<TOption> AllData()
        {
            Init();
            var allData = new List<TOption>();

            allData.AddRange(ChosenData);
            allData.AddRange(NotChosenData); 

            return allData;
        }

        public void Clear()
        {
            Init();
            ChosenData.Clear();
            NotChosenData.Clear();
        }

        public void Append(DataFromChoosing<TOption> other)
        {
            Init();
            ChosenData.AddRange(other.ChosenData);
            NotChosenData.AddRange(other.NotChosenData);
        }

        public void AppendChosen(List<TOption> other)
        {
            Init();
            ChosenData.AddRange(other);
        }

        public void AppendNotChosen(List<TOption> other)
        {
            Init();
            NotChosenData.AddRange(other);
        }

        public void AddChosen(TOption option)
        {
            Init();
            ChosenData.Add(option);
        }

        public void AddNotChosen(TOption option)
        {
            Init();
            NotChosenData.Add(option);
        }
    }

    public interface IChooseData<TOption>
    {
        public List<TOption> AllData();
        public DataFromChoosing<TOption> ChooseData();
        public List<TOption> GetChosenData();
        public List<TOption> GetNotChosenData();

        public bool SetGameObject(GameObject gameObj);
        public GameObject GetGameObject();

        public IChooseData<TOption> Clone();
    }
}