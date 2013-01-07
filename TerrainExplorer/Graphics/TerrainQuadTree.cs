#region Using Statements
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
#endregion

namespace TerrainExplorer
{
    /// <summary>
    /// This class stores various parameters concerning the construction of a terrain quad tree.
    /// </summary>
    [XmlRoot(Namespace=null, IsNullable=true, ElementName="TerrainQuadTreeParameters")]
    public class TerrainQuadTreeParameters
    {
        #region Properties

        /// <summary>
        /// This is the name of the height map texture in the content hierarchy.
        /// </summary>
        [XmlAttribute()]
        public string HeightMapName;

        /// <summary>
        /// This is the name of the first multitexture layer map in the content hierarchy.
        /// </summary>
        [XmlAttribute()]
        public string LayerMap0Name;

        /// <summary>
        /// This is the name of the second multitexture layer map in the content hierarchy.
        /// </summary>
        [XmlAttribute()]
        public string LayerMap1Name;

        /// <summary>
        /// This is the name of the third multitexture layer map in the content hierarchy.
        /// </summary>
        [XmlAttribute()]
        public string LayerMap2Name;

        /// <summary>
        /// This is the name of the grass texture in the content hierarchy.
        /// </summary>
        [XmlAttribute()]
        public string GrassTextureName;

        /// <summary>
        /// This is the maximum height of the terrain vertices above the ground plane.
        /// </summary>
        [XmlAttribute()]
        public float MaxHeight;

        /// <summary>
        /// This is the distance at which a grass blade begins to fade into sight.
        /// </summary>
        [XmlAttribute()]
        public float GrassStartFadingInDistance;

        /// <summary>
        /// This is the distance at which a grass blade will have become fully visible.
        /// </summary>
        [XmlAttribute()]
        public float GrassStopFadingInDistance;

        /// <summary>
        /// The width/height of the grass chunk that is tiled over the terrain.
        /// </summary>
        [XmlAttribute()]
        public int GrassChunkDimension;

        /// <summary>
        /// The amount of separation between stars of quads in the grass chunk that is tiled over the terrain.
        /// </summary>
        [XmlAttribute()]
        public float GrassChunkStarSeparation;

        /// <summary>
        /// Represents the strength of the wind used to animate the grass.
        /// </summary>
        [XmlAttribute()]
        public float WindStrength;

        /// <summary>
        /// The direction in which the wind blows, used to animate the grass.
        /// </summary>
        [XmlAttribute()]
        public Vector4 WindDirection;

        /// <summary>
        /// This value is used to scale the horizontal dimensions of the final terrain meshes.
        /// </summary>
        [XmlAttribute()]
        public float TerrainScale;

        /// <summary>
        /// This is the screen space error threshold used when determining whether a node in the quadtree hierarchy should
        /// be expanded into its children or rendered as is.
        /// </summary>
        [XmlAttribute()]
        public float MaxScreenSpaceError;

        /// <summary>
        /// This is the height of the viewport in pixels used for LOD calculations.
        /// </summary>
        [XmlAttribute()]
        public float ScreenHeight;

        /// <summary>
        /// This is the vertical field of view of the screen used for LOD calculations.
        /// </summary>
        [XmlAttribute()]
        public float FieldOfView;

        /// <summary>
        /// If this is true, the chunk data will be constructed from scratch and then saved to the file "Chunks.dat".
        /// If this is false, then the program will search for a file named "Chunks.dat" and try to load the
        /// chunk data from this file.
        /// </summary>
        [XmlAttribute()]
        public bool DoPreprocessing;

        private float K = 1.0f;

