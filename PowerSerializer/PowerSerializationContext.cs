using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DouglasDwyer.PowerSerializer
{
    /// <summary>
    /// Stores data about a <see cref="PowerSerializer"/>'s serialization operation, including a list of serialized types as well as the current object graph. This class, along with <see cref="PowerSerializer.CreateSerializationContext"/>, may be overriden to store additional information.
    /// </summary>
    public class PowerSerializationContext
    {
        /// <summary>
        /// A list the currently-known heap-based objects being serialized. This list is updated as additional objects are serialized. It does not contain value types that exist as part of a reference type's data; structs will only appear in this list if they are boxed as objects.
        /// </summary>
        public IList<object> ObjectGraph => SerializedObjects;
        /// <summary>
        /// A list of the currently-known types included in serialization. This list is updated as additional objects are serialized. It does not contain value types that exist as part of a reference type's data; struct types will only appear in this list if their instances are boxed as objects.
        /// </summary>
        public ImmutableList<Type> IncludedTypes => SerializedTypes.Keys.ToImmutableList();

        private Dictionary<Type, ushort> SerializedTypes = new Dictionary<Type, ushort>();
        private Dictionary<object, ushort> SerializedObjectIDs = new Dictionary<object, ushort>();
        private List<object> SerializedObjects = new List<object>() { null };

        /// <summary>
        /// Creates a serialization context for a new serialization operation.
        /// </summary>
        public PowerSerializationContext() { }

        /// <summary>
        /// Returns whether the given object is already registered in the object graph.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>Whether the object is already in the serialization object graph.</returns>
        public bool HasObject(object obj)
        {
            return obj != null && SerializedObjectIDs.ContainsKey(obj);
        }

        /// <summary>
        /// Returns the reference ID of the given object, or throws an exception if the object isn't registered in the object graph.
        /// </summary>
        /// <param name="obj">The object to examine.</param>
        /// <returns>The ID of the object.</returns>
        public ushort GetObjectID(object obj)
        {
            return SerializedObjectIDs[obj];
        }

        /// <summary>
        /// Registers an object in the object graph and adds its type to the list of known types, returning the object's type and assigned ID.
        /// </summary>
        /// <param name="obj">The object to register.</param>
        /// <returns>A tuple containing the ID of the object and the type of the object.</returns>
        public (ushort id, Type type) RegisterObject(object obj)
        {
            Type type = obj.GetType();
            if(!SerializedTypes.ContainsKey(type))
            {
                SerializedTypes[type] = (ushort)SerializedTypes.Count;
            }
            ushort toReturn = SerializedObjectIDs[obj] = (ushort)SerializedObjects.Count;
            SerializedObjects.Add(obj);
            return (toReturn, type);
        }

        /// <summary>
        /// Gets the ID for a given type, or throws an exception if the type isn't registered in the known types list.
        /// </summary>
        /// <param name="type">The type to examine.</param>
        /// <returns>The ID of the type.</returns>
        public ushort GetTypeID(Type type)
        {
            return SerializedTypes[type];
        }
    }
}
