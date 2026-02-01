using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using System.Globalization;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit
{
    public sealed class UserIdConverterTests
    {
        [Fact]
        public void UserKey_Roundtrip_Should_Preserve_Value()
        {
            var key = UserKey.New();
            var converter = new UAuthUserIdConverter<UserKey>();

            var str = converter.ToCanonicalString(key);
            var parsed = converter.FromString(str);

            Assert.Equal(key, parsed);
        }

        [Fact]
        public void Guid_Roundtrip_Should_Work()
        {
            var id = Guid.NewGuid();
            var converter = new UAuthUserIdConverter<Guid>();

            var str = converter.ToCanonicalString(id);
            var parsed = converter.FromString(str);

            Assert.Equal(id, parsed);
        }

        [Fact]
        public void String_Roundtrip_Should_Work()
        {
            var id = "user_123";
            var converter = new UAuthUserIdConverter<string>();

            var str = converter.ToCanonicalString(id);
            var parsed = converter.FromString(str);

            Assert.Equal(id, parsed);
        }

        [Fact]
        public void Int_Should_Use_Invariant_Culture()
        {
            var id = 1234;
            var converter = new UAuthUserIdConverter<int>();

            var str = converter.ToCanonicalString(id);

            Assert.Equal(id.ToString(CultureInfo.InvariantCulture), str);
        }

        [Fact]
        public void Long_Roundtrip_Should_Work()
        {
            var id = 9_223_372_036_854_775_000L;
            var converter = new UAuthUserIdConverter<long>();

            var str = converter.ToCanonicalString(id);
            var parsed = converter.FromString(str);

            Assert.Equal(id, parsed);
        }

        [Fact]
        public void Double_UserId_Should_Throw()
        {
            var converter = new UAuthUserIdConverter<double>();

            Assert.ThrowsAny<Exception>(() => converter.ToCanonicalString(12.34));
        }

        private sealed class CustomUserId
        {
            public string Value { get; set; } = "x";
        }

        [Fact]
        public void Custom_UserId_Should_Fail()
        {
            var converter = new UAuthUserIdConverter<CustomUserId>();

            Assert.ThrowsAny<Exception>(() => converter.ToCanonicalString(new CustomUserId()));
        }

        [Fact]
        public void UserKey_Json_Serialization_Should_Be_String()
        {
            var key = UserKey.New();

            var json = JsonSerializer.Serialize(key);
            var roundtrip = JsonSerializer.Deserialize<UserKey>(json);

            Assert.Equal(key, roundtrip);
        }

    }
}
