using DouglasDwyer.PowerSerializer;

namespace TestProject
{
    public class Baz
    {
        public bool Car;
    }

    public sealed class Cyclic
    {
        public string Foo;
        public Cyclic?[] Next;

        public Cyclic() { }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var options = new PowerSerializerOptions();
            options.KnownAssemblies.Add(typeof(Program).Assembly);

            var serializer = new PowerSerializer(options);

            var ppp = new Type[] { typeof(int).GetType(), typeof(List<string>) };

            var ser = serializer.Serialize<object>(ppp);
            var deser = serializer.Deserialize<object>(ser);

            Console.WriteLine("Hello, World!");
        }
    }
}
