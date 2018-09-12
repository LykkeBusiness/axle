﻿// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Axle.Tests.Acceptance.Support
{
    using Xunit;

    [CollectionDefinition("Axle")]
    public class AxleCollection : ICollectionFixture<AxleFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
