﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text;
using Bricklayer.Core.Client.Interface.Windows;
using Bricklayer.Core.Server.Data;
using MonoForce.Controls;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.IO;
using Bricklayer.Core.Common.Net;
using Bricklayer.Core.Common.Net.Messages;

namespace Bricklayer.Core.Client.Interface.Screens
{
    public class LobbyScreen : Screen
    {
        //Controls
        private LobbyWindow wndLobby;
        private ImageBox  imgBackground;
        private ImageBox imgBanner;
        private byte[] bannerInfo;

        public string Description { get; }
        public string Name { get; }
        public string Intro { get; }
        public int Online { get; }

        public List<LobbySaveData> Rooms { get; private set; }

        public LobbyScreen(string description, string name, string intro, int online, List<LobbySaveData> rooms)
        {
            Description = description;
            Name = name;
            Intro = intro;
            Online = online;
            Rooms = rooms;


        }
        public override void Add(ScreenManager screenManager)
        {
            base.Add(screenManager);
            Client.State = GameState.Lobby;

            imgBackground = new ImageBox(Manager)
            {
                Image = Client.Content["gui.background"],
                SizeMode = SizeMode.Stretched
            };
            imgBackground.SetSize(Window.Width, Window.Height);
            imgBackground.SetPosition(0, 0);
            imgBackground.Init();
            Window.Add(imgBackground);

            // When client gets banner data from server
            Client.Events.Network.Game.Banner.AddHandler(args =>
            {
                bannerInfo = args.Banner;
                var stream = new MemoryStream(bannerInfo);
                Texture2D image = Texture2D.FromStream(screenManager.Window.Client.GraphicsDevice, stream);

                imgBanner = new ImageBox(Manager)
                {
                    Image = image,
                };
                imgBanner.SetSize(424, 60);
                imgBanner.SetPosition(0, 0);
                imgBanner.Init();
                Window.Add(imgBanner);
            });

            //Add the login window
            wndLobby = new LobbyWindow(Manager, this);
            wndLobby.Init();
            Window.Add(wndLobby);
            wndLobby.Show();

            Client.Network.Send(new RequestMessage(MessageTypes.Banner));
        }

        public override void Remove()
        {
            Window.Remove(wndLobby);
            Window.Remove(imgBackground);
        }
    }
}
