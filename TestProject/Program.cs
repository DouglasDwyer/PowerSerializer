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

            var ppp = new Cyclic { Foo = "ass1", Next = new[] { new Cyclic { Foo = "ass2" } } };
            ppp.Next[0]!.Next = new[] { null, ppp };

            var ser = serializer.Serialize<Cyclic>(ppp);
            var deser = serializer.Deserialize<Cyclic>(ser);

            Console.WriteLine("Hello, World!");
        }
    }
}
