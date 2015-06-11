﻿using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Bricklayer.Core.Client.Net.Messages.GameServer;
using Bricklayer.Core.Common;
using Bricklayer.Core.Common.Net;
using Bricklayer.Core.Common.Net.Messages;
using Lidgren.Network;

namespace Bricklayer.Core.Client.Net
{
    internal class MessageHandler
    {
        private readonly NetworkManager networkManager;
        private Thread messageThread;

        public MessageHandler(NetworkManager networkManager)
        {
            this.networkManager = networkManager;
        }

        public void Start()
        {
            messageThread = new Thread(ProcessNetworkMessages)
            {
                Name = "Message Handler"
            };
            messageThread.SetApartmentState(ApartmentState.STA);
            messageThread.Start();
        }

        /// <summary>
        /// Handles a data message (The bulk of all messages received, containing player movements, block places, etc)
        /// </summary>
        private void HandleDataMessage(NetIncomingMessage im)
        {
            if (im == null) throw new ArgumentNullException("im");

            var messageType = (MessageTypes)im.ReadByte();
        }

        /// <summary>
        /// Handles an unconnected message, usually from the auth server.
        /// </summary>
        private void HandleUnconnectedMessage(NetIncomingMessage im)
        {
            if (im == null) throw new ArgumentNullException(nameof(im));

            if (Equals(im.SenderEndPoint.Address, Dns.GetHostEntry(Globals.Values.DefaultAuthAddress).AddressList[0]) &&
                im.SenderEndPoint.Port == Globals.Values.DefaultAuthPort)
            {
                var messageType = (MessageTypes)im.ReadByte(); //Find the type of data message sent
                switch (messageType)
                {
                    case MessageTypes.AuthInit:
                    {
                        var msg = new AuthInitMessage(im, MessageContext.Client);
                        networkManager.Client.Events.Network.Auth.Init.Invoke(
                            new EventManager.NetEvents.AuthServerEvents.InitEventArgs(msg.Username, msg.UID, msg.PrivateKey,
                                msg.PublicKey));
                        break;
                    }
                    case MessageTypes.FailedLogin:
                    {
                        var msg = new FailedLoginMessage(im, MessageContext.Client);
                        networkManager.Client.Events.Network.Auth.FailedLogin.Invoke(
                            new EventManager.NetEvents.AuthServerEvents.FailedLoginEventArgs(msg.ErrorMessage));
                        break;
                    }
                    case MessageTypes.Verified:
                    {
                        var msg = new VerifiedMessage(im, MessageContext.Client);
                        networkManager.Client.Events.Network.Auth.Verified.Invoke(
                            new EventManager.NetEvents.AuthServerEvents.VerifiedEventArgs(msg.Verified));
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Processes any incoming messages.
        /// </summary>
        private void ProcessNetworkMessages()
        {
            while (networkManager.NetClient.Status == NetPeerStatus.Running)
            {
                if (networkManager.NetClient != null)
                {
                    //Block thread until next message
                    networkManager.NetClient.MessageReceivedEvent.WaitOne();

                    NetIncomingMessage im; //Holder for the incoming message
                    while ((im = networkManager.ReadMessage()) != null)
                    {
                        switch (im.MessageType)
                        {
                            case NetIncomingMessageType.ConnectionLatencyUpdated:
                                networkManager.Client.Events.Network.Game.LatencyUpdated.Invoke(
                                    new EventManager.NetEvents.GameServerEvents.LatencyUpdatedEventArgs(im.ReadSingle()));
                                break;
                            case NetIncomingMessageType.VerboseDebugMessage:
                            case NetIncomingMessageType.DebugMessage:
                            case NetIncomingMessageType.WarningMessage:
                            case NetIncomingMessageType.ErrorMessage:
                                Debug.WriteLine(im.ToString());
                                break;
                            case NetIncomingMessageType.StatusChanged:
                            {
                                var status = (NetConnectionStatus)im.ReadByte();
                                switch (status)
                                {
                                    case NetConnectionStatus.None:
                                    {
                                        networkManager.Client.Events.Network.Game.Disconnect.Invoke(
                                            new EventManager.NetEvents.GameServerEvents.DisconnectEventArgs(
                                                "Error connecting to the server."));
                                        break;
                                    }
                                    //When connected to the server
                                    case NetConnectionStatus.Connected:
                                    {
                                        //Must read the first byte of the hail message, which should correspond to the byte of the Init type
                                        im.SenderConnection.RemoteHailMessage.ReadByte(); //Throw it away
                                        var msg = new InitMessage(im.SenderConnection.RemoteHailMessage,
                                            MessageContext.Client);
                                        networkManager.Client.Events.Network.Game.Connect.Invoke(
                                            new EventManager.NetEvents.GameServerEvents.ConnectEventArgs());
                                        break;
                                    }
                                    //When disconnected from the server
                                    case NetConnectionStatus.Disconnected:
                                    {
                                        var reason = im.ReadString();
                                        networkManager.Client.Events.Network.Game.Disconnect.Invoke(
                                            new EventManager.NetEvents.GameServerEvents.DisconnectEventArgs(reason));
                                        break;
                                    }
                                    case NetConnectionStatus.RespondedAwaitingApproval:
                                    {
                                        im.SenderConnection.Approve();
                                        break;
                                    }
                                }
                                break;
                            }
                            //Data messages are sent by a connected server. (Such as a game server)
                            case NetIncomingMessageType.Data:
                            {
                                HandleDataMessage(im);
                                break;
                            }
                            //Unconnected messages are sent by a server, without creating a full connection. (Used for the auth server)
                            case NetIncomingMessageType.UnconnectedData:
                            {
                                HandleUnconnectedMessage(im);
                                break;
                            }
                        }
                        networkManager.Recycle(im);
                    }
                }
            }
        }
    }
}