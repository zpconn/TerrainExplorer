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
    /// This class stores the rectangular bounds of the subregion of the heightmap corresponding to a 
    /// particular terrain chunk.
    /// </summary>
    public class TerrainChunkTextureCoordinates
    {
        /// <summary>
        /// This is the value of the u-coordinate on the leftmost boundary of the subregion.
        /// </summary>
        public float UStart;

        /// <summary>
        /// This is the value of the u-coordinate on the rightmost boundary of the subregion.
        /// </summary>
        public float UEnd;

        /// <summary>
        /// This is the value of the v-coordinate on the bottommost boundary of the subregion.
        /// </summary>
        public float VStart;

        /// <summary>
        /// This is the value of the v-coordinate on the topmost boundary of the subregion.
        /// </summary>
        public float VEnd;
    }

    /// <summary>
    /// This represents one chunk of terrain in a terrain quadtree.
    /// </summary>
    public class TerrainChunk
    {
        #region Fields

        private TerrainQuadTree quadTree;

        private TerrainChunk parent;
        private List<TerrainChunk> children;

        private Vector3 centerWorldPosition;
        private float scaleFactor;

        private Matrix worldTransform;

        private TerrainChunkTextureCoordinates textureCoordinates;

        private BoundingBox boundingBox;

        private float geometricError = 0.0f;

        private bool splitIntoChildren = false;

        private float minimumDistanceToSplit;

        private int index;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the reference to the overall terrain quadtree of which this chunk is a member.
        /// </summary>
        public TerrainQuadTree QuadTree
        {
            get { return quadTree; }
            set { quadTree = value; }
        }

        /// <summary>
        /// Gets or sets the parent of this chunk in the quadtree.
        /// </summary>
        public TerrainChunk Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        /// <summary>
        /// Gets the list of child chunks in the quadtree.
        /// </summary>
        public List<TerrainChunk> Children
        {
            get { return children; }
        }

        /// <summary>
        /// Gets the center of this chunk in world coordinates.
        /// </summary>
        public Vector3 CenterWorldPosition
        {
            get { return centerWorldPosition; }
        }

        /// <summary>
        /// Gets the factor by which the base grid must be scaled to represent this chunk.
        /// </summary>
        public float ScalingFactor
        {
            get { return ScalingFactor; }
        }

        /// <summary>
        /// Gets the world transformation matrix for this chunk.
        /// </summary>
        public Matrix WorldTransform
        {
            get { return worldTransform; }
        }

        /// <summary>
        /// Gets the texture coordinate range in the heightmap of this chunk.
        /// </summary>
        public TerrainChunkTextureCoordinates TextureCoordinates
        {
            get { return textureCoordinates; }
        }

        /// <summary>
        /// Returns the bounding box of this chunk used for frustum culling.
        /// </summary>
        public BoundingBox BoundingBox
        {
            get { return boundingBox; }
        }

        /// <summary>
        /// Gets a measure of the geometric deviation in model-space of this chunk's geometry from the 
        /// "ideal" geometry represented by the heightmap.
        /// </summary>
        public float GeometricError
        {
            get { return geometricError; }
        }

        /// <summary>
        /// If the distance from the camera to this chunk is less than this value, then the chunk will split
        /// into its children to increase the detail level.
        /// </summary>
        public float MinimumDistanceToSplit
        {
            get { return minimumDistanceToSplit; }
        }

        /// <summary>
        /// Gets the unique integer ID for this chunk.
        /// </summary>
        public int Index
        {
            get { return index; }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// This initializes a new terrain chunk given the provided world transform.
        /// </summary>
        public TerrainChunk(TerrainQuadTree quadTree, TerrainChunk parent, Vector3 centerWorldPosition, float scalingFactor)
        {
            this.quadTree = quadTree;
            this.parent = parent;
            this.centerWorldPosition = centerWorldPosition;
            this.scaleFactor = scalingFactor;
        }

        /// <summary>
        /// This computes the raw geometric error of this chunk without consideration of the children chunks' errors.
        /// </summary>
        private float ComputeRawGeometricError()
        {
            // For every height value in the heightmap that does not correspond to a vertex of the grid for this chunk
            // we compute the distance from this chunk's actual geometry to the "ideal" chunk geometry.

            // It's difficult to access the height data outside the shader, since the whole system is designed
            // for the grid to be dynamically displaced by the height map data in the vertex shader, but
            // since this is only done once upon initialization we can get away with it.

            // Unfortunately, the values obtained here are not exactly accurate, because they are sampled "as is" whereas
            // in the shader height values are sampled with bilinear filtering. The error measure is still correct enough
            // that we do not need to worry about correcting this.

            Color[] heights = new Color[quadTree.HeightMap.Width * quadTree.HeightMap.Height];
            quadTree.HeightMap.GetData<Color>(heights);

            float du = 1.0f / quadTree.HeightMap.Width;
            float dv = 1.0f / quadTree.HeightMap.Height;

            float maxError = 0.0f;

            for (float u = textureCoordinates.UStart; u <= textureCoordinates.UEnd; u += du)
            {
                for (float v = textureCoordinates.VStart; v <= textureCoordinates.VEnd; v += dv)
                {
                    // Does (u,v) correspond to a vertex in the grid?

                    if (!(u % (1.0f / quadTree.CommonGrid.Dimension) == 0.0f && v % (1.0f / quadTree.CommonGrid.Dimension) == 0.0f))
                    {
                        // No. So we must find the distance from the vertex represented by (u,v) to the actual
                        // mesh geometry.

                        int imageX = (int)(u * (quadTree.HeightMap.Width - 1));
                        int imageY = (int)(v * (quadTree.HeightMap.Height - 1));

                        float height = heights[imageY * quadTree.HeightMap.Width + imageX].R;

                        if (v % (1.0f / quadTree.CommonGrid.Dimension) == 0.0f)
                        {
                            Vector2 right = new Vector2(u + du, v);
                            Vector2 left = new Vector2(u - du, v);

                            int imageXRight = (int)(right.X * (quadTree.HeightMap.Width - 1));
                            int imageYRight = (int)(right.Y * (quadTree.HeightMap.Height - 1));

                            int imageXLeft = (int)(left.X * (quadTree.HeightMap.Width - 1));
                            int imageYLeft = (int)(left.Y * (quadTree.HeightMap.Height - 1));

                            float leftHeight = heights[imageYLeft * quadTree.HeightMap.Width + imageXLeft].R;
                            float rightHeight = heights[imageYRight * quadTree.HeightMap.Height + imageXRight].R;

                            float interpolatedHeight = (leftHeight + rightHeight) / 2.0f;

                            float error = Math.Abs(height - interpolatedHeight);

                            if (error > maxError)
                                maxError = error;
                        }
                        else if (u % (1.0f / quadTree.CommonGrid.Dimension) == 0.0f)
                        {
                            Vector2 top = new Vector2(u, v + dv);
                            Vector2 bottom = new Vector2(u, v - dv);

                            int imageXTop = (int)(top.X * (quadTree.HeightMap.Width - 1));
                            int imageYTop = (int)(top.Y * (quadTree.HeightMap.Height - 1));

                            int imageXBottom = (int)(bottom.X * (quadTree.HeightMap.Width - 1));
                            int imageYBottom = (int)(bottom.Y * (quadTree.HeightMap.Height - 1));

                            float topHeight = heights[imageYTop * quadTree.HeightMap.Width + imageXTop].R;
                            float bottomHeight = heights[imageYBottom * quadTree.HeightMap.Width + imageXBottom].R;

                            float interpolatedHeight = (topHeight + bottomHeight) / 2.0f;

                            float error = Math.Abs(height - interpolatedHeight);

                            if (error > maxError)
                                maxError = error;
                        }
                        else
                        {
                            Vector2 topRight = new Vector2(u + du, v + dv);
                            Vector2 topLeft = new Vector2(u - du, v + dv);

                            int imageXTopRight = (int)(topRight.X * (quadTree.HeightMap.Width - 1));
                            int imageYTopRight = (int)(topRight.Y * (quadTree.HeightMap.Height - 1));

                            int imageXTopLeft = (int)(topLeft.X * (quadTree.HeightMap.Width - 1));
                            int imageYTopLeft = (int)(topLeft.Y * (quadTree.HeightMap.Height - 1));

                            float topLeftHeight = heights[imageYTopLeft * quadTree.HeightMap.Width + imageXTopLeft].R;
                            float topRightHeight = heights[imageYTopRight * quadTree.HeightMap.Width + imageXTopRight].R;

                            Vector2 bottomRight = new Vector2(u + du, v - dv);
                            Vector2 bottomLeft = new Vector2(u - du, v - dv);

                            int imageXBottomRight = (int)(bottomRight.X * (quadTree.HeightMap.Width - 1));
                            int imageYBottomRight = (int)(bottomRight.Y * (quadTree.HeightMap.Height - 1));

                            int imageXBottomLeft = (int)(bottomLeft.X * (quadTree.HeightMap.Width - 1));
                            int imageYBottomLeft = (int)(bottomLeft.Y * (quadTree.HeightMap.Height - 1));

                            float bottomLeftHeight = heights[imageYBottomLeft * quadTree.HeightMap.Width + imageXBottomLeft].R;
                            float bottomRightHeight = heights[imageYBottomRight * quadTree.HeightMap.Width + imageXBottomRight].R;

                            float interpolatedHeight = (topLeftHeight + topRightHeight + bottomLeftHeight + bottomRightHeight) / 4.0f;

                            float error = Math.Abs(height - interpolatedHeight);

                            if (error > maxError)
                                maxError = error;
                        }
                    }
                }
            }

            return maxError;
        }

        /// <summary>
        /// Outside code should only call this on the root. This method will recursively construct an entire quadtree starting
        /// from the root node. It will continue to expand a branch of the tree until any new nodes would have to
        /// have a resolution greater than that of the heightmap itself.
        /// </summary>
        public void RecursivelyBuildTree()
        {
            // Get this chunk's index.

            index = quadTree.GetNextChunkIndex();

            // First compute the net world transform for this node.

            worldTransform = Matrix.CreateScale(scaleFactor, 1.0f, scaleFactor) * Matrix.CreateTranslation(centerWorldPosition);

            // Calculate the texture coordinate range for this chunk.

            textureCoordinates = new TerrainChunkTextureCoordinates();

            float terrainDimension = 2 * quadTree.Parameters.TerrainScale;

            textureCoordinates.UStart = ((centerWorldPosition.X - scaleFactor / 2.0f + terrainDimension / 2.0f) / terrainDimension - 0.25f) * 2f;
            textureCoordinates.UEnd = ((centerWorldPosition.X + scaleFactor / 2.0f + terrainDimension / 2.0f) / terrainDimension - 0.25f) * 2f;
            textureCoordinates.VStart = ((centerWorldPosition.Z - scaleFactor / 2.0f + terrainDimension / 2.0f) / terrainDimension - 0.25f) * 2f;
            textureCoordinates.VEnd = ((centerWorldPosition.Z + scaleFactor / 2.0f + terrainDimension / 2.0f) / terrainDimension - 0.25f) * 2f;

            // Construct the bounding box to be used for frustum culling

            boundingBox = new BoundingBox(new Vector3(centerWorldPosition.X - scaleFactor / 2.0f, 0.0f, centerWorldPosition.Z - scaleFactor / 2.0f),
                                         new Vector3(centerWorldPosition.X + scaleFactor / 2.0f, quadTree.Parameters.MaxHeight, centerWorldPosition.Z + scaleFactor / 2.0f));

            // If this node has a lower resolution than the heightmap itself, we expand it into four children.
            // Otherwise, this branch of the tree ends.

            children = new List<TerrainChunk>();

            if (quadTree.HeightMap.Width / quadTree.Parameters.TerrainScale >= quadTree.CommonGrid.Dimension / scaleFactor)
            {
                // This isn't a leaf chunk, so we compute/look up its geometric error.

                if (quadTree.Parameters.DoPreprocessing)
                {
                    // Compute it manually

                    geometricError = ComputeRawGeometricError();
                    quadTree.ChunkGeometricErrors[index] = geometricError;
                }
                else
                {
                    // Look it up from the precomputed table

                    geometricError = quadTree.ChunkGeometricErrors[index];
                }

                // Expand into four children

                TerrainChunk child1 = new TerrainChunk(quadTree, this,
                                                       centerWorldPosition + new Vector3(scaleFactor / 4.0f, 0.0f, scaleFactor / 4.0f),
                                                       scaleFactor / 2.0f);

                TerrainChunk child2 = new TerrainChunk(quadTree, this,
                                                       centerWorldPosition + new Vector3(scaleFactor / 4.0f, 0.0f, -scaleFactor / 4.0f),
                                                       scaleFactor / 2.0f);

                TerrainChunk child3 = new TerrainChunk(quadTree, this,
                                                       centerWorldPosition + new Vector3(-scaleFactor / 4.0f, 0.0f, scaleFactor / 4.0f),
                                                       scaleFactor / 2.0f);

                TerrainChunk child4 = new TerrainChunk(quadTree, this,
                                                       centerWorldPosition + new Vector3(-scaleFactor / 4.0f, 0.0f, -scaleFactor / 4.0f),
                                                       scaleFactor / 2.0f);

                children.Add(child1);
                children.Add(child2);
                children.Add(child3);
                children.Add(child4);

                child1.RecursivelyBuildTree();
                child2.RecursivelyBuildTree();
                child3.RecursivelyBuildTree();
                child4.RecursivelyBuildTree();
            }

            // Finally, as the recursion unwinds, we accumulate the geometric error from children.

            if (children.Count > 0)
            {
                geometricError += (float)Math.Max(children[0].GeometricError, Math.Max(children[1].GeometricError,
                                                  Math.Max(children[2].GeometricError, children[3].GeometricError)));
            }

            minimumDistanceToSplit = geometricError / quadTree.Parameters.MaxScreenSpaceError * quadTree.Parameters.PerspectiveScalingFactor;
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// This updates the chunk and its children if needed.
        /// </summary>
        public void Update(Camera camera)
        {
            float distance = (camera.Position - centerWorldPosition).Length();

            float screenSpaceError = geometricError / distance * quadTree.Parameters.PerspectiveScalingFactor;

            if (screenSpaceError > quadTree.Parameters.MaxScreenSpaceError && children.Count > 0)
            {
                splitIntoChildren = true;

                foreach (TerrainChunk child in children)
                {
                    child.Update(camera);
                }
            }
            else
            {
                splitIntoChildren = false;
            }
        }

        public void Render(Camera camera)
        {
            // If this node should be split, then render the four children instead

            if (splitIntoChildren)
            {
                foreach (TerrainChunk child in children)
                {
                    child.Render(camera);
                }
            }
            else
            {
                IGraphicsDeviceService igs = (IGraphicsDeviceService)quadTree.Game.Services.GetService(typeof(IGraphicsDeviceService));
                GraphicsDevice graphicsDevice = igs.GraphicsDevice;

                // Do frustum culling

                BoundingFrustum frustum = new BoundingFrustum(camera.ViewMatrix * camera.ProjMatrix);

                ContainmentType containment;
                frustum.Contains(ref boundingBox, out containment);

                if (containment == ContainmentType.Disjoint)
                    return;

                // Prepare the effect

                quadTree.TerrainShader.Parameters["world"].SetValue(worldTransform);
                quadTree.TerrainShader.Parameters["view"].SetValue(camera.ViewMatrix);
                quadTree.TerrainShader.Parameters["proj"].SetValue(camera.ProjMatrix);

                quadTree.TerrainShader.Parameters["maxHeight"].SetValue(quadTree.Parameters.MaxHeight);
                quadTree.TerrainShader.Parameters["textureSize"].SetValue(quadTree.HeightMap.Width);

                quadTree.TerrainShader.Parameters["heightMap"].SetValue(quadTree.HeightMap);
                quadTree.TerrainShader.Parameters["grassMap"].SetValue(quadTree.LayerMap0);
                quadTree.TerrainShader.Parameters["rockMap"].SetValue(quadTree.LayerMap1);
                quadTree.TerrainShader.Parameters["snowMap"].SetValue(quadTree.LayerMap2);
                quadTree.TerrainShader.Parameters["normalMap"].SetValue(quadTree.NormalMap);

                quadTree.TerrainShader.Parameters["uStart"].SetValue(textureCoordinates.UStart);
                quadTree.TerrainShader.Parameters["uEnd"].SetValue(textureCoordinates.UEnd);
                quadTree.TerrainShader.Parameters["vStart"].SetValue(textureCoordinates.VStart);
                quadTree.TerrainShader.Parameters["vEnd"].SetValue(textureCoordinates.VEnd);
                
                // Render it!

                quadTree.TerrainShader.CurrentTechnique = quadTree.TerrainShader.Techniques["TerrainDraw"];
                quadTree.TerrainShader.Begin();

                foreach (EffectPass pass in quadTree.TerrainShader.CurrentTechnique.Passes)
                {
                    pass.Begin();

                    quadTree.CommonGrid.Draw();

                    pass.End();
                }

                quadTree.TerrainShader.End();
            }
        }

        #endregion
    }
}