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
    /// This class encapsulates the geometry and rendering methdos for a skybox. 
    /// </summary>
    public class SkyBox
    {
        #region Fields

        Texture2D[] textures;
        Model model;
        Effect effect;

        Camera camera;

        Game game;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a reference to the main game object.
        /// </summary>
        public Game Game
        {
            get { return game; }
        }

        /// <summary>
        /// This gets or sets the camera relative to which the skybox is rendered.
        /// </summary>
        public Camera Camera
        {
            get { return camera; }
            set { camera = value; }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// This loads the model and texture data needed to render the skybox.
        /// </summary>
        public SkyBox(Game game, ContentManager content, Camera camera)
        {
            this.game = game;
            this.camera = camera;

            GraphicsDevice graphicsDevice = game.GraphicsDevice;

            model = content.Load<Model>("Content\\Models\\Skybox");
            textures = new Texture2D[model.Meshes.Count];

            // Initialize the effect.

            effect = content.Load<Effect>("Content\\Shaders\\Skybox");

            // Save the textures in the mesh to the textures array.

            int textureCount = 0;

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect meshEffect in mesh.Effects)
                {
                    textures[textureCount] = meshEffect.Texture;
                    ++textureCount;
                }
            }

            // We need to pass our effect down to the child mesh parts.

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    part.Effect = effect.Clone(graphicsDevice);
                }
            }
        }

        #endregion

        #region Draw

        /// <summary>
        /// This method draws the skybox.
        /// </summary>
        public void Draw()
        {
            GraphicsDevice graphicsDevice = game.GraphicsDevice;

            // Prepare the device for the special circumstances of rendering this skybox.

            graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Clamp;
            graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Clamp;

            graphicsDevice.RenderState.DepthBufferWriteEnable = false;

            // Let's remember the bone transforms of the model

            Matrix[] skyboxTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(skyboxTransforms);

            // Now let's render the model, mesh by mesh.

            int textureIndex = 0;

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (Effect meshEffect in mesh.Effects)
                {
                    Matrix worldMatrix = skyboxTransforms[mesh.ParentBone.Index] * Matrix.CreateTranslation(camera.Position);

                    meshEffect.CurrentTechnique = effect.Techniques["SkyboxDraw"];
                    meshEffect.Parameters["world"].SetValue(worldMatrix);
                    meshEffect.Parameters["view"].SetValue(camera.ViewMatrix);
                    meshEffect.Parameters["proj"].SetValue(camera.ProjMatrix);
                    meshEffect.Parameters["textureMap"].SetValue(textures[textureIndex]);

                    ++textureIndex;
                }

                mesh.Draw();
            }

            graphicsDevice.RenderState.DepthBufferWriteEnable = true;
        }

        #endregion
    }
}