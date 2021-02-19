using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using DouglasDwyer.PowerSerializer;

namespace TestProject
{
    class Program
    {
        public static int WARM = 10000000;
        public static int THRESH = 1000000;

        static void Main(string[] args)
        {
            PowerSerializer ser = new PowerSerializer();
            byte[] data = ser.Serialize(new[] { typeof(Program), null, typeof(string) });
            object deserialized = ser.Deserialize(data);

            Console.ReadKey();
        }

        static void SpeedTest()
        {
            PowerSerializer ser = new PowerSerializer(new FullGuidTypeResolver());
            Cat<int> cat = new Cat<int>();
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream memorystream = new MemoryStream();
            bf.Serialize(memorystream, cat);
            byte[] compettee = memorystream.ToArray();
            byte[] pb = ser.Serialize(cat);
            for (int i = 0; i < WARM; i++) { }

            DateTime start = DateTime.Now;
            for (int i = 0; i < THRESH; i++)
            {
                ser.Serialize(cat);
            }
            TimeSpan pTime = DateTime.Now - start;
            start = DateTime.Now;
            for (int i = 0; i < THRESH; i++)
            {
                memorystream = new MemoryStream();
                bf.Serialize(memorystream, cat);
            }
            TimeSpan sTime = DateTime.Now - start;
            

            Console.WriteLine("Total time for p: " + pTime.TotalMilliseconds + " and for s: " + sTime.TotalMilliseconds);
        }
    }

    [Serializable]
    public class Cat<T>
    {
        public object Yeet = 53;
    }
}
