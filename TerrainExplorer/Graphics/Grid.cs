#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace TerrainExplorer
{
    /// <summary>
    /// This forms a 2D grid mesh that can be deformed for terrain rendering by reading height values
    /// in a vertex shader with vertex texture fetch.
    /// </summary>
    public class Grid
    {
        #region Fields

        Game game;
        GraphicsDevice graphicsDevice;

        VertexBuffer vertexBuffer;
        IndexBuffer indexBuffer;

        VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[4];
        int[] indices = new int[6];

        VertexDeclaration vertexDecl;

        private float cellSize = 4;
        private int dimension = 128;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the size of a single cell in the grid.
        /// </summary>
        public float CellSize
        {
            get { return cellSize; }
        }

        /// <summary>
        /// Gets the dimension of the grid.
        /// </summary>
        public int Dimension
        {
            get { return dimension; }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Creates a new instance of Grid. This automatically loads the graphics content and constructs the grid,
        /// so the graphics device must be available upon calling this.
        /// </summary>
        public Grid(Game game, float cellSize, int dimension)
        {
            this.game = game;
            this.cellSize = cellSize;
            this.dimension = dimension + 2;

            LoadGraphicsContent();
        }

        /// <summary>
        /// Creates the graphics resources needed to render the grid.
        /// </summary>
        private void LoadGraphicsContent()
        {
            GenerateStructure();

            IGraphicsDeviceService igs = (IGraphicsDeviceService)game.Services.GetService(typeof(IGraphicsDeviceService));
            graphicsDevice = igs.GraphicsDevice;

            int numVertices = (dimension + 1) * (dimension + 1);
            int numIndices = 6 * dimension * dimension;

            vertexBuffer = new VertexBuffer(graphicsDevice, numVertices * VertexPositionNormalTexture.SizeInBytes,
                BufferUsage.None);
            indexBuffer = new IndexBuffer(graphicsDevice, numIndices * sizeof(int), BufferUsage.None, IndexElementSize.ThirtyTwoBits);

            vertexBuffer.SetData<VertexPositionNormalTexture>(vertices);
            indexBuffer.SetData<int>(indices);

            vertexDecl = new VertexDeclaration(graphicsDevice, VertexPositionNormalTexture.VertexElements);
        }

        /// <summary>
        /// Given the coordinates of a grid point, this returns the index of that vertex.
        /// </summary>
        private int VBIndex(int x, int y)
        {
            return y * (dimension + 1) + x;
        }

        /// <summary>
        /// Gets the index into the index buffer corresponding to the vertex at point (x,y).
        /// </summary>
        private int IBINdex(int x, int y)
        {
            return y * dimension + x;
        }

        /// <summary>
        /// Creates the mesh structure of the grid.
        /// </summary>
        private void GenerateStructure()
        {
            int numVertices = (dimension + 1) * (dimension + 1);
            int numIndices = 6 * dimension * dimension;

            vertices = new VertexPositionNormalTexture[numVertices];
            indices = new int[numIndices];

            // Fill in the data for each vertex.

            for (int i = 0; i < dimension + 1; ++i)
            {
                for (int j = 0; j < dimension + 1; ++j)
                {
                    VertexPositionNormalTexture vertex = new VertexPositionNormalTexture();

                    float height = 0.0f;

                    if (i == 0 || i == dimension || j == 0 || j == dimension)
                        height = -600.0f;

                    int offsetI = (int)MathHelper.Clamp(i, 1, dimension - 1);
                    int offsetJ = (int)MathHelper.Clamp(j, 1, dimension - 1);

                    vertex.Position = new Vector3((offsetI - dimension / 2.0f) * cellSize, height, (offsetJ - dimension / 2.0f) * cellSize);
                    vertex.Normal = Vector3.Up;
                    vertex.TextureCoordinate = new Vector2((float)(offsetI - 1) / (dimension - 2), (float)(offsetJ - 1) / (dimension - 2));

                    vertices[VBIndex(i, j)] = vertex;
                }
            }

            // Now record the indices for each triangle in the mesh. Each vertex is the lower-left corner of a square,
            // and the square is made up of two triangles, so there are six indices and two triangles for each vertex.

            for (int i = 0; i < dimension; ++i)
            {
                for (int j = 0; j < dimension; ++j)
                {
                    indices[6 * IBINdex(i, j) + 0] = VBIndex(i, j);
                    indices[6 * IBINdex(i, j) + 1] = VBIndex(i + 1, j);
                    indices[6 * IBINdex(i, j) + 2] = VBIndex(i + 1, j + 1);

                    indices[6 * IBINdex(i, j) + 3] = VBIndex(i, j);
                    indices[6 * IBINdex(i, j) + 4] = VBIndex(i + 1, j + 1);
                    indices[6 * IBINdex(i, j) + 5] = VBIndex(i, j + 1);
                }
            }
        }

        #endregion

        #region Draw

        /// <summary>
        /// This renders the grid as constructed.
        /// </summary>
        public void Draw()
        {
            IGraphicsDeviceService igs = (IGraphicsDeviceService)game.Services.GetService(typeof(IGraphicsDeviceService));
            graphicsDevice = igs.GraphicsDevice;

            int numVertices = (dimension + 1) * (dimension + 1);
            int numTriangles = 2 * dimension * dimension;

            graphicsDevice.VertexDeclaration = vertexDecl;
            graphicsDevice.Vertices[0].SetSource(vertexBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
            graphicsDevice.Indices = indexBuffer;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVertices, 0, numTriangles);
        }

        #endregion
    }
}