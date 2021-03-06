﻿using System;
using System.IO;
using System.Linq;
using Bricklayer.Core.Client.Components;
using Bricklayer.Core.Client.Interface;
using Bricklayer.Core.Common.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoForce.Controls;
using EventArgs = System.EventArgs;
using Level = Bricklayer.Core.Client.World.Level;

namespace Bricklayer.Core.Client
{
    /// <summary>
    /// The main class of the Bricklayer client.
    /// </summary>
    public class Client : Game
    {
        /// <summary>
        /// Manages and handles all game assets and content.
        /// </summary>
        public new ContentManager Content { get; private set; }

        public EventManager Events { get; }

        /// <summary>
        /// The game's graphics manager.
        /// </summary>
        public GraphicsDeviceManager Graphics { get; }

        /// <summary>
        /// Manages and handles game input from the mouse and keyboard.
        /// </summary>
        public InputHandler Input { get; private set; }

        /// <summary>
        /// Manages and handles file operations.
        /// </summary>
        public IOComponent IO { get; set; }

        /// <summary>
        /// The PluginComponent for loading and managing plugins.
        /// </summary>
        public PluginComponent Plugins { get; set; }

        /// <summary>
        /// The game's sprite batch for drawing content.
        /// </summary>
        public SpriteBatch SpriteBatch { get; private set; }

        /// <summary>
        /// The current state of the game. (Login, server list, in game, etc.)
        /// </summary>
        public GameState State
        {
            get { return state; }
            set
            {
                if (State != value)
                {
                    Events.Game.StateChanged.Invoke(new EventManager.GameEvents.GameStateEventArgs(State, value));
                    state = value;
                }
            }
        }

        /// <summary>
        /// The MonoForce UI manager.
        /// </summary>
        public Manager UI { get; }

        /// <summary>
        /// The current level.
        /// </summary>
        public Level Level { get; internal set; }

        /// <summary>
        /// The main window, which is the root of all UI controls.
        /// </summary>
        public new MainWindow Window { get; private set; }

        /// <summary>
        /// Handles receiving and sending of game server network messages
        /// </summary>
        internal NetworkManager Network { get; private set; }

        /// <summary>
        /// Texture loader for loading texture assets.
        /// </summary>
        internal TextureLoader TextureLoader { get; private set; }

        private GameState state;

        internal Client()
        {
            Events = new EventManager();
            Graphics = new GraphicsDeviceManager(this);
            IsFixedTimeStep = false;
            Graphics.SynchronizeWithVerticalRetrace = false;

            AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;

            // Create the manager for MonoForce UI
            UI = new Manager(this, "Bricklayer")
            {
                TargetFrames = 10000,
                AutoCreateRenderTarget = true,
                LogUnhandledExceptions = false,
                ShowSoftwareCursor = true
            };
        }

        private void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // TODO: Save to a file (not important until later)
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values. (Delta time)</param>
        protected override void Draw(GameTime gameTime)
        {
            UI.BeginDraw(gameTime);
            GraphicsDevice.Clear(Color.Black);

            if (State == GameState.Game)
            {
                SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                Level?.Draw(SpriteBatch, gameTime);
                SpriteBatch.End();
            }

            UI.EndDraw();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            SpriteBatch = new SpriteBatch(Graphics.GraphicsDevice);
            Input = new InputHandler();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override async void LoadContent()
        {
            base.LoadContent();

            IO = new IOComponent(this);
            Plugins = new PluginComponent(this);
            Network = new NetworkManager(this);

            await IO.Init();
            await IO.LoadConfig();
            await Network.Init();

            Content = new ContentManager();
            TextureLoader = new TextureLoader(Graphics.GraphicsDevice);
            Content.LoadTextures(Path.Combine(IO.Directories["Content"], "Textures"), this);

            if (IO.Config.Client.Resolution.X == 0 && IO.Config.Client.Resolution.Y == 0)
            {
                Graphics.PreferredBackBufferWidth = (int) (GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width*.9);
                Graphics.PreferredBackBufferHeight =
                    (int) (GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height*.8);
                Graphics.ApplyChanges();
                base.Window.Position =
                    new Point(
                        (GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - Graphics.PreferredBackBufferWidth)/
                        2,
                        (GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - Graphics.PreferredBackBufferHeight)/
                        2);
            }
            else
            {
                Graphics.PreferredBackBufferWidth = IO.Config.Client.Resolution.X;
                Graphics.PreferredBackBufferHeight = IO.Config.Client.Resolution.Y;
                Graphics.ApplyChanges();
            }

            // Initialize MonoForce after loading skins.
            UI.Initialize();
            SpriteBatch = UI.Renderer.SpriteBatch; // Set the spritebatch to the Neoforce managed one


            // Create the main window for all content to be added to.
            Window = new MainWindow(UI, this);
            Window.Init();
            UI.Add(Window);
            Window.SendToBack();

            // Unlike the server, the clientside plugins must be loaded last.
            await Plugins.Init();

            // Now that all the textures have been loaded, set the block textures
            foreach (var block in BlockType.Blocks.Where(x => x.IsRenderable))
            {
                var str = "blocks." + (block.Category != null ? block.Category + "." : string.Empty) + block.Name;
                block.Texture = Content[str];
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values. (Delta time)</param>
        protected override void Update(GameTime gameTime)
        {
            UI.Update(gameTime);

            if (State == GameState.Game)
            {
                Level?.Update(gameTime);
            }

            base.Update(gameTime);
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            Network.Disconnect("Exited game.");
            base.OnExiting(sender, args);
        }
    }
}