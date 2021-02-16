# PowerSerializer
 PowerSerializer is a fast, efficient, customizable C# binary serializer that can serialize absolutely anything.

### Installation

PowerSerializer can be obtained as a Nuget package. To import it into your project, either download DouglasDwyer.PowerSerializer from the Visual Studio package manager or run the command `Install-Package DouglasDwyer.PowerSerializer` using the package manager console.

### How to use

PowerSerializer is robust but quite simple to use. The following code snippet serializes and then deserializes an object, storing all of the object's data in a byte array and then retrieving it:
```csharp
PowerSerializer ser = new PowerSerializer();
List<object> objs = new List<object>() { 73, "hello", false };
byte[] data = ser.Serialize(objs);
List<object> deserialized = ser.Deserialize<List<object>>(data);
Console.WriteLine(objs.SequenceEqual(deserialized)); //True
```

### Features

#### Functionality
PowerSerializer is an object-oriented, reference-based binary serializer. Upon serialization, PowerSerializer iterates over the graph of the given object, discovering its references and primitive values, and stores them in a byte array. PowerSerializer does the opposite for deserialization. All of an object's fields - both public and nonpublic - are inspected to obtain a full graph of the object. PowerSerializer officially supports the following:
- Polymorphism and serialization of inherited types
- Serialization of reference types with complex object graphs, including cyclical references
- Serialization of strings
- Serialization of primitive values and other struct types, both boxed and unboxed
- Serialization of arrays

Types do not need any specific designation/attribute to be serializable. However, the `ITypeResolver` associated with the serializer instance must allow for serialization of a given type in order for it to be serialized.

#### Binary format

PowerSerializer stores objects as bit-based structs without field names. Each serialized object looks much like it would in-memory - struct types are written directly as a fixed number of bytes, and references to reference types are written as 16-bit unsigned pointers. This means that struct/class layouts must match **exactly** during serialization and deserialization, or deserialization will fail. This makes PowerSerializer most useful for short-term applications where object layouts shouldn't change. If cross-platform or cross-assembly compatability is important, however, PowerSerializer may be modified to store field names. A builtin PowerSerializer subclass with this functionality is planned in the future.

#### Security

Serialization can be a dangerous affair, especially with a library as far-reaching/nonrestrictive as PowerSerializer. As such, PowerSerializer implements some key safety features to minimize the risk of serialization-based attacks. In addition to utilizing PowerSerializer's safety features, all consumers of the library are encouraged to read more about serialization security.

First and foremost, PowerSerializer does not run any code on deserialized objects by default. Custom constructors and other methods are never called. In addition, each serializer employs an `ITypeResolver`, an object that regulates which objects may be serialized/deserialized. The default type resolver is a `FinalizerLimitedTypeResolver`, which allows serialization of any type that doesn't have a finalizer. This prevents [finalization attacks](https://security.stackexchange.com/questions/13490/is-it-safe-to-binary-deserialize-user-provided-data). Other flavors of resolvers, like `ClassLimitedTypeResolver` (which only allows serialization of types or assemblies specified in the constructor) are available as well. Type resolvers are also used to translate type names to binary, and can be customized to allow for serialization between identical types in different assemblies.

#### Thread-safety

All of PowerSerializer's methods are completely thread-safe, and multiple objects may be serialized/deserialized using the same serializer instance at the same time. However, modifying an object that is currently being serialized may lead to unexpected results. While serialization will complete successfully, different parts of the object may be written to the byte array at different times. This means that a modification during serialization *could* lead to the serialization data coding for an object state that never actually existed in-memory. For example, consider the following:
- A new object, with an `int` field and a `string` field, is passed as an argument into a serializer's `Serialize()` method. The current state of the object will be denoted as (0, ""), where the first value is the `int` and the second the `string`. The initial serialization data contains the value (?, ?), because neither field has been written yet.
- Another thread increments the `int` field, changing the state of the object to (1, "").
- The serializer serializes the `int` field, and the serialization data is now (1, ?).
- The other thread decrements the `int` field, and then sets the `string` to "hello", changing the state of the object to (0, "hello").
- The serializer serializes the `string` field, resulting in a serialized object of (1, "hello").

Though this situation is exceedingly rare, users should be wary of modifying their objects during serialization. Concurrent modifications can result in a serialized object whose state never existed in-memory; (1, "hello") was the result of the above example's serialization, but the object's value was never (1, "hello").

#### Extensibility

PowerSerializer is designed specifically for customizability. Most of its methods can be overridden to provide for custom behavior during serialization/deserialization (like re-hashing a dictionary's elements), allowing for PowerSerializer to meet a wide variety of needs.

### API Reference

For documentation about each type included in PowerSerializer, please see the [complete API reference](https://douglasdwyer.github.io/PowerSerializer/).

### Basic concepts

#### Serialization

Serialization begins when a user calls the `Serialize` method on a serializer instance. If the object to serialize is null, it is replaced with an internal class called `NullRepresentative`, because the serializer does allow serializing a completely null value. The serializer then calls `CreatePowerSerializationContext` to create a new serialization context where the growing object graph and type list are stored. The object graph is stored as a linear list, and the index of each object in the graph represents the pointer number used to refer to that object in the byte array.

The object graph begins by containing just the object to serialize. `SerializeObject` is called on the object to serialize, and its fields are inspected. Any value-type fields are written directly to the byte array, and any reference-type fields are added to the object graph. The references of each field are written to the byte array using the `WriteObjectReference` method. `SerializeObject` is then called again on each of the new objects in the object graph until the end of the graph is reached.

Lastly, all of the type IDs are written to the byte array by calling `TypeResolver.WriteTypeID`. Then, the resulting byte array is returned.

#### Deserialization

Deserialization begins when a user calls the `Deserialize()` method on a deserializer instance. `CreatePowerDeserializationContext` is called to create a new deserialization context where the growing object graph and type list are stored. The serializer then reads the type IDs from the end of the byte array, calling `TypeResolver.ReadTypeID` to obtain each type.

The serializer then calls `ReadAndCreateObject` to create a new object of the specified type. That object is then added to the object graph, and `DeserializeObject` is called on it, reading value-typed fields directly from the byte array and object references using `ReadObjectReference`. This process is repeated with each of the new objects in the object graph until the end of the graph is reached.

Lastly, `ProcessObjectGraph` is called to make any last-minute adjustments to the object graph, and it returns the final deserialized object.
