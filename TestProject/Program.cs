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

            var ppp = new List<string>() { "a", "a" , "a", "a" };
            /*var ppp = new HashSet<object>();
            ppp.Add(1);
            ppp.Add(-2);
            ppp.Add(64);*/

            var ser = serializer.Serialize<List<string>>(ppp);
            var deser = serializer.Deserialize<List<string>>(ser);

            Console.WriteLine("Hello, World!");
        }
    }
}
