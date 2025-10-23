namespace DouglasDwyer.PowerSerializer;


public interface IFormatterResolver
{
    IFormatter<T>? GetFormatter<T>();
}
