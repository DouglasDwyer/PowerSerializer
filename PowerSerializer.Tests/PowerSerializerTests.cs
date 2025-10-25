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
    }
}
