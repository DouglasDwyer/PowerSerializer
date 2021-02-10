using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DouglasDwyer.PowerSerializer
{
    public sealed class PowerSerializationContext
    {
        private Dictionary<Type, ushort> SerializedTypes = new Dictionary<Type, ushort>();
        private Dictionary<object, SerializedObjectData> SerializedObjects = new Dictionary<object, SerializedObjectData>();

        public Type RegisterObject(Type type, object obj)
        {
            SerializedObjects[obj] = new SerializedObjectData((ushort)(1 + SerializedObjects.Count), obj, type);
            return type;
        }

        public ushort GetIDForType(Type type)
        {
            return SerializedTypes[type];
        }

        public SerializedObjectData GetDataForObject(object obj)
        {
            return SerializedObjects[obj];
        }

        public bool HasObject(object obj)
        {
            return SerializedObjects.ContainsKey(obj);
        }

        public ImmutableList<Type> GetSerializedTypes()
        {
            return SerializedTypes.OrderBy(x => x.Value).Select(x => x.Key).ToImmutableList();
        }

        public ImmutableList<SerializedObjectData> GetSerializationData()
        {
            return SerializedObjects.Values.OrderBy(x => x.ID).ToImmutableList();
        }

        public bool AddType(Type type)
        {
            if (!SerializedTypes.ContainsKey(type))
            {
                SerializedTypes.Add(type, (ushort)SerializedTypes.Count);
                return true;
            }
            return false;
        }

        public struct SerializedObjectData
        {
            public ushort ID { get; }
            public object Value { get; }
            public Type ObjectType { get; }

            public SerializedObjectData(ushort id, object obj, Type objectType)
            {
                ID = id;
                Value = obj;
                ObjectType = objectType;
            }
        }
    }
}
