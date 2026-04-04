using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.EntityFrameworkCore
{
    /// <summary>
    /// Provides configuration options for setting up Entity Framework Core database contexts used by the UltimateAuth
    /// system.
    /// </summary>
    /// <remarks>Use this class to specify delegates that configure the options for various DbContext
    /// instances, such as Users, Credentials, Authorization, Sessions, Tokens, and Authentication. Each property allows
    /// customization of the corresponding context's setup, including database provider and connection details. If a
    /// specific configuration delegate is not set for a context, the default configuration is applied. This class is
    /// typically configured during application startup to ensure consistent and flexible database context
    /// initialization.</remarks>
    public sealed class UAuthEfCoreOptions
    {
        /// <summary>
        /// Gets or sets the default action to configure the database context options builder.
        /// </summary>
        /// <remarks>Use this property to specify a delegate that applies default configuration to a
        /// DbContextOptionsBuilder instance. This action is typically invoked when setting up a new database context to
        /// ensure consistent configuration across the application.</remarks>
        public Action<DbContextOptionsBuilder>? Default { get; set; }


        /// <summary>
        /// Gets or sets the delegate used to configure options for the Users DbContext.
        /// If not set, default option will implement.
        /// </summary>
        /// <remarks>Assign a delegate to customize the configuration of the Users DbContext, such as
        /// specifying the database provider or connection string. This property is typically used during application
        /// startup to control how the Users context is set up.</remarks>
        public Action<DbContextOptionsBuilder>? Users { get; set; }

        /// <summary>
        /// Gets or sets the delegate used to configure options for the Credentials DbContext.
        /// If not set, default option will implement.
        /// </summary>
        /// <remarks>Assign a delegate to customize the configuration of the Credentials DbContext, such as
        /// specifying the database provider or connection string. This property is typically used during application
        /// startup to control how the Credentials context is set up.</remarks>
        public Action<DbContextOptionsBuilder>? Credentials { get; set; }

        /// <summary>
        /// Gets or sets the delegate used to configure options for the Authorization DbContext.
        /// If not set, default option will implement.
        /// </summary>
        /// <remarks>Assign a delegate to customize the configuration of the Authorization DbContext, such as
        /// specifying the database provider or connection string. This property is typically used during application
        /// startup to control how the Authorization context is set up.</remarks>
        public Action<DbContextOptionsBuilder>? Authorization { get; set; }

        /// <summary>
        /// Gets or sets the delegate used to configure options for the Sessions DbContext.
        /// If not set, default option will implement.
        /// </summary>
        /// <remarks>Assign a delegate to customize the configuration of the Sessions DbContext, such as
        /// specifying the database provider or connection string. This property is typically used during application
        /// startup to control how the Sessions context is set up.</remarks>
        public Action<DbContextOptionsBuilder>? Sessions { get; set; }

        /// <summary>
        /// Gets or sets the delegate used to configure options for the Tokens DbContext.
        /// If not set, default option will implement.
        /// </summary>
        /// <remarks>Assign a delegate to customize the configuration of the Tokens DbContext, such as
        /// specifying the database provider or connection string. This property is typically used during application
        /// startup to control how the Tokens context is set up.</remarks>
        public Action<DbContextOptionsBuilder>? Tokens { get; set; }

        /// <summary>
        /// Gets or sets the delegate used to configure options for the Authentication DbContext.
        /// If not set, default option will implement.
        /// </summary>
        /// <remarks>Assign a delegate to customize the configuration of the Authentication DbContext, such as
        /// specifying the database provider or connection string. This property is typically used during application
        /// startup to control how the Authentication context is set up.</remarks>
        public Action<DbContextOptionsBuilder>? Authentication { get; set; }

        internal Action<DbContextOptionsBuilder> Resolve(Action<DbContextOptionsBuilder>? specific)
            => specific ?? Default ?? throw new InvalidOperationException("No database configuration provided for UltimateAuth EFCore. Use options.Default or configure specific DbContext options.");
    }
}
