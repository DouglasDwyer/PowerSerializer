using DouglasDwyer.PowerSerializer;

namespace TestProject
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var serializer = new PowerSerializer();

            var ser = serializer.Serialize(typeof(int));
            var deser = serializer.Deserialize<Type>(ser);

            Console.WriteLine("Hello, World!");
        }
    }
}
