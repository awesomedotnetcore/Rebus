﻿using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Bugs
{
    [TestFixture, Ignore("Not sure that this is how it is supposed to work")]
    public class DeferralOfMessageMappedToAnotherEndpoint : FixtureBase
    {
        readonly InMemNetwork _network = new InMemNetwork();

        protected override void SetUp()
        {
            _network.Reset();
        }

        [Test]
        public async Task EndpointMappingsAreUsedWhenDeferringMessages()
        {
            var gotTheString = new ManualResetEvent(false);

            var a = CreateBus("endpoint-a", c =>
            {
                c.Routing(r => r.TypeBased().Map<string>("endpoint-b"));
            });

            var b = CreateBus("endpoint-b");

            b.Handle<string>(async str => gotTheString.Set());

            await a.Bus.Defer(TimeSpan.FromSeconds(0.1), "HEJ MED DIG MIN VEEEEEEEEEEEEEEEEN");

            gotTheString.WaitOrDie(TimeSpan.FromSeconds(2), "Did not get the expected string within 2 s timeout");
        }

        BuiltinHandlerActivator CreateBus(string queueName, Action<RebusConfigurer> additionalConfiguration = null)
        {
            var activator = new BuiltinHandlerActivator();

            Using(activator);

            var rebusConfigurer = Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(_network, queueName))
                .Timeouts(t => t.StoreInMemory());

            additionalConfiguration?.Invoke(rebusConfigurer);

            rebusConfigurer.Start();

            return activator;
        }
    }
}