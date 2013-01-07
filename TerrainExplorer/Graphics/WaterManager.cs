#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
#endregion

namespace TerrainExplorer
{
    /// <summary>
    /// This class manages the logstics behind rendering a flat plane of normal mapped water using refraction and reflection blended with the Fresnel term.
    /// </summary>
    public class WaterManager
    {
        #region Fields

        Game game;

        float waterHeight = 500.0f;

        RenderTarget2D refractionRenderTarget;
        Texture2D refractionMap;
        Plane refractionClippingPlane;

        #endregion

        #region Delegates

        public delegate void RenderSceneDelegate();

        #endregion

        #region Properties

        /// <summary>
        /// Gets a reference to the main game object.
        /// </summary>
        public Game Game
        {
            get { return game; }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// This creates the needed graphics resources to render the water.
        /// </summary>
        public WaterManager(Game game, float waterHeight)
        {
            this.game = game;
            this.waterHeight = waterHeight;

            GraphicsDevice graphicsDevice = game.GraphicsDevice;
            PresentationParameters presentParams = graphicsDevice.PresentationParameters;

            refractionRenderTarget = new RenderTarget2D(graphicsDevice, presentParams.BackBufferWidth, presentParams.BackBufferHeight, 1, graphicsDevice.DisplayMode.Format);
        }

        #endregion

        #region Draw

        /// <summary>
        /// This draws the scene with reflective and refractive water. It takes a delegate function to perform the actual scene rendering needed to construct the refraction
        /// and reflection maps.
        /// </summary>
        /// <param name="camera"></param>
        public void Draw(Camera camera, RenderSceneDelegate RenderScene)
        {
            GraphicsDevice graphicsDevice = game.GraphicsDevice;

            // Render the refraction and reflection maps

            DrawRefractionMap(graphicsDevice, camera, RenderScene);

        }

        /// <summary>
        /// This renders the refraction map;
        /// </summary>
        private void DrawRefractionMap(GraphicsDevice device, Camera camera, RenderSceneDelegate RenderScene)
        {
            // Clipping computations will be done after vertices have passed through the vertex shader and are in screen space. We must therefore represent the clipping planes
            // screen space as well, which is achieved by multiplying the plane coefficients by the view and projection matrices.

            Vector4 refractionPlaneCoefficients = new Vector4(0.0f, 1.0f, 0.0f, -waterHeight);
            refractionPlaneCoefficients = Vector4.Transform(refractionPlaneCoefficients, Matrix.Transpose(Matrix.Invert(camera.ViewMatrix * camera.ProjMatrix)));

            refractionClippingPlane = new Plane(refractionPlaneCoefficients);

            // Prepare the device for rendering to the desired render target

            device.ClipPlanes[0].Plane = refractionClippingPlane;
            device.ClipPlanes[0].IsEnabled = true;
            device.SetRenderTarget(0, refractionRenderTarget);
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);

            // Render the scene with clipping

            RenderScene();

            // Now disable the clipping plane and save the result to a texture

            device.ClipPlanes[0].IsEnabled = false;
            device.SetRenderTarget(0, null);
            refractionMap = refractionRenderTarget.GetTexture();
        }

        #endregion
    }
}