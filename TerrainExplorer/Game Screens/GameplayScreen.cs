#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#endregion

namespace TerrainExplorer
{
    /// <summary>
    /// This screen implements the actual game logic.
    /// </summary>
    class GameplayScreen : GameScreen
    {
        #region Fields

        Camera camera;
        TerrainQuadTree terrain;
        SkyBox skyBox;
        WaterManager waterManager;

        #endregion

        #region Initialization

        /// <summary>
        /// Constructs a new gameplay screen.
        /// </summary>
        public GameplayScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(1.0);
            TransitionOffTime = TimeSpan.FromSeconds(1.0);
        }

        /// <summary>
        /// Loads graphics resources needed for the game.
        /// </summary>
        public override void LoadGraphicsContent(bool loadAllContent)
        {
            // Initialize the camera

            camera = new FirstPersonCamera(GameOptions.CameraAccelerationMagnitude, GameOptions.CameraRotationSpeed,
                                           GameOptions.CameraVelocityDecayRate);
            camera.AspectRatio = (float)ScreenManager.GraphicsDevice.Viewport.Width /
                                    (float)ScreenManager.GraphicsDevice.Viewport.Height;
            camera.Position = new Vector3(0, GameOptions.TerrainMaxHeight, 0);
            camera.Angles = new Vector3(-MathHelper.PiOver2, 0.0f, 0.0f);

            if (loadAllContent)
            {
                // Load the sky box
                skyBox = new SkyBox(ScreenManager.Game, ScreenManager.Content, camera);

                // Set up the terrain parameters
                TerrainQuadTreeParameters parameters = new TerrainQuadTreeParameters();

                parameters.HeightMapName = "Content\\Textures\\Heightmap";
                parameters.LayerMap0Name = "Content\\Textures\\grass01";
                parameters.LayerMap1Name = "Content\\Textures\\rock01";
                parameters.LayerMap2Name = "Content\\Textures\\snow01";
                parameters.GrassTextureName = "Content\\Textures\\grass";

                parameters.MaxHeight = GameOptions.TerrainMaxHeight;
                parameters.TerrainScale = GameOptions.TerrainScale;

                parameters.MaxScreenSpaceError = 0.075f;
                parameters.ScreenHeight = ScreenManager.Game.GraphicsDevice.Viewport.Height;
                parameters.FieldOfView = camera.FieldOfView;

                parameters.GrassChunkDimension = 30;
                parameters.GrassChunkStarSeparation = 266.6f;
                parameters.GrassStartFadingInDistance = 4000.0f;
                parameters.GrassStopFadingInDistance = 3900.0f;

                parameters.WindStrength = 100.0f;
                parameters.WindDirection = new Vector4(-1.0f, 0.0f, 0.0f, 0.0f);

                parameters.DoPreprocessing = true;

                parameters.ComputePerspectiveScalingFactor();

                terrain = new TerrainQuadTree(ScreenManager.Game, ScreenManager.Content, parameters);

                // Iniialize the water manager
                waterManager = new WaterManager(ScreenManager.Game, GameOptions.WaterHeight);
            }
        }

        /// <summary>
        /// Frees graphics content.
        /// </summary>
        public override void UnloadGraphicsContent(bool unloadAllContent)
        {
            if (unloadAllContent)
            {
            }
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Updates the state of the game. This method checks the GameScreen.IsActive property, so the game
        /// will stop updating when the pause menu is active or if the player tabs away to a different
        /// application.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            camera.Update(gameTime);
            terrain.Update(gameTime);
        }

        /// <summary>
        /// Lets the game respond to player input.
        /// </summary>
        public override void HandleInput()
        {
        }

         /// <summary>
        /// Draws the game.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // Draw the water
            waterManager.Draw(camera, DrawScene);

            // Now draw the scene itself
            DrawScene();

            // If the game is transitioning on or off, fade it out to black
            if (TransitionPosition > 0)
                ScreenManager.FadeBackBufferToBlack(ScreenManager.SpriteBatch, 255 - TransitionAlpha);
        }

        /// <summary>
        /// This is a delegate rendering function used for water rendering
        /// </summary>
        private void DrawScene()
        {
            skyBox.Draw();

            ScreenManager.GraphicsDevice.RenderState.DepthBufferEnable = true;
            ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;

            terrain.Draw(camera);
        }

        #endregion
    }
}