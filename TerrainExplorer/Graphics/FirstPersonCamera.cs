#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
#endregion

namespace TerrainExplorer
{
    /// <summary>
    /// This class receives input from the gamepad to move the camera around as it would
    /// in a first-person shooter.
    /// </summary>
    public class FirstPersonCamera : Camera
    {
        #region Fields

        private Vector3 velocity;
        private Vector3 acceleration;

        private float velocityDecayRate = 0.1f;

        private float accelerationMagnitude = 100.0f;
        private float rotationSpeed = MathHelper.TwoPi / 6.0f;

        private InputState inputState;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of FirstPersonCamera.
        /// </summary>
        public FirstPersonCamera()
        {
            inputState = new InputState();
        }

        /// <summary>
        /// Initializes a new instance of FirstPersonCamera.
        /// </summary>
        public FirstPersonCamera(float accelerationMagnitude, float rotationSpeed, float velocityDecayRate)
        {
            inputState = new InputState();

            this.accelerationMagnitude = accelerationMagnitude;
            this.rotationSpeed = rotationSpeed;
            this.velocityDecayRate = velocityDecayRate;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the magnitude of acceleration applied when reacting to input.
        /// </summary>
        public float AccelerationMagnitude
        {
            get { return accelerationMagnitude; }
            set { accelerationMagnitude = value; }
        }

        /// <summary>
        /// Gets or sets the speed of rotation of the camera.
        /// </summary>
        public float RotationSpeed
        {
            get { return rotationSpeed; }
            set { rotationSpeed = value; }
        }

        /// <summary>
        /// Gets the velocity of the camera.
        /// </summary>
        public Vector3 Velocity
        {
            get { return velocity; }
        }

        /// <summary>
        /// Gets the acceleration of the camera.
        /// </summary>
        public Vector3 Acceleration
        {
            get { return acceleration; }
        }

        /// <summary>
        /// Gets or sets the rate at which linear velocity decays away.
        /// </summary>
        public float VelocityDecayRate
        {
            get { return velocityDecayRate; }
            set { velocityDecayRate = value; }
        }

        #endregion

        #region Update

        /// <summary>
        /// This reacts to input from the gamepad.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // React to input.

            inputState.Update();

            acceleration += direction * accelerationMagnitude * inputState.CurrentGamePadState.ThumbSticks.Left.Y;
            acceleration += right * accelerationMagnitude * inputState.CurrentGamePadState.ThumbSticks.Left.X;

            angles.X += rotationSpeed * inputState.CurrentGamePadState.ThumbSticks.Right.Y * dt;
            angles.Y -= rotationSpeed * inputState.CurrentGamePadState.ThumbSticks.Right.X * dt;

            // Integrate the equations of motion using a simple Euler scheme.
            velocity += acceleration * dt;
            position += velocity * dt;

            // Decay the motion variables

            acceleration = new Vector3();
            velocity *= velocityDecayRate;

            // Now recompute the matrices
            UpdateMatrices();
        }

        #endregion
    }
}