namespace Kasta.Shared;

/// <summary>
/// Thrown when Kasta failed to initialise in AppStartupService
/// </summary>
/// <remarks>
/// Generated with
/// <see href="https://ktwrd.github.io/csharp-exception-generator.html"/>
/// </remarks>
public class KastaInitialiseException : Exception
{
    #region Constructors
    public KastaInitialiseException() : base()
    {}
    
    public KastaInitialiseException(string? message) : base(message)
    {}
    
    public KastaInitialiseException(string? message, Exception? innerException) : base(message, innerException)
    {}
    #endregion

    /// <inheritdoc/>
    public override string ToString()
    {
        return base.ToString();
    }
}