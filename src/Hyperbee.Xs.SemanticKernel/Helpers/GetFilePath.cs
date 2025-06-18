namespace Hyperbee.XS.SemanticKernel.Helpers;

public static class GetFilePath
{
    /// <summary>
    /// Resolves and validates a file path, expanding environment variables and making it absolute.
    /// Throws if the file does not exist.
    /// </summary>
    public static string Resolve( string path )
    {
        if ( string.IsNullOrWhiteSpace( path ) )
            throw new ArgumentException( "File path must not be empty.", nameof( path ) );

        // Expand environment variables and ~
        var expanded = Environment.ExpandEnvironmentVariables( path );
        if ( expanded.StartsWith( "~" ) )
        {
            var home = Environment.GetFolderPath( Environment.SpecialFolder.UserProfile );
            expanded = Path.Combine( home, expanded.Substring( 1 ).TrimStart( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ) );
        }

        var fullPath = Path.GetFullPath( expanded );

        if ( !File.Exists( fullPath ) )
            throw new FileNotFoundException( $"File not found: {fullPath}" );

        return fullPath;
    }
}
