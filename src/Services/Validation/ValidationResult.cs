namespace Services.Validation;

/// <summary>
/// Outcome of a business-rules validation pass: a success flag plus the
/// human-readable error messages collected when it fails.
/// </summary>
public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    /// <summary>A passing result with no errors.</summary>
    public static ValidationResult Success { get; } = new(true, []);

    /// <summary>A failing result carrying one or more error messages.</summary>
    public static ValidationResult Failure(params string[] errors) => new(false, errors);

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> with the joined error
    /// messages when the result is invalid; returns silently otherwise. This
    /// centralizes the translation from a validation outcome to the exception
    /// the service contract continues to surface to its callers.
    /// </summary>
    public void EnsureValid()
    {
        if (!IsValid)
        {
            throw new InvalidOperationException(string.Join(" ", Errors));
        }
    }
}
