﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace WoL.Services
{
    public class CompositePingService : DnsPingServiceBase
    {
        private readonly RdpPortPingService rdpPing;
        private readonly IcmpPingService icmpPing;

        public CompositePingService(IAddressLookupService addressLookupService, RdpPortPingService rdpPing, IcmpPingService icmpPing, ILoggerFactory loggerFactory) : base(addressLookupService, loggerFactory.CreateLogger<DnsPingServiceBase>())
        {
            this.rdpPing = rdpPing;
            this.icmpPing = icmpPing;
        }

        public override async Task<bool> IsReachable(IPAddress ip, TimeSpan timeout)
        {
            // start pings in parallel
            var rdp = rdpPing.IsReachable(ip, timeout);
            var icmp = icmpPing.IsReachable(ip, timeout);

            var finished = await Task.WhenAny(rdp, icmp).ConfigureAwait(false);
            // if one finishes early and can connect, return early
            if (await finished.ConfigureAwait(false))
                return true;

            // otherwise wait for the second task and check if it is true
            if (finished == icmp && await rdp.ConfigureAwait(false))
                return true;
            if (finished == rdp && await icmp.ConfigureAwait(false))
                return true;

            return false;
        }
    }
}
