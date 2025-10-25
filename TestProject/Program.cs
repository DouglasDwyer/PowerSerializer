using DouglasDwyer.PowerSerializer;

namespace TestProject
{
    public interface IAss<T> { }

    public class Ass<T> : IAss<T>
    {
        public Ass(string hurdur) { }

        //public Ass(List<int> foobar) { }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var options = new PowerSerializerOptions();
            options.KnownAssemblies.Add(typeof(Program).Assembly);

            var serializer = new PowerSerializer(options);

            var myObj = new object();
            var ppp = new object[] { 1, "yourmom", myObj, false, new object(), myObj, 4, 5 };
            var ser = serializer.Serialize<object[]>(ppp);
            var deser = serializer.Deserialize<object[]>(ser);

            Console.WriteLine("Hello, World!");
        }
    }
}
