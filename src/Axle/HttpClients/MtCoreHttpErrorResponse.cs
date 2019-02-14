﻿// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Axle.HttpClients
{
    public class MtCoreHttpErrorResponse
    {
        public string ErrorMessage { get; set; }

        public override string ToString() => this.ErrorMessage;
    }
}
