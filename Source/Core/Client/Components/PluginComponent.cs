﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bricklayer.Core.Client.Interface.Windows;
using Bricklayer.Core.Common;
using MonoForce.Controls;
using Console = System.Console;

namespace Bricklayer.Core.Client.Components
{
    /// <summary>
    /// Handles plugin management.
    /// </summary>
    public class PluginComponent : ClientComponent
    {
        /// <summary>
        /// The number of plugins currently loaded.
        /// </summary>
        public int PluginCount => plugins.Count;

        private readonly List<ClientPlugin> plugins;

        public PluginComponent(Client client) : base(client)
        {
            plugins = new List<ClientPlugin>();
        }

        public override async Task Init()
        {
            if (!Client.IO.Initialized)
                throw new InvalidOperationException("The IO component must be initialized first.");

            LoadPlugins();

            Client.Events.Network.Auth.PluginDownload.AddHandler(args =>
            {
                if (!PluginDownloadWindow.IsDownloading(args.Message.ID))
                {
                    var pluginWindow = new PluginDownloadWindow(Client.UI, Client.Window, args.Message.ModName, args.Message.ID,
                        args.Message.FileName, false);
                    pluginWindow.Init();
                    Client.Window.Add(pluginWindow);
                    pluginWindow.Show();

                    Client.Network.PingAuthMessage("GotPlugin");
                }
            });

            await base.Init();
        }

        /// <summary>
        /// Loads all plugins that are not already loaded.
        /// </summary>
        internal void LoadPlugins()
        {
            //Get a list of all the .dlls in the directory
            IEnumerable<PluginData> files = null;
            try
            {
                files = IOHelper.GetPlugins(Client.IO.Directories["Plugins"], Client.IO.SerializationSettings);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            if (files != null)
            {
                foreach (var file in files.Where(file => !plugins.Contains(file)))
                {
                    //TODO: Use AppDomains for security
                    //Load the assembly
                    try
                    {
                        var asm = IOHelper.LoadPlugin(AppDomain.CurrentDomain, file.Path);
                        RegisterPlugin(IOHelper.CreatePluginInstance<ClientPlugin>(asm, Client, file));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }

        private void RegisterPlugin(ClientPlugin plugin)
        {
            plugins.Add(plugin);
            plugin.Load();
        }
    }
}