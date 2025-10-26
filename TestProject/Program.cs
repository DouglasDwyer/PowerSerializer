using DouglasDwyer.PowerSerializer;
using DouglasDwyer.PowerSerializer.Formatters;
using System.Text;

namespace TestProject
{
    public class Baz
    {
        public bool Car;
    }

    public sealed class Cyclic
    {
        public string Foo;
        public Cyclic Next;
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var options = new PowerSerializerOptions();
            options.KnownAssemblies.Add(typeof(Program).Assembly);

            var serializer = new PowerSerializer(options);

            var ppp = new Cyclic { Foo = "ass1", Next = new Cyclic { Foo = "ass2" } };
            ppp.Next.Next = ppp;

            var ser = serializer.Serialize<Cyclic>(ppp);
            var deser = serializer.Deserialize<Cyclic>(ser);

            Console.WriteLine("Hello, World!");
        }
    }
}
