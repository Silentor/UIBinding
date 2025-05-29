using System;

namespace UIBindings.Runtime.Types
{
    [Serializable]
    public struct KeyValue<TSerializable>
    {
        public int           Key;
        public TSerializable Value;
    }
}