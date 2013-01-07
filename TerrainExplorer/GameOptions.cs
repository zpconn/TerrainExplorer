#region Using Statements
using System;
using Microsoft.Xna.Framework;
#endregion

namespace TerrainExplorer
{
    /// <summary>
    /// This class stores a bunch of static game settings to be accessed from anywhere. Generally this level of global
    /// access is not a good design decision, but for a project that isn't huge, like this one, we can get by with it.
    /// </summary>
    public class GameOptions
    {
        /// <summary>
        /// The rate at which the camera accelerates when responding to user input.
        /// </summary>
        public static float CameraAccelerationMagnitude = 100000.0f;

        /// <summary>
        /// The rate at which the camera rotates.
        /// </summary>
        public static float CameraRotationSpeed = MathHelper.TwoPi / 4.0f;

        /// <summary>
        /// The rate at which the camera's linear velocity decays away.
        /// </summary>
        public static float CameraVelocityDecayRate = 0.7f;

        /// <summary>
        /// The maximum height of the terrain.
        /// </summary>
        public static float TerrainMaxHeight = 2048 * 11 * 2;

        /// <summary>
        /// This scales the x- and z-coordinates of the terrain mesh.
        /// </summary>
        public static float TerrainScale = 2048 * 76 * 24;

        /// <summary>
        /// This is the height of the water plane.
        /// </summary>
        public static float WaterHeight = 500.0f;
    }
}