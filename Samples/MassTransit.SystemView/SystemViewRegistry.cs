// Copyright 2007-2011 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.

using System;
using MassTransit.Transports.ZeroMQ;
using ShortBus.ServiceBusHost;
using ShortBus.ZeroMqHost;

namespace MassTransit.SystemView
{

	public class SystemViewRegistry 

	{
		public IServiceBus GetBus(IConfiguration configuration)
		{

		    var bus = ServiceBusHost.Create(conf =>
		    {
                conf.SubscriptionServiceHost(configuration.SubscriptionServiceMachine);
                conf.UseZeroMq();
		    });

		    return bus.Bus as IServiceBus;
		    return ServiceBusFactory.New(sbc =>
				{
                    ZeroMqAddress.RegisterLocalPort(60002);
                    ZeroMqAddress.RegisterLocalPort(60003);
					//sbc.ReceiveFrom(configuration.SystemViewDataUri);
                    sbc.UseBsonSerializer();
                    sbc.SetNetwork(string.Empty);
				    sbc.UseZeroMq(config =>
				        {
				            //config.UseSubscriptionService(configuration.SubscriptionServiceUri);
				        });
                    
					sbc.SetConcurrentConsumerLimit(1);
                    sbc.UseControlBus();
				});

			
		}

	}
}