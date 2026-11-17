using Stellaria.Helper.Security;

namespace Stellaria.Helper.Catalog;

internal static class TrustedKeys
{
    public const string Registry = "RWTou8VvwZXwQHs+sbv0zMiXD1JvVzuCX0ICnWMLpDqDAt7GcjMCMe/Z";

    public static MinisignPublicKey RegistryKey { get; } = MinisignPublicKey.Parse(Registry);
}