        /// <summary>
        /// This is a pre-computed scaling factor used in LOD calculations.
        /// </summary>
        [XmlAttribute()]
        public float PerspectiveScalingFactor
        {
            get { return K; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This must be called after the LOD parameters are modified to recompute the perspective scaling factor.
        /// </summary>
        public void ComputePerspectiveScalingFactor()
        {
            K = ScreenHeight / (2.0f * (float)Math.Tan(FieldOfView / 2.0f));
        }

        /// <summary>
        /// Writes the parameters to an XML file.
        /// </summary>
        public void Save(string filename)
        {
            Stream stream = null;

            try
            {
                stream = File.Create(filename);
                XmlSerializer serializer = new XmlSerializer(typeof(TerrainQuadTreeParameters));
                serializer.Serialize(stream, this);
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }

        /// <summary>
        /// Loads a new set of parameters from an XML file.
        /// </summary>
        public static TerrainQuadTreeParameters Load(string filename)
        {
            FileStream stream = null;
            TerrainQuadTreeParameters paramaters = null;

            try
            {
                stream = File.OpenRead(filename);
                XmlSerializer serializer = new XmlSerializer(typeof(TerrainQuadTreeParameters));
                paramaters = (TerrainQuadTreeParameters)serializer.Deserialize(stream);
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }

            return paramaters;
        }

        #endregion
    }

    /// <summary>
    /// This class encapsulates the data representation and rendering of a terrain mesh.
    /// </summary>
    public class TerrainQuadTree
    {
        #region Fields

        Game game;

        Grid grid;

        Effect terrainEffect;
        Effect grassEffect;

        TerrainQuadTreeParameters parameters;
        TerrainChunk rootNode;

        GrassChunk grassChunk;
        List<Vector3> grassChunkPositions;

        Texture2D heightMap;
        Texture2D layerMap0;
        Texture2D layerMap1;
        Texture2D layerMap2;
        Texture2D grassTexture;

        SpriteBatch normalSpriteBatch;
        DepthStencilBuffer normalDepthBuffer;
        RenderTarget2D normalRenderTarget;

        Dictionary<int, float> chunkGeometricErrors;

        int currentChunkIndex;

        int heightMapResolution;

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
        /// Exposes the terrain effect object.
        /// </summary>
        public Effect TerrainShader
        {
            get { return terrainEffect; }
        }

        /// <summary>
        /// Exposes the grass effect object.
        /// </summary>
        public Effect GrassShader
        {
            get { return grassEffect; }
        }

        /// <summary>
        /// Gets the set of parameters used to construct this quadtree.
        /// </summary>
        public TerrainQuadTreeParameters Parameters
        {
            get { return parameters; }
        }

        /// <summary>
        /// Gets the texture used for the terrain heightmap.
        /// </summary>
        public Texture2D HeightMap
        {
            get { return heightMap; }
        }

        /// <summary>
        /// Gets the textured used for the dynamic grass.
        /// </summary>
        public Texture2D GrassTexture
        {
            get { return grassTexture; }
        }

        /// <summary>
        /// Gets the normal map generated upon initialization of the quadtree.
        /// </summary>
        public Texture2D NormalMap
        {
            get { return normalRenderTarget.GetTexture(); }
        }

        /// <summary>
        /// Gets the first multitexture used for the terrain texturing.
        /// </summary>
        public Texture2D LayerMap0
        {
            get { return layerMap0; }
        }

        /// <summary>
        /// Gets the second multitexture used for the terrain texturing.
        /// </summary>
        public Texture2D LayerMap1
        {
            get { return layerMap1; }
        }

        /// <summary>
        /// Gets the third multitexture used for the terrain texturing.
        /// </summary>
        public Texture2D LayerMap2
        {
            get { return layerMap2; }
        }

        /// <summary>
        /// Gets the common grid geometry used for all the chunks.
        /// </summary>
        public Grid CommonGrid
        {
            get { return grid; }
        }

        /// <summary>
        /// Gets the dictionary of geometric errors loaded from the chunk data file.
        /// </summary>
        public Dictionary<int, float> ChunkGeometricErrors
        {
            get { return chunkGeometricErrors; }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// This initializes a new instance of TerrainQuadTree. It automatically loads all graphics resources,
        /// so it must be called at an appropriate time.
        /// </summary>
        public TerrainQuadTree(Game game, ContentManager content, TerrainQuadTreeParameters parameters)
        {
            this.game = game;
            this.parameters = parameters;

            heightMap = content.Load<Texture2D>(parameters.HeightMapName);
            layerMap0 = content.Load<Texture2D>(parameters.LayerMap0Name);
            layerMap1 = content.Load<Texture2D>(parameters.LayerMap1Name);
            layerMap2 = content.Load<Texture2D>(parameters.LayerMap2Name);
            grassTexture = content.Load<Texture2D>(parameters.GrassTextureName);

            terrainEffect = content.Load<Effect>("Content\\Shaders\\Terrain");
            grassEffect = content.Load<Effect>("Content\\Shaders\\Grass");

            this.heightMapResolution = heightMap.Width;

            grid = new Grid(game, 1.0f / 16.0f, 16);

            ComputeNormalMap();

            // Due to a bug with XNA 2.0, it keeps the height map data locked after creating the normal map.
            // The only solution I found was to set the graphics device's reference to the texture to null.

            game.GraphicsDevice.Textures[0] = null;

            // About 99% of the preprocessing time is spent simply doing the detailed computations necessary
            // to determine the geometric errors of the chunks. Therefore, instead of serializing the entire
            // quadtree to speed up initialization, just these values are saved.

            if (!parameters.DoPreprocessing)
            {
                // Load the geometric errors from the chunk data file.

                Stream stream = File.Open("Chunks.dat", FileMode.Open);
                BinaryFormatter formatter = new BinaryFormatter();
                

                chunkGeometricErrors = (Dictionary<int, float>)formatter.Deserialize(stream);
                stream.Close();
            }
            else
            {
                chunkGeometricErrors = new Dictionary<int, float>();
            }

            currentChunkIndex = 0;

            ConstructQuadTree();

            if (parameters.DoPreprocessing)
            {
                // Save the geometric errors to the chunk data file.

                Stream stream = File.Open("Chunks.dat", FileMode.Create);
                BinaryFormatter formatter = new BinaryFormatter();

                formatter.Serialize(stream, chunkGeometricErrors);
                stream.Close();
            }

            grassChunk = new GrassChunk(game, this, parameters.GrassChunkDimension, parameters.GrassChunkStarSeparation);
            GenerateGrassChunkPositions();
        }

        /// <summary>
        /// This constructs a list of positions along the terrain at which the grass chunk will be rendered.
        /// </summary>
        private void GenerateGrassChunkPositions()
        {
            grassChunkPositions = new List<Vector3>();

            float grassChunkSize = grassChunk.Dimension * grassChunk.StarSeparation;

            for (int i = 0; i < parameters.TerrainScale / grassChunkSize; ++i)
            {
                for (int j = 0; j < parameters.TerrainScale / grassChunkSize; ++j)
                {
                    grassChunkPositions.Add(new Vector3(-parameters.TerrainScale / 2.0f + grassChunkSize * i,
                                                        0.0f,
                                                        -parameters.TerrainScale / 2.0f + grassChunkSize * j));
                }
            }
        }

        /// <summary>
        /// This method constructs the terrain quad tree structure, creating all the terrain chunks and geometry.
        /// </summary>
        private void ConstructQuadTree()
        {
            // Set up the root node centered at the origin and scaled to fit the entire terrain.

            rootNode = new TerrainChunk(this, null, new Vector3(), parameters.TerrainScale);

            // This will take care of the rest!

            rootNode.RecursivelyBuildTree();
        }

        /// <summary>
        /// This computes a normal map using an 8-tap Sodel filter for the loaded heightmap.
        /// </summary>
        private void ComputeNormalMap()
        {
            GraphicsDevice graphicsDevice = game.GraphicsDevice;

            RenderTarget2D oldRenderTarget = graphicsDevice.GetRenderTarget(0) as RenderTarget2D;
            DepthStencilBuffer oldDepthBuffer = graphicsDevice.DepthStencilBuffer;

            normalSpriteBatch = new SpriteBatch(game.GraphicsDevice);

            normalRenderTarget = new RenderTarget2D(graphicsDevice, heightMap.Width, 
                heightMap.Height, 1, SurfaceFormat.Color);

            normalDepthBuffer = new DepthStencilBuffer(graphicsDevice,
                heightMap.Width, heightMap.Height, graphicsDevice.DepthStencilBuffer.Format);

            graphicsDevice.SetRenderTarget(0, normalRenderTarget);
            graphicsDevice.DepthStencilBuffer = normalDepthBuffer;

            graphicsDevice.Clear(Color.White);

            normalSpriteBatch.Begin(SpriteBlendMode.None, SpriteSortMode.Immediate, SaveStateMode.None);

            terrainEffect.Parameters["heightMap"].SetValue(heightMap);
            terrainEffect.Parameters["textureSize"].SetValue(heightMap.Width);

            terrainEffect.CurrentTechnique = terrainEffect.Techniques["ComputeNormals"];

            terrainEffect.Begin();
            terrainEffect.CurrentTechnique.Passes[0].Begin();

            normalSpriteBatch.Draw(heightMap, new Rectangle(0, 0, heightMap.Width, heightMap.Height),
                                   Color.Black);

            terrainEffect.CurrentTechnique.Passes[0].End();
            terrainEffect.End();

            normalSpriteBatch.End();

            graphicsDevice.SetRenderTarget(0, oldRenderTarget);
            graphicsDevice.DepthStencilBuffer = oldDepthBuffer;
        }

        /// <summary>
        /// Generates a unique integer ID for a chunk in the quadtree.
        /// </summary>
        /// <returns></returns>
        public int GetNextChunkIndex()
        {
            return currentChunkIndex++;
        }

        #endregion

        #region Draw

        /// <summary>
        /// Updates the quadtree.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            grassChunk.Update(gameTime);
        }

        /// <summary>
        /// Renders the grass chunks distributed across the terrain.
        /// </summary>
        /// <param name="camera"></param>
        private void RenderGrass(Camera camera)
        {
            BoundingFrustum frustum = new BoundingFrustum(camera.ViewMatrix * camera.ProjMatrix);

            float grassChunkSize = grassChunk.Dimension * grassChunk.StarSeparation;

            foreach (Vector3 position in grassChunkPositions)
            {
                // Do frustum culling.

                Vector2 cameraPosVec2 = new Vector2(camera.Position.X, camera.Position.Z);
                Vector2 grassPosVec2 = new Vector2(position.X, position.Z);

                if ((cameraPosVec2 - grassPosVec2).Length() > parameters.GrassStartFadingInDistance + grassChunkSize / 2.0f)
                    continue;

                BoundingBox boundingBox = new BoundingBox(new Vector3(position.X - grassChunkSize / 2.0f, 0.0f, position.Z - grassChunkSize / 2.0f),
                                              new Vector3(position.X + grassChunkSize / 2.0f, parameters.MaxHeight + grassChunk.QuadHeight, position.Z + grassChunkSize / 2.0f));

                ContainmentType containment;
                frustum.Contains(ref boundingBox, out containment);

                if (containment == ContainmentType.Disjoint)
                    continue;

                grassChunk.Draw(position, camera);
            }
        }

        /// <summary>
        /// This draws the terrain using the GPU for height map displacement and texture blending.
        /// </summary>
        public void Draw(Camera camera)
        {
            GraphicsDevice graphicsDevice = game.GraphicsDevice;

            graphicsDevice.RenderState.CullMode = CullMode.None;
            //graphicsDevice.RenderState.FillMode = FillMode.WireFrame;

            rootNode.Update(camera);
            rootNode.Render(camera);

            RenderGrass(camera);

            graphicsDevice.RenderState.AlphaBlendEnable = false;
            graphicsDevice.RenderState.AlphaTestEnable = false;
            graphicsDevice.RenderState.DepthBufferWriteEnable = true;
        }

        #endregion
    }
}