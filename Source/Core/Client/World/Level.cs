﻿using System;
using Bricklayer.Core.Common.Data;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Bricklayer.Core.Client.World
{
    public class Level : Common.World.Level
    {
        /// <summary>
        /// The main camera to follow the player.
        /// </summary>
        public Camera Camera { get; set; }

        private static readonly float cameraSpeed = .18f;
        private static readonly float cameraParallax = .9f;

        public Level(PlayerData creator, string name, Guid uuid, string description, int plays, double rating) : base(creator, name, uuid, description, plays, rating)
        {

        }

        public Level(LevelData level) : base(level)
        {

        }

        /// <summary>
        /// Converts an instance of a common level to a client level.
        /// </summary>
        public Level(Common.World.Level level) : base(level)
        {
            Tiles = level.Tiles;
            Width = level.Width;
            Height = level.Width;
            Players = level.Players;
            Spawn = level.Spawn;
        }

        public void Update(GameTime delta)
        {
            
        }

        public void Draw(SpriteBatch batch, GameTime delta)
        {
            
        }
    }
}