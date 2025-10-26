using DouglasDwyer.PowerSerializer;
using DouglasDwyer.PowerSerializer.Formatters;

namespace TestProject
{
    public class Baz
    {
        public bool Car;
    }

    public sealed class Ass
    {
        public string Foo;
        public int Bar = 4040;

        public Ass() { }

        public Ass(int bar)
        {
            Bar = bar;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var options = new PowerSerializerOptions();
            options.KnownAssemblies.Add(typeof(Program).Assembly);

            var serializer = new PowerSerializer(options);

            var ppp = new Ass(58) { Foo = "foo" };
            var ser = serializer.Serialize<Ass>(ppp);
            var deser = serializer.Deserialize<Ass>(ser);

            Console.WriteLine("Hello, World!");
        }
    }
}
