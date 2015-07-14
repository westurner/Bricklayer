﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Bricklayer.Core.Common.World
{
    /// <summary>
    /// A tile's block type (Ex: Dirt, Stone, etc)
    /// </summary>
    public class BlockType
    {
        /// <summary>
        /// List of all block types.
        /// </summary>
        public static List<BlockType> Blocks;

        /// <summary>
        /// How players should collide with this tile. (Only used for foregrounds).
        /// </summary>
        public BlockCollision Collision { get; set; }

        /// <summary>
        /// The average color of the tile to appear on the minimap.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// ID of this block used for loading and saving blocks.
        /// A list of IDs is generated by the server at startup, depending on what plugins are loaded and how many blocks they
        /// contain.
        /// </summary>
        public short ID { get; }

        /// <summary>
        /// Defines if this block is a background or foreground.
        /// </summary>
        public Layer Layer { get; set; }

        /// <summary>
        /// Name of the block.
        /// </summary>
        public string Name { get; private set; }

        static BlockType()
        {
            Blocks = new List<BlockType>();
        }

        /// <summary>
        /// Creates a new instance a block type.
        /// </summary>
        /// <param name="name">Name of the block</param>
        public BlockType(string name, Layer layer, BlockCollision collision = BlockCollision.Passable)
        {
            //TODO: ID will be calulcated by server, and the list will be rearranged after plugins are loaded
            //This way we can use an array or list instead of LINQ/for loop to find an id in `FromID`
            Name = name;
            Layer = layer;
            Collision = collision;
            ID = (byte)Blocks.Count();
            Blocks.Add(this);
        }

        /// <summary>
        /// Finds a <c>BlockType</c> from it's ID
        /// </summary>
        public static BlockType FromID(short ID)
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            // ReSharper disable once LoopCanBeConvertedToQuery
            // For loop for optimization as this is called for every tile access
            for (var i = 0; i < Blocks.Count; i++)
            {
                var x = Blocks[i];
                if (x.ID == ID) return x;
            }
            return null;
        }
    }
}