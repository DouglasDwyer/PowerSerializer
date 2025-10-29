using DouglasDwyer.PowerSerializer.Formatters;

namespace DouglasDwyer.PowerSerializer.Tests
{
    /// <summary>
    /// Tests for the <see cref="PowerSerializer"/> type.
    /// </summary>
    [TestClass]
    public sealed class PowerSerializerTests
    {
        /// <summary>
        /// Tests that the serializer preserves references.
        /// </summary>
        [TestMethod]
        public void TestReferenceEquality()
        {
            var serializer = new PowerSerializer();
            var a = new object();
            var b = new object();

            var bytes = serializer.Serialize(new[] { a, b, a });
            var deserialized = serializer.Deserialize<object[]>(bytes);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual(deserialized.Length, 3);
            Assert.AreEqual(deserialized[0], deserialized[2]);
            Assert.AreNotEqual(deserialized[0], deserialized[1]);
        }

        /// <summary>
        /// Tests that an open generic type can be serialized.
        /// </summary>
        [TestMethod]
        public void TestSerializeOpenGeneric()
        {
            var serializer = new PowerSerializer();

            var bytes = serializer.Serialize(typeof(List<>));
            var deserialized = serializer.Deserialize<Type>(bytes);

            Assert.AreEqual(typeof(List<>), deserialized);
        }

        /// <summary>
        /// Tests that it is possible to serialize a constructed
        /// generic with type parameters.
        /// </summary>
        [TestMethod]
        public void TestSerializeTypeParameters()
        {
            var serializer = new PowerSerializer();

            var constructedGeneric = typeof(List<>)
                .GetInterfaces().First(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == typeof(IList<>));
            var bytes = serializer.Serialize(constructedGeneric);
            var deserialized = serializer.Deserialize<Type>(bytes);

            Assert.AreEqual(constructedGeneric, deserialized);
        }

        /// <summary>
        /// Tests that the <see cref="DefaultFormatterAttribute"/> can be used to specify a custom formatter
        /// with a default constructor.
        /// </summary>
        [TestMethod]
        public void TestDefaultFormatterAttributeDefaultConstructor()
        {
            var serializer = new PowerSerializer();

            var bytes = serializer.Serialize(new TestCustom());
            var deserialized = serializer.Deserialize<TestCustom>(bytes);

            Assert.IsNotNull(deserialized);
            Assert.IsTrue(deserialized.Deserialized);
        }

        /// <summary>
        /// Tests that the <see cref="DefaultFormatterAttribute"/> can be used to specify a custom formatter
        /// with a constructor accepting <see cref="PowerSerializer"/>.
        /// </summary>
        [TestMethod]
        public void TestDefaultFormatterAttributeSerializerConstructor()
        {
            var serializer = new PowerSerializer();

            var bytes = serializer.Serialize(new TestCustom2());
            var deserialized = serializer.Deserialize<TestCustom2>(bytes);

            Assert.IsNotNull(deserialized);
            Assert.IsTrue(deserialized.Deserialized);
        }

        [DefaultFormatter(typeof(TestCustomFormatter))]
        private class TestCustom
        {
            public readonly bool Deserialized;

            public TestCustom() : this(false) { }

            private TestCustom(bool deserialized)
            {
                Deserialized = deserialized;
            }

            private sealed class TestCustomFormatter : IFormatter<TestCustom>
            {
                public void Deserialize(BufferReader reader, out TestCustom value)
                {
                    value = new TestCustom(true);
                }

                public void Serialize(BufferWriter writer, in TestCustom value) { }
            }
        }

        [DefaultFormatter(typeof(TestCustom2Formatter))]
        private class TestCustom2
        {
            public readonly bool Deserialized;

            public TestCustom2() : this(false) { }

            private TestCustom2(bool deserialized)
            {
                Deserialized = deserialized;
            }

            private sealed class TestCustom2Formatter : IFormatter<TestCustom2>
            {
                public TestCustom2Formatter(PowerSerializer serializer) { }

                public void Deserialize(BufferReader reader, out TestCustom2 value)
                {
                    value = new TestCustom2(true);
                }

                public void Serialize(BufferWriter writer, in TestCustom2 value) { }
            }
        }
    }
}
