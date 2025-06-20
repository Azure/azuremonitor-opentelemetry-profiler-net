using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using Azure.Core;

namespace Microsoft.ApplicationInsights.Profiler.Core.Auth;

/// <summary>
/// Creates an AuthToken from an object.
/// The target object could be a string or a AuthToken object from Application Insights.
/// </summary>
internal class AccessTokenFactory : IAccessTokenFactory
{
    private const string Token = nameof(AccessToken.Token);
    private const string ExpiresOn = nameof(AccessToken.ExpiresOn);

    public bool TryCreateFrom(object reflectedAuthTokenObject, out AccessToken authToken)
    {
        authToken = default;
        if (reflectedAuthTokenObject == null)
        {
            return false;
        }

        // String AuthToken
        if (reflectedAuthTokenObject is string stringToken)
        {
            authToken = FromStringToken(stringToken);
            return true;
        }

        try
        {
            authToken = FromAuthTokenObject(reflectedAuthTokenObject);
            return true;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            return false;
        }
    }

    /// <summary>
    /// Get an AuthToken object from
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private AccessToken FromStringToken(string token)
        => new AccessToken(token, ExtractExpiry(token));

    /// <summary>
    /// Token object that have to properties:
    /// Token: string
    /// ExpiresOn: DateTimeOffset
    /// </summary>
    /// <param name="tokenObject"></param>
    private AccessToken FromAuthTokenObject(object tokenObject)
    {
        TypeInfo tokenObjectTypeInfo = tokenObject.GetType().GetTypeInfo();

        // Gets token
        PropertyInfo tokenPropertyInfo = tokenObjectTypeInfo.GetDeclaredProperty(Token);
        string token = (string)tokenPropertyInfo.GetValue(tokenObject);

        // Gets expiresOn
        PropertyInfo expiresOnPropertyInfo = tokenObjectTypeInfo.GetDeclaredProperty(ExpiresOn);
        DateTimeOffset expiresOn = (DateTimeOffset)expiresOnPropertyInfo.GetValue(tokenObject);

        // Create access token.
        return new AccessToken(token, expiresOn);
    }

    /// <summary>
    /// Extracts expiry from a JWT access token by ClaimType of exp.
    /// </summary>
    private DateTimeOffset ExtractExpiry(string token)
    {
        JwtSecurityToken jwtToken = new JwtSecurityToken(token);
        long expiration = long.Parse(jwtToken.Claims.First(c => c.Type == "exp").Value, CultureInfo.InvariantCulture);
        return DateTimeOffset.FromUnixTimeSeconds(expiration);
    }
}
