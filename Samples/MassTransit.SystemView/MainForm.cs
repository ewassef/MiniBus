﻿// Copyright 2007-2011 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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

using MassTransit.Services.HealthMonitoring;
using ShortBus.Hostable.Shared.Specialized;
using ShortBus.ServiceBusHost;

namespace MassTransit.SystemView
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using Distributor.Messages;
    using Services.HealthMonitoring.Messages;
    using Services.Subscriptions.Messages;
    using Services.Timeout.Messages;

    public partial class MainForm :
        Form
    {
        readonly Guid _clientId = NewId.NextGuid();
        ServiceBusHost _bus;
        IEndpoint _subscriptionServiceEndpoint;
        UnsubscribeAction _unsubscribe;
        private HealthClient _client;
        public MainForm()
        {
            InitializeComponent();
        }

        public void Consume(AddSubscription message)
        {
            Action<SubscriptionInformation> method = x => AddSubscriptionToView(x);
            BeginInvoke(method, new object[] {message.Subscription});
        }

        public void Consume(EndpointIsDown message)
        {
            Action<EndpointIsDown> method = x => AddOrUpdateHealthItem(x.ControlUri, x.LastHeartbeat, x.State);
            BeginInvoke(method, new object[] {message});
        }

        public void Consume(EndpointIsHealthy message)
        {
            Action<EndpointIsHealthy> method = x => AddOrUpdateHealthItem(x.ControlUri, x.LastHeartbeat, x.State);
            BeginInvoke(method, new object[] {message});
        }

        public void Consume(EndpointIsOffline message)
        {
            Action<EndpointIsOffline> method = x => AddOrUpdateHealthItem(x.ControlUri, x.LastHeartbeat, x.State);
            BeginInvoke(method, new object[] {message});
        }

        public void Consume(EndpointIsSuspect message)
        {
            Action<EndpointIsSuspect> method = x => AddOrUpdateHealthItem(x.ControlUri, x.LastHeartbeat, x.State);
            BeginInvoke(method, new object[] {message});
        }

        public void Consume(HealthUpdate message)
        {
            Action<IEnumerable<HealthInformation>> method = RefreshHealthView;
            BeginInvoke(method, new object[] {message.Information});
        }

        public void Consume(IWorkerAvailable message)
        {
        }

        public void Consume(RemoveSubscription message)
        {
            lock (this)
            {
                Action<SubscriptionInformation> method = RemoveSubscriptionFromView;
                BeginInvoke(method, new object[] { message.Subscription }); 
            }
        }

        public void Consume(SubscriptionRefresh message)
        {
            Action<IEnumerable<SubscriptionInformation>> method = RefreshSubscriptions;
            BeginInvoke(method, new object[] {message.Subscriptions});
        }

        public void Consume(TimeoutExpired message)
        {
            Action<TimeoutExpired> method = x => RemoveTimeoutFromListView(x.CorrelationId, x.Tag);
            BeginInvoke(method, new object[] {message});
        }

        public void Consume(TimeoutRescheduled message)
        {
            Action<TimeoutRescheduled> method = x => AddOrUpdateTimeoutListView(x.CorrelationId, x.Tag, x.TimeoutAt);
            BeginInvoke(method, new object[] {message});
        }

        public void Consume(TimeoutScheduled message)
        {
            Action<TimeoutScheduled> method = x => AddOrUpdateTimeoutListView(x.CorrelationId, x.Tag, x.TimeoutAt);
            BeginInvoke(method, new object[] {message});
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            
            

            _unsubscribe();
            _bus.Cleanup();
            _bus.Dispose();
            _bus = null;

            base.OnClosing(e);
        }

        void RefreshHealthView(IEnumerable<HealthInformation> informations)
        {
            List<ListViewItem> existing = healthListView.Items.Cast<ListViewItem>().ToList();

            foreach (HealthInformation entry in informations)
            {
                ListViewItem item = AddOrUpdateHealthItem(entry.ControlUri, entry.LastHeartbeat, entry.State);

                if (existing.Contains(item))
                    existing.Remove(item);
            }

            foreach (ListViewItem item in existing)
            {
                item.Remove();
            }
        }

        ListViewItem AddOrUpdateHealthItem(Uri controlUri, DateTime lastHeartbeat, string state)
        {
            string key = controlUri.ToString();

            ListViewItem item;
            if (healthListView.Items.ContainsKey(key))
            {
                item = healthListView.Items[key];
                item.SubItems[1].Text = lastHeartbeat.ToLocalTime().ToShortTimeString();
                item.SubItems[2].Text = state;
            }
            else
            {
                item = healthListView.Items.Add(key, controlUri.ToString(), 0);

                item.SubItems.Add(new ListViewItem.ListViewSubItem(item, lastHeartbeat.ToLocalTime().ToShortTimeString()));
                item.SubItems.Add(new ListViewItem.ListViewSubItem(item, state));
            }
            return item;
        }

        void MainFormLoad(object sender, EventArgs e)
        {
            BootstrapContainer();

            BootstrapServiceBus();

            ConnectToSubscriptionService();

            _client = new HealthClient(30);
            _client.Start(_bus.Bus as IServiceBus);
        }

        void ConnectToSubscriptionService()
        {
            var innerBus = _bus.Bus as IServiceBus;
            var subscriptionUri = new Uri(String.Format("tcp://{0}:50000/", _configuration.SubscriptionServiceMachine));
            _subscriptionServiceEndpoint =
                innerBus.GetEndpoint(subscriptionUri);

            _subscriptionServiceEndpoint.Send(new AddSubscriptionClient(_clientId, innerBus.Endpoint.Address.Uri,
                innerBus.Endpoint.Address.Uri));

            _unsubscribe = innerBus.SubscribeHandler<SubscriptionRefresh>(Consume);
            _unsubscribe += innerBus.SubscribeHandler<AddSubscription>(Consume);
            _unsubscribe += innerBus.SubscribeHandler<RemoveSubscription>(Consume);
            _unsubscribe += innerBus.SubscribeHandler<AddSubscriptionClient>(Consume);
            _unsubscribe += innerBus.SubscribeHandler<RemoveSubscriptionClient>(Consume);
            _unsubscribe += innerBus.SubscribeHandler<HealthUpdate>(Consume);
            _unsubscribe += innerBus.SubscribeHandler<TimeoutScheduled>(Consume);
            _unsubscribe += innerBus.SubscribeHandler<TimeoutRescheduled>(Consume);
            _unsubscribe += innerBus.SubscribeHandler<TimeoutExpired>(Consume);
            _unsubscribe += innerBus.SubscribeHandler<EndpointIsHealthy>(Consume);
            _unsubscribe += innerBus.SubscribeHandler<EndpointIsDown>(Consume);
            _unsubscribe += innerBus.SubscribeHandler<EndpointIsSuspect>(Consume);
            _unsubscribe += innerBus.SubscribeHandler<EndpointIsOffline>(Consume);
            _unsubscribe += innerBus.SubscribeHandler<IWorkerAvailable>(Consume);
            _unsubscribe += innerBus.SubscribeHandler<PerformanceUpdate>(Consume);
        }

        void BootstrapServiceBus()
        {
            _bus = new SystemViewRegistry().GetBus(_configuration);
            
            
        }

        private IConfiguration _configuration;
        void BootstrapContainer()
        {
            _configuration = new Configuration();
        }

        TreeNode AddSubscriptionToView(SubscriptionInformation subscription)
        {
            TreeNode endpointNode;
            if (!subscriptionView.Nodes.ContainsKey(subscription.EndpointUri.ToString()))
            {
                endpointNode = new TreeNode(subscription.EndpointUri.ToString());
                endpointNode.Name = subscription.EndpointUri.ToString();

                subscriptionView.Nodes.Add(endpointNode);
            }
            else
            {
                endpointNode = subscriptionView.Nodes[subscription.EndpointUri.ToString()];
            }

            string messageName = subscription.MessageName;

            string description = GetDescription(subscription);

            TreeNode messageNode;
            if (!endpointNode.Nodes.ContainsKey(messageName))
            {
                messageNode = new TreeNode(description);

                if (messageName.StartsWith("MassTransit"))
                    messageNode.ForeColor = Color.DimGray;

                messageNode.Name = messageName;
                messageNode.Tag = subscription;

                endpointNode.Nodes.Add(messageNode);
            }
            else
            {
                messageNode = endpointNode.Nodes[messageName];
                if (messageNode.Text != description)
                    messageNode.Text = description;
            }

            return messageNode;
        }

        void RemoveSubscriptionFromView(SubscriptionInformation subscription)
        {
            if (!subscriptionView.Nodes.ContainsKey(subscription.EndpointUri.ToString()))
                return;

            TreeNode endpointNode = subscriptionView.Nodes[subscription.EndpointUri.ToString()];

            string messageName = subscription.MessageName;

            if (!endpointNode.Nodes.ContainsKey(messageName))
                return;

            TreeNode messageNode = endpointNode.Nodes[messageName];

            messageNode.Remove();

            if (endpointNode.Nodes.Count == 0)
            {
                endpointNode.Remove();
            }
        }

        void RefreshSubscriptions(IEnumerable<SubscriptionInformation> subscriptions)
        {
            var existingNodes = new List<TreeNode>();
            foreach (TreeNode endpointNode in subscriptionView.Nodes)
            {
                foreach (TreeNode subscriptionNode in endpointNode.Nodes)
                {
                    existingNodes.Add(subscriptionNode);
                }
            }

            foreach (SubscriptionInformation subscription in subscriptions)
            {
                TreeNode messageNode = AddSubscriptionToView(subscription);

                if (existingNodes.Contains(messageNode))
                    existingNodes.Remove(messageNode);
            }

            foreach (TreeNode node in existingNodes)
            {
                node.Remove();
            }
        }

        void AddOrUpdateTimeoutListView(Guid correlationId, int tag, DateTime timeoutAt)
        {
            string key = correlationId + "." + tag;

            ListViewItem item;
            if (timeoutListView.Items.ContainsKey(key))
            {
                item = timeoutListView.Items[key];
                item.SubItems[0].Text = timeoutAt.ToLocalTime().ToLongTimeString();
            }
            else
            {
                item = timeoutListView.Items.Add(key, timeoutAt.ToLocalTime().ToLongTimeString(), 0);

                item.SubItems.Add(new ListViewItem.ListViewSubItem(item, correlationId.ToString()));
                item.SubItems.Add(new ListViewItem.ListViewSubItem(item, tag.ToString()));
            }
        }

        void RemoveTimeoutFromListView(Guid correlationId, int tag)
        {
            string key = correlationId + "." + tag;

            if (timeoutListView.Items.ContainsKey(key))
            {
                timeoutListView.Items.RemoveByKey(key);
            }
        }

        void SubscriptionViewAfterSelect(object sender, TreeViewEventArgs e)
        {
            subscriptionView.SelectedNode = e.Node;
            endpointInfo.Bind(e.Node.Tag as SubscriptionInformation);
        }

        void RemoveToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (subscriptionView.SelectedNode != null)
            {
                RemoveSubscriptions(subscriptionView.SelectedNode);
            }
        }

        void SubscriptionViewPreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                RemoveSubscriptions(subscriptionView.SelectedNode);
            }
        }

        void RemoveSubscriptions(TreeNode node)
        {
            var toRemove = new List<SubscriptionInformation>();
            if (IsRemovable(node))
            {
                toRemove.Add((SubscriptionInformation) node.Tag);
            }

            toRemove.AddRange(node.Nodes.Cast<TreeNode>().Where(IsRemovable).Select(x => x.Tag)
                .Cast<SubscriptionInformation>().ToList());

            if (toRemove.Count == 0)
            {
                return;
            }

            string confirmMessage = string.Format("Are you sure you want to remove these subscriptions?{0}{0}{1}",
                Environment.NewLine, string.Join(Environment.NewLine, toRemove.Select(x =>
                                                                                      string.Format("{0} -> {1}",
                                                                                          x.EndpointUri,
                                                                                          GetDescription(x))).ToArray()));

            if (DialogResult.OK !=
                MessageBox.Show(confirmMessage, "Confirm Remove Subscriptions", MessageBoxButtons.OKCancel))
            {
                return;
            }

            toRemove.ForEach(x => _subscriptionServiceEndpoint.Send(new RemoveSubscription(x)));
        }

        static string GetDescription(SubscriptionInformation subscription)
        {
            string[] parts = subscription.MessageName.Split(',');
            string d = parts.Length > 0 ? parts[0] : subscription.MessageName;
            string[] dd = d.Split('.');

            string description = dd[dd.Length - 1];

            string[] gs = subscription.MessageName.Split('`');
            if (gs.Length > 1)
            {
                var generics = new Queue<string>(gs.Reverse().Skip(1).Reverse());

                while (generics.Count > 0)
                {
                    string g = generics.Dequeue();
                    string[] gg = g.Split('.');
                    string ggg = gg.Length > 0 ? gg[gg.Length - 1] : g;

                    description = string.Format("{0}<{1}>", ggg, description);
                }
            }

            if (!string.IsNullOrEmpty(subscription.CorrelationId))
                description += " (" + subscription.CorrelationId + ")";
            return description;
        }

        static bool IsRemovable(TreeNode node)
        {
            return node.Tag is SubscriptionInformation &&
                   !((SubscriptionInformation) node.Tag).MessageName.StartsWith("MassTransit.Services");
        }

        public void Consume(PerformanceUpdate message)
        {
                    }

        public void Consume(AddSubscriptionClient message)
        {
            var action = new Action(() =>
                {
                    TreeNode endpointNode;
                    if (!subscriptionView.Nodes.ContainsKey(message.DataUri.ToString()))
                    {
                        endpointNode = new TreeNode(message.DataUri.ToString());
                        endpointNode.Name = message.DataUri.ToString();

                        subscriptionView.Nodes.Add(endpointNode);
                    }
                });
            BeginInvoke(action);
            action = new Action(() =>
            {
                TreeNode endpointNode;
                if (!subscriptionView.Nodes.ContainsKey(message.ControlUri.ToString()))
                {
                    endpointNode = new TreeNode(message.ControlUri.ToString());
                    endpointNode.Name = message.ControlUri.ToString();

                    subscriptionView.Nodes.Add(endpointNode);
                }
            });
            BeginInvoke(action);

        }

        public void Consume(RemoveSubscriptionClient message)
        {
            var action = new Action(() =>
            {
                if (!subscriptionView.Nodes.ContainsKey(message.ControlUri.ToString()))
                    return;

                TreeNode endpointNode = subscriptionView.Nodes[message.ControlUri.ToString()];
                endpointNode.Remove();

            });
            BeginInvoke(action);
            action = new Action(() =>
            {
                if (!subscriptionView.Nodes.ContainsKey(message.DataUri.ToString()))
                    return;

                TreeNode endpointNode = subscriptionView.Nodes[message.DataUri.ToString()];
                endpointNode.Remove();
            });
            BeginInvoke(action);
        }
    }
}