namespace Kasta.Shared;

/// <summary>
/// Thrown when a user-provided input tried to attempt a Path Traversal Attack.
/// The user should never be told if this happens.
/// Return a 403 error and silently log this exception.
/// </summary>
/// <remarks>
/// Generated with
/// <see href="https://ktwrd.github.io/csharp-exception-generator.html"/>
/// </remarks>
public class AttemptedPathTraversalAttackException : Exception
{
    private const string MessageFmt = "Path Traversal Attack was attempted with user-provided input\nBase Location: {0}\nUser-Provided Location: {1}";
    #region Constructors
    public AttemptedPathTraversalAttackException(string baseLocation, string userProvidedLocation)
        : base(string.Format(MessageFmt, baseLocation, userProvidedLocation))
    {
        BaseLocation = baseLocation;
        UserProvidedLocation = userProvidedLocation;
    }
    #endregion

    public string BaseLocation { get; }
    public string UserProvidedLocation { get; }
}
