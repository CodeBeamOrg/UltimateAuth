using System.Xml.Linq;

namespace CodeBeam.UltimateAuth.Server.Options
{
    public sealed class UAuthCookieLifetimeOptions
    {
        /// <summary>
        /// Extra lifetime added on top of the logical credential lifetime.
        /// Prevents premature cookie eviction by the browser.
        /// </summary>
        public TimeSpan? IdleBuffer { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Allows developer to fully override cookie lifetime.
        /// If set, buffer logic is ignored.
        /// </summary>
        public TimeSpan? AbsoluteLifetimeOverride { get; set; }

        internal UAuthCookieLifetimeOptions Clone() => new()
        {
            IdleBuffer = IdleBuffer,
            AbsoluteLifetimeOverride = AbsoluteLifetimeOverride
        };
    }
}
