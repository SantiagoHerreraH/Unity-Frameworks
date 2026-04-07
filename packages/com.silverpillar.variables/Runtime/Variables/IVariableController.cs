using UnityEngine;

namespace SilverPillar.Variables
{
    public interface IVariableController
    {
        public bool HasVariable(Variable variable);
        public float GetValue(Variable variable);
    }
}
