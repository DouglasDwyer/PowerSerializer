using System;
using System.Collections.Generic;
using System.Text;

namespace DouglasDwyer.PowerSerializer
{
    /// <summary>
    /// Stores data about a <see cref="PowerSerializer"/>'s deserialization operation, including a list of serialized types as well as the current object graph. This class, along with <see cref="PowerSerializer.CreateDeserializationContext"/>, may be overriden to store additional information.
    /// </summary>
    public class PowerDeserializationContext
    {
        /// <summary>
        /// A list of the currently-known heap-based objects being deserialized. This list is updated as additional objects are deserialized. It does not contain value types that exist as part of a reference type's data; structs will only appear in this list if they are boxed as objects.
        /// </summary>
        public IList<object> ObjectGraph => ObjectData;
        /// <summary>
        /// A list of all the types included in deserialization. It does not contain value types that exist as part of a reference type's data; struct types will only appear in this list if their instances are boxed as objects.
        /// </summary>
        public IList<Type> IncludedTypes { get; set; }

        private List<object> ObjectData = new List<object>() { null };

        /// <summary>
        /// Creates a deserialization context for a new deserialization operation.
        /// </summary>
        public PowerDeserializationContext()
        {
        }

        /// <summary>
        /// Gets type associated with a given ID, or throws an exception if the type isn't registered in the known types list.
        /// </summary>
        /// <param name="type">The ID of the type to retrieve.</param>
        /// <returns>The identified type.</returns>
        public Type GetTypeFromID(ushort id)
        {
            return IncludedTypes[id];
        }

        /// <summary>
        /// Returns whether the given object is already instantiated and registered in the object graph.
        /// </summary>
        /// <param name="obj">The ID of the object to check.</param>
        /// <returns>Whether the object is already in the deserialization object graph.</returns>
        public bool HasObject(ushort id)
        {
            return id < ObjectData.Count;
        }

        /// <summary>
        /// Returns the object with the given reference ID, or throws an exception if the object isn't registered in the object graph.
        /// </summary>
        /// <param name="obj">The ID of the object to retrieve.</param>
        /// <returns>The identified object.</returns>
        public object GetObject(ushort id)
        {
            return ObjectData[id];
        }

        /// <summary>
        /// Registers the object that has the next reference ID in the object graph.
        /// </summary>
        /// <param name="obj">The object to register.</param>
        public void RegisterNextObject(object obj)
        {
            ObjectData.Add(obj);
        }
    }
}
