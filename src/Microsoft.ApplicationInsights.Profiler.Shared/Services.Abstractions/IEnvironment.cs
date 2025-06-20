namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

/// <summary>
/// A mockable service to provide functions of <see cref="System.Environment" /> static class.
/// </summary>
internal interface IEnvironment
{
    /// <summary>
    /// Replaces the name of each environment variable embedded in the specified string with the string equivalent of the value of the variable, then returns the resulting string.
    /// </summary>
    /// <param name="name">A string containing the names of zero or more environment variables. Each environment variable is quoted with the percent sign character (%).</param>
    /// <returns>A string with each environment variable replaced by its value.</returns>
    string ExpandEnvironmentVariables(string name);
}
