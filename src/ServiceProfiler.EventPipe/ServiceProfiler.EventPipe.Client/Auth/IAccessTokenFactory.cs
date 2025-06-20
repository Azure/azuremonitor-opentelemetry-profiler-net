using Azure.Core;

namespace Microsoft.ApplicationInsights.Profiler.Core.Auth
{
    internal interface IAccessTokenFactory
    {
        bool TryCreateFrom(object reflectedAuthTokenObject, out AccessToken authToken);
    }
}
