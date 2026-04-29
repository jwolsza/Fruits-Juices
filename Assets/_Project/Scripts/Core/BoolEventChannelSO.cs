using System;
using UnityEngine;

namespace Project.Core
{
    [CreateAssetMenu(fileName = "OnBoolChanged", menuName = "Project/Events/Bool Event Channel")]
    public class BoolEventChannelSO : ScriptableObject
    {
        public event Action<bool> Raised;
        public bool LastValue { get; private set; }

        public void Raise(bool value)
        {
            LastValue = value;
            Raised?.Invoke(value);
        }
    }
}
