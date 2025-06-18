using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Hyperbee.XS.SemanticKernel.Helpers
{
    public static class AppSettingsHelper
    {
        private static IConfigurationRoot? _config;
        public static IConfigurationRoot Config => _config ??= BuildConfig();

        private static IConfigurationRoot BuildConfig()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath( AppDomain.CurrentDomain.BaseDirectory )
                .AddJsonFile( "appsettings.json", optional: true );

            // Try to load from the samples directory (relative to project root)
            var samplesPath = Path.GetFullPath( Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "..", "..", "samples" ) );
            var samplesConfig = Path.Combine( samplesPath, "appsettings.json" );
            if ( File.Exists( samplesConfig ) )
            {
                Console.WriteLine( $"[AppSettingsHelper] Found samples appsettings.json at: {samplesConfig}" );
                builder.AddJsonFile( samplesConfig, optional: true );
            }
            else
            {
                Console.WriteLine( $"[AppSettingsHelper] samples appsettings.json NOT found at: {samplesConfig}" );
            }

            // Try to load from the current working directory (notebook location)
            var cwdConfig = Path.Combine( Directory.GetCurrentDirectory(), "appsettings.json" );
            if ( File.Exists( cwdConfig ) )
            {
                Console.WriteLine( $"[AppSettingsHelper] Found appsettings.json in current working directory: {cwdConfig}" );
                builder.AddJsonFile( cwdConfig, optional: true );
            }
            else
            {
                Console.WriteLine( $"[AppSettingsHelper] appsettings.json NOT found in current working directory: {cwdConfig}" );
            }

            var config = builder.Build();
            return config;
        }
        public static string? Get( string key ) => Config[key];
    }
}
