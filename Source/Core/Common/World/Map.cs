﻿using System;
using System.Collections.Generic;
using System.Linq;
using Bricklayer.Core.Common.Data;
using Bricklayer.Core.Common.Entity;
using Microsoft.Xna.Framework;

namespace Bricklayer.Core.Common.World
{
    /// <summary>
    /// Represents a level/map, with an array of tiles and a list of players.
    /// </summary>
    public abstract class Map : LevelData
    {
        /// <summary>
        /// The height, in blocks, of the map
        /// </summary>
        public virtual int Height { get; protected set; }

        /// <summary>
        /// The number of players currently online this level.
        /// </summary>
        public override int Online => Players.Count;

        /// <summary>
        /// The list of players currently in the map, synced with the server
        /// </summary>
        public List<Player> Players { get; set; }

        /// <summary>
        /// The spawn point new players will originate from
        /// </summary>
        public virtual Vector2 Spawn { get; set; }

        /// <summary>
        /// The tile array for the map, containing all tiles and tile data [X, Y, Layer/Z]
        /// Layer 0 = Background
        /// Layer 1 = Foreground
        /// </summary>
        public virtual Tile[,,] Tiles { get; protected set; }

        /// <summary>
        /// The width, in blocks, of the map
        /// </summary>
        public virtual int Width { get; protected set; }

        protected Random random;

        public Map(string name, string uuid, string description, int players, int plays, double rating)
            : base(name, uuid, description, players, plays, rating)
        {
            random = new Random();
        }

        /// <summary>
        /// Determines if a grid position is in the bounds of the map array.
        /// </summary>
        public bool InBounds(int x, int y, int z = 1)
        {
            return !(y < 1 || y >= Height - 1 || x < 1 || x >= Width - 1 || z > 1 || z < 0);
        }

        /// <summary>
        /// Determines if a grid position is in the bounds of the drawing area (The map, but not the border)
        /// </summary>
        public bool InDrawBounds(int x, int y)
        {
            return !(y < 0 || y >= Height || x < 0 || x >= Width);
        }

        /// <summary>
        /// Returns a player from an ID.
        /// </summary>
        public Player PlayerFromID(int id)
        {
            var player = Players.FirstOrDefault(x => x.ID == id);
            if (player != null)
                return player;
            throw new KeyNotFoundException("Could not find player from ID: " + id);
        }
    }
}