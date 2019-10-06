﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Axle.Services
{
    using Axle.Dto;

    public interface INotificationService
    {
        Task PublishSessionTermination(TerminateSessionNotification terminateSessionNotification);

        Task PublishOtherTabsTermination(TerminateOtherTabsNotification terminateOtherTabsNotification);
    }
}
