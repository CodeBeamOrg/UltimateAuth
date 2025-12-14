using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed record ReauthResult
    {
        public bool Success { get; init; }
    }
}
