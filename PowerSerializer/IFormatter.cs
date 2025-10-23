using System;
using System.Collections.Generic;
using System.Text;

namespace DouglasDwyer.PowerSerializer;

public interface IFormatter<T>
{
    void Serialize(BufferWriter writer, in T value);
    void Deserialize(BufferReader reader, out T value);
}
