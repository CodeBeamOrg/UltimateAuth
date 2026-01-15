using CodeBeam.UltimateAuth.Core.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeBeam.UltimateAuth.Tests.Unit
{
    internal static class TestIds
    {
        public static AuthSessionId Session(string raw)
        {
            if (!AuthSessionId.TryCreate(raw, out var id))
                throw new InvalidOperationException($"Invalid test AuthSessionId: {raw}");

            return id;
        }
    }
}
