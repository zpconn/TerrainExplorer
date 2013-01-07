#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
#endregion

namespace TerrainExplorer
{
    /// <summary>
    /// This class embodies a camera object, which can be positied and oriented arbitrarily. Cameras
    /// that hook to certain input devices derive from this class.
    /// </summary>
    public abstract class Camera
    {
        #region Fields

        protected float aspectRatio = 4.0f / 3.0f;
        protected float fieldOfView = (float)Math.PI / 4.0f;
        protected float nearPlaneDistance = 1.0f;
        protected float farPlaneDistance = 99999999.0f;

        protected Vector3 position;
        protected Vector3 direction;
        protected Vector3 up;
        protected Vector3 right;
        protected Vector3 angles;

        protected Matrix viewMatrix;
        protected Matrix projMatrix;

        #endregion

        #region Properties

        /// <summary>
        /// Determines how projections from 3D space to 2D space are scaled.
        /// </summary>
        public float AspectRatio
        {
            get { return aspectRatio; }
            set { aspectRatio = value; }
        }

        /// <summary>
        /// Perspective field of view.
        /// </summary>
        public float FieldOfView
        {
            get { return fieldOfView; }
            set { fieldOfView = value; }
        }

        /// <summary>
        /// The distance of the near clipping plane.
        /// </summary>
        public float NearPlaneDistance
        {
            get { return nearPlaneDistance; }
            set { nearPlaneDistance = value; }
        }

        /// <summary>
        /// The distance of the far clipping plane.
        /// </summary>
        public float FarPlaneDistance
        {
            get { return farPlaneDistance; }
            set { farPlaneDistance = value; }
        }

        /// <summary>
        /// Gets the view matrix.
        /// </summary>
        public Matrix ViewMatrix
        {
            get { return viewMatrix; }
        }

        /// <summary>
        /// Gets the projection matrix.
        /// </summary>
        public Matrix ProjMatrix
        {
            get { return projMatrix; }
        }

        /// <summary>
        /// Gets or sets the position of the camera.
        /// </summary>
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        /// <summary>
        /// Each component of the Angles vector corresponds to the camera's rotation about the respective axis.
        /// </summary>
        public Vector3 Angles
        {
            get { return angles; }
            set { angles = value; }
        }

        /// <summary>
        /// Gets a vector pointing in the actual direction the camera is facing.
        /// </summary>
        public Vector3 Direction
        {
            get { return direction; }
        }

        /// <summary>
        /// Gets a vector pointing up relative to the camera's frame of reference.
        /// </summary>
        public Vector3 Up
        {
            get { return up; }
        }

        /// <summary>
        /// Gets a vector pointing to the right in the camera's frame of reference.
        /// </summary>
        public Vector3 Right
        {
            get { return right; }
        }

        #endregion

        #region Update

        /// <summary>
        /// This computes the view and projection matrices given the camera transformation.
        /// </summary>
        public void UpdateMatrices()
        {
            // Create the projection matrix.

            projMatrix = Matrix.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, nearPlaneDistance, farPlaneDistance);

            // First compute the look-at and up vectors.

            Quaternion rotation = Quaternion.CreateFromYawPitchRoll(angles.Y, angles.X, angles.Z);

            Vector3 lookAt = new Vector3(0.0f, 0.0f, -1.0f);
            direction = Vector3.Transform(lookAt, rotation);
            lookAt = position + direction;

            up = Vector3.Transform(new Vector3(0.0f, 1.0f, 0.0f), rotation);

            // Calculate the right vector for external use
            right = Vector3.Cross(direction, up);

            // Now compute the view matrix.

            viewMatrix = Matrix.CreateLookAt(position, lookAt, up);
        }

        /// <summary>
        /// This abstract method must be implemented by base classes to update the camera state and react to input.
        /// </summary>
        public abstract void Update(GameTime gameTime);

        #endregion
    }
}