using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public struct DataFromChoosing<TOption>
    {
        public List<TOption> ChosenData;
        public List<TOption> NotChosenData;

        public List<TOption> AllData()
        {
            var allData = new List<TOption>();

            allData.AddRange(ChosenData);
            allData.AddRange(NotChosenData); 

            return allData;
        }

        public void Clear()
        {
            ChosenData.Clear();
            NotChosenData.Clear();
        }

        public void Append(DataFromChoosing<TOption> other)
        {
            ChosenData.AddRange(other.ChosenData);
            NotChosenData.AddRange(other.NotChosenData);
        }

        public void AppendChosen(List<TOption> other)
        {
            ChosenData.AddRange(other);
        }

        public void AppendNotChosen(List<TOption> other)
        {
            NotChosenData.AddRange(other);
        }

        public void AddChosen(TOption option)
        {
            ChosenData.Add(option);
        }

        public void AddNotChosen(TOption option)
        {
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