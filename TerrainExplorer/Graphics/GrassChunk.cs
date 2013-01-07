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
    /// This represents the geometry for a grid of grass stars, where each star is constructed from three criss-crossing
    /// vertical quads.
    /// </summary>
    public class GrassChunk
    {
        #region Fields

        Game game;
        GraphicsDevice graphicsDevice;

        TerrainQuadTree quadTree;

        VertexBuffer vertexBuffer;
        IndexBuffer indexBuffer;

        List<Vector3> starPositions;

        VertexPositionNormalTexture[] vertices;
        int[] indices;

        VertexDeclaration vertexDecl;

        int dimension;
        float starSeparation;

        float quadWidth = 400.0f;
        float quadHeight = 200.0f;

        float timer = 0.0f;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the dimension of the chunk, which describes the number of grass stars along any row or column.
        /// </summary>
        public int Dimension
        {
            get { return dimension; }
        }

        /// <summary>
        /// This determines the spacing between different grass stars.
        /// </summary>
        public float StarSeparation
        {
            get { return starSeparation; }
        }

        /// <summary>
        /// Gets the width in model space of each quad in the grass geometry.
        /// </summary>
        public float QuadWidth
        {
            get { return quadWidth; }
        }

        /// <summary>
        /// Gets the height in model space of each quad in the grass geometry.
        /// </summary>
        public float QuadHeight
        {
            get { return quadHeight; }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// This constructs a new GrassChunk, including the grass geometry.
        /// </summary>
        public GrassChunk(Game game, TerrainQuadTree quadTree, int dimension, float starSeparation)
        {
            this.game = game;
            this.quadTree = quadTree;
            this.dimension = dimension;
            this.starSeparation = starSeparation;

            starPositions = new List<Vector3>();

            LoadGraphicsContent();
        }

        /// <summary>
        /// This constructs a new GrassChunk, including the grass geometry.
        /// </summary>
        public GrassChunk(Game game, TerrainQuadTree quadTree, int dimension, float starSeparation,
                          float quadWidth, float quadHeight)
        {
            this.game = game;
            this.quadTree = quadTree;
            this.dimension = dimension;
            this.starSeparation = starSeparation;
            this.quadWidth = quadWidth;
            this.quadHeight = quadHeight;

            starPositions = new List<Vector3>();

            LoadGraphicsContent();
        }

        /// <summary>
        /// Creates the graphics resources needed to render the grass.
        /// </summary>
        private void LoadGraphicsContent()
        {
            GenerateStructure();

            IGraphicsDeviceService igs = (IGraphicsDeviceService)game.Services.GetService(typeof(IGraphicsDeviceService));
            graphicsDevice = igs.GraphicsDevice;

            int numVertices = dimension * dimension * 12;
            int numIndices = dimension * dimension * 18;

            vertexBuffer = new VertexBuffer(graphicsDevice, numVertices * VertexPositionNormalTexture.SizeInBytes,
                BufferUsage.None);
            indexBuffer = new IndexBuffer(graphicsDevice, numIndices * sizeof(int), BufferUsage.None, IndexElementSize.ThirtyTwoBits);

            vertexBuffer.SetData<VertexPositionNormalTexture>(vertices);
            indexBuffer.SetData<int>(indices);

            vertexDecl = new VertexDeclaration(graphicsDevice, VertexPositionNormalTexture.VertexElements);
        }

        /// <summary>
        /// Computes an index corresponding to the grass star at grid coordinates (i, j).
        /// </summary>
        private int StarIndex(int i, int j)
        {
            return j * dimension + i;
        }

        /// <summary>
        /// Creates the geometry for all the grass stars in one vertex/index buffer pair.
        /// </summary>
        private void GenerateStructure()
        {
            int numVertices = dimension * dimension * 12;
            int numIndices = dimension * dimension * 18;

            vertices = new VertexPositionNormalTexture[numVertices];
            indices = new int[numIndices];

            // Choose the positions first for each grass star

            float start = -dimension / 2.0f * starSeparation;

            for (int i = 0; i < dimension; ++i)
            {
                for (int j = 0; j < dimension; ++j)
                {
                    starPositions.Add(new Vector3(start + i * starSeparation + (j % 2 == 0 ? -starSeparation / 2.0f : 0.0f), 
                                                  0.0f, 
                                                  start + j * starSeparation + (i % 2 == 0 ? -starSeparation / 2.0f : 0.0f)));
                }
            }

            // Now, for each point in the starPositions list, construct a star of three criss-crossing quads

            for (int i = 0; i < dimension; ++i)
            {
                for (int j = 0; j < dimension; ++j)
                {
                    Vector3 position = starPositions[StarIndex(i, j)];
                    int starIndex = StarIndex(i, j);

                    // Quad 1

                    vertices[12 * starIndex + 0] = new VertexPositionNormalTexture();
                    vertices[12 * starIndex + 0].Position = new Vector3(quadWidth / 2.0f, 0.0f, 0.0f);
                    vertices[12 * starIndex + 0].TextureCoordinate = new Vector2(1.0f, 1.0f);
                    vertices[12 * starIndex + 0].Normal = Vector3.Up;

                    vertices[12 * starIndex + 1] = new VertexPositionNormalTexture();
                    vertices[12 * starIndex + 1].Position = new Vector3(quadWidth / 2.0f, quadHeight, 0.0f);
                    vertices[12 * starIndex + 1].TextureCoordinate = new Vector2(1.0f, 0.0f);
                    vertices[12 * starIndex + 1].Normal = Vector3.Up;

                    vertices[12 * starIndex + 2] = new VertexPositionNormalTexture();
                    vertices[12 * starIndex + 2].Position = new Vector3(-quadWidth / 2.0f, quadHeight, 0.0f);
                    vertices[12 * starIndex + 2].TextureCoordinate = new Vector2(0.0f, 0.0f);
                    vertices[12 * starIndex + 2].Normal = Vector3.Up;

                    vertices[12 * starIndex + 3] = new VertexPositionNormalTexture();
                    vertices[12 * starIndex + 3].Position = new Vector3(-quadWidth / 2.0f, 0.0f, 0.0f);
                    vertices[12 * starIndex + 3].TextureCoordinate = new Vector2(0.0f, 1.0f);
                    vertices[12 * starIndex + 3].Normal = Vector3.Up;

                    indices[18 * starIndex + 0] = 12 * starIndex + 0;
                    indices[18 * starIndex + 1] = 12 * starIndex + 1;
                    indices[18 * starIndex + 2] = 12 * starIndex + 2;

                    indices[18 * starIndex + 3] = 12 * starIndex + 0;
                    indices[18 * starIndex + 4] = 12 * starIndex + 2;
                    indices[18 * starIndex + 5] = 12 * starIndex + 3;

                    // Quad 2

                    vertices[12 * starIndex + 4] = new VertexPositionNormalTexture();
                    vertices[12 * starIndex + 4].Position = new Vector3(quadWidth / 2.0f * (float)Math.Cos(MathHelper.PiOver4), 0.0f, quadWidth / 2.0f * (float)Math.Sin(MathHelper.PiOver4));
                    vertices[12 * starIndex + 4].TextureCoordinate = new Vector2(1.0f, 1.0f);
                    vertices[12 * starIndex + 4].Normal = Vector3.Up;

                    vertices[12 * starIndex + 5] = new VertexPositionNormalTexture();
                    vertices[12 * starIndex + 5].Position = new Vector3(quadWidth / 2.0f * (float)Math.Cos(MathHelper.PiOver4), quadHeight, quadWidth / 2.0f * (float)Math.Sin(MathHelper.PiOver4));
                    vertices[12 * starIndex + 5].TextureCoordinate = new Vector2(1.0f, 0.0f);
                    vertices[12 * starIndex + 5].Normal = Vector3.Up;

                    vertices[12 * starIndex + 6] = new VertexPositionNormalTexture();
                    vertices[12 * starIndex + 6].Position = new Vector3(-quadWidth / 2.0f * (float)Math.Cos(MathHelper.PiOver4), quadHeight, -quadWidth / 2.0f * (float)Math.Sin(MathHelper.PiOver4));
                    vertices[12 * starIndex + 6].TextureCoordinate = new Vector2(0.0f, 0.0f);
                    vertices[12 * starIndex + 6].Normal = Vector3.Up;

                    vertices[12 * starIndex + 7] = new VertexPositionNormalTexture();
                    vertices[12 * starIndex + 7].Position = new Vector3(-quadWidth / 2.0f * (float)Math.Cos(MathHelper.PiOver4), 0.0f, -quadWidth / 2.0f * (float)Math.Sin(MathHelper.PiOver4));
                    vertices[12 * starIndex + 7].TextureCoordinate = new Vector2(0.0f, 1.0f);
                    vertices[12 * starIndex + 7].Normal = Vector3.Up;

                    indices[18 * starIndex + 6] = 12 * starIndex + 0 + 4;
                    indices[18 * starIndex + 7] = 12 * starIndex + 1 + 4;
                    indices[18 * starIndex + 8] = 12 * starIndex + 2 + 4;

                    indices[18 * starIndex + 9] = 12 * starIndex + 0 + 4;
                    indices[18 * starIndex + 10] = 12 * starIndex + 2 + 4;
                    indices[18 * starIndex + 11] = 12 * starIndex + 3 + 4;

                    // Quad 3

                    vertices[12 * starIndex + 8] = new VertexPositionNormalTexture();
                    vertices[12 * starIndex + 8].Position = new Vector3(quadWidth / 2.0f * (float)Math.Cos(3.0f * MathHelper.PiOver4), 0.0f, quadWidth / 2.0f * (float)Math.Sin(3.0f * MathHelper.PiOver4));
                    vertices[12 * starIndex + 8].TextureCoordinate = new Vector2(1.0f, 1.0f);
                    vertices[12 * starIndex + 8].Normal = Vector3.Up;

                    vertices[12 * starIndex + 9] = new VertexPositionNormalTexture();
                    vertices[12 * starIndex + 9].Position = new Vector3(quadWidth / 2.0f * (float)Math.Cos(3.0f * MathHelper.PiOver4), quadHeight, quadWidth / 2.0f * (float)Math.Sin(3.0f * MathHelper.PiOver4));
                    vertices[12 * starIndex + 9].TextureCoordinate = new Vector2(1.0f, 0.0f);
                    vertices[12 * starIndex + 9].Normal = Vector3.Up;

                    vertices[12 * starIndex + 10] = new VertexPositionNormalTexture();
                    vertices[12 * starIndex + 10].Position = new Vector3(-quadWidth / 2.0f * (float)Math.Cos(3.0f * MathHelper.PiOver4), quadHeight, -quadWidth / 2.0f * (float)Math.Sin(3.0f * MathHelper.PiOver4));
                    vertices[12 * starIndex + 10].TextureCoordinate = new Vector2(0.0f, 0.0f);
                    vertices[12 * starIndex + 10].Normal = Vector3.Up;

                    vertices[12 * starIndex + 11] = new VertexPositionNormalTexture();
                    vertices[12 * starIndex + 11].Position = new Vector3(-quadWidth / 2.0f * (float)Math.Cos(3.0f * MathHelper.PiOver4), 0.0f, -quadWidth / 2.0f * (float)Math.Sin(3.0f * MathHelper.PiOver4));
                    vertices[12 * starIndex + 11].TextureCoordinate = new Vector2(0.0f, 1.0f);
                    vertices[12 * starIndex + 11].Normal = Vector3.Up;

                    indices[18 * starIndex + 12] = 12 * starIndex + 0 + 8;
                    indices[18 * starIndex + 13] = 12 * starIndex + 1 + 8;
                    indices[18 * starIndex + 14] = 12 * starIndex + 2 + 8;

                    indices[18 * starIndex + 15] = 12 * starIndex + 0 + 8;
                    indices[18 * starIndex + 16] = 12 * starIndex + 2 + 8;
                    indices[18 * starIndex + 17] = 12 * starIndex + 3 + 8;

                    for (int q = 0; q < 12; ++q)
                    {
                        Vector3 pos = vertices[12 * starIndex + q].Position;

                        pos += new Vector3(position.X, 0.0f, position.Z);

                        vertices[12 * starIndex + q].Position = pos;
                    }
                }
            }
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Updates the parameters for the grass animation.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        /// <summary>
        /// This renders the grass chunk.
        /// </summary>
        public void Draw(Vector3 center, Camera camera)
        {
            IGraphicsDeviceService igs = (IGraphicsDeviceService)game.Services.GetService(typeof(IGraphicsDeviceService));
            graphicsDevice = igs.GraphicsDevice;

            // Prepare the effect

            Matrix worldTransform = Matrix.CreateTranslation(center);

            quadTree.GrassShader.Parameters["world"].SetValue(worldTransform);
            quadTree.GrassShader.Parameters["view"].SetValue(camera.ViewMatrix);
            quadTree.GrassShader.Parameters["proj"].SetValue(camera.ProjMatrix);

            quadTree.GrassShader.Parameters["maxHeight"].SetValue(quadTree.Parameters.MaxHeight);
            quadTree.GrassShader.Parameters["textureSize"].SetValue(quadTree.HeightMap.Width);

            quadTree.GrassShader.Parameters["heightMap"].SetValue(quadTree.HeightMap);
            quadTree.GrassShader.Parameters["normalMap"].SetValue(quadTree.NormalMap);
            quadTree.GrassShader.Parameters["grassMap"].SetValue(quadTree.GrassTexture);

            quadTree.GrassShader.Parameters["terrainScale"].SetValue(quadTree.Parameters.TerrainScale);

            quadTree.GrassShader.Parameters["cameraPosition"].SetValue(camera.Position);

            quadTree.GrassShader.Parameters["timer"].SetValue(timer);

            quadTree.GrassShader.Parameters["startFadingInDistance"].SetValue(quadTree.Parameters.GrassStartFadingInDistance);
            quadTree.GrassShader.Parameters["stopFadingInDistance"].SetValue(quadTree.Parameters.GrassStopFadingInDistance);

            quadTree.GrassShader.Parameters["windStrength"].SetValue(quadTree.Parameters.WindStrength);
            quadTree.GrassShader.Parameters["windDirection"].SetValue(quadTree.Parameters.WindDirection);

            // Render it!

            int numVertices = dimension * dimension * 12;
            int numTriangles = dimension * dimension * 6;

            quadTree.GrassShader.CurrentTechnique = quadTree.GrassShader.Techniques["GrassDraw"];
            quadTree.GrassShader.Begin();

            graphicsDevice.VertexDeclaration = vertexDecl;
            graphicsDevice.Vertices[0].SetSource(vertexBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
            graphicsDevice.Indices = indexBuffer;

            foreach (EffectPass pass in quadTree.GrassShader.CurrentTechnique.Passes)
            {
                pass.Begin();

                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVertices, 0, numTriangles);

                pass.End();
            }

            quadTree.GrassShader.End();
        }

        #endregion
    }
}