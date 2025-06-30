//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection
{
    internal interface IServiceCollectionBuilder
    {
        IServiceCollection Build(IServiceCollection serviceCollection);
    }
}