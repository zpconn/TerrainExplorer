#region Using Statements
using System;
using Microsoft.Xna.Framework;
#endregion

namespace TerrainExplorer
{
    /// <summary>
    /// Describes the screen transition state.
    /// </summary>
    public enum ScreenState
    {
        TransitionOn,
        Active,
        TransitionOff,
        Hidden,
    }

    /// <summary>
    /// A screen encapsulates all the drawing and updating logic for a single game state. The 
    /// interaction between game screens can give rise to a complicated and layered menu system.
    /// Screens are flexible self-contained modules used to implement everything from a loading screen
    /// to the main game itself.
    /// </summary>
    public abstract class GameScreen
    {
        #region Fields

        bool isPopup = false;
        TimeSpan transitionOnTime = TimeSpan.Zero;
        TimeSpan transitionOffTime = TimeSpan.Zero;
        float transitionPosition = 1;
        ScreenState screenState = ScreenState.TransitionOn;
        bool isExiting = false;
        bool otherScreenHasFocus;
        ScreenManager screenManager;

        #endregion

        #region Properties

        /// <summary>
        /// Signals whether this screen is only a small popup, in which case screens underneath this one do
        /// not need to transition off.
        /// </summary>
        public bool IsPopup
        {
            get { return isPopup; }
            protected set { isPopup = value; }
        }


        /// <summary>
        /// Indicates how long the screen takes to transition on when it is activated.
        /// </summary>
        public TimeSpan TransitionOnTime
        {
            get { return transitionOnTime; }
            protected set { transitionOnTime = value; }
        }


        /// <summary>
        /// Indicates how long the screen takes to transition off when it is deactivated.
        /// </summary>
        public TimeSpan TransitionOffTime
        {
            get { return transitionOffTime; }
            protected set { transitionOffTime = value; }
        }


        /// <summary>
        /// Represents how far into the transition process the screen is. Values range from 0 to 1, where
        /// 0 means it is fully active and 1 means it is fully hidden.
        /// </summary>
        public float TransitionPosition
        {
            get { return transitionPosition; }
            protected set { transitionPosition = value; }
        }


        /// <summary>
        /// To help in making screens fade off while transitioning, this property represents an alpha value
        /// that continuously moves from 255 (fully opaque) to 0 (fully transparent) or from 0 to 255 as
        /// the screen undergoes a transition process.
        /// </summary>
        public byte TransitionAlpha
        {
            get { return (byte)(255 - TransitionPosition * 255); }
        }


        /// <summary>
        /// Represents the current transition state of the screen.
        /// </summary>
        public ScreenState ScreenState
        {
            get { return screenState; }
            protected set { screenState = value; }
        }


        /// <summary>
        /// If a screen is transitioning off, it might be doing so because a new screen has been pushed on top of it,
        /// or it might be going away forever. This property indicates whether or not the second case is true of this screen.
        /// </summary>
        public bool IsExiting
        {
            get { return isExiting; }
            protected set { isExiting = value; }
        }


        /// <summary>
        /// Indicates whether this screen is active and will respond to user input.
        /// </summary>
        public bool IsActive
        {
            get
            {
                return !otherScreenHasFocus
                    && (screenState == ScreenState.TransitionOn || screenState == ScreenState.Active);
            }
        }


        /// <summary>
        /// Gets the screen manager to which this screen belongs.
        /// </summary>
        public ScreenManager ScreenManager
        {
            get { return screenManager; }
            internal set { screenManager = value; }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Load graphics content for the screen.
        /// </summary>
        public virtual void LoadGraphicsContent(bool loadAllContent) { }


        /// <summary>
        /// Unload content for the screen.
        /// </summary>
        public virtual void UnloadGraphicsContent(bool unloadAllContent) { }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Runs the screen logic and updates the transition position if needed. This method is called
        /// regardles of the screen's transition state.
        /// </summary>
        public virtual void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            this.otherScreenHasFocus = otherScreenHasFocus;

            if (isExiting)
            {
                // If the screen is going away forever, it should transition off.
                screenState = ScreenState.TransitionOff;

                // If the transition is done, kill this screen.
                if (!UpdateTransition(gameTime, transitionOffTime, 1))
                {
                    ScreenManager.RemoveScreen(this);

                    isExiting = false;
                }
            }
            else if (coveredByOtherScreen)
            {
                // Transition off this screen, but do not remove it from the screen list

                // If it's still busy transitioning, make the screen state reflect this
                if (UpdateTransition(gameTime, transitionOffTime, 1))
                {
                    screenState = ScreenState.TransitionOff;
                }
                else
                {
                    // Transition finished
                    screenState = ScreenState.Hidden;
                }
            }
            else
            {
                // Make sure the screen has transitioned on
                if (UpdateTransition(gameTime, transitionOnTime, -1))
                {
                    // Still busy transitioning
                    screenState = ScreenState.TransitionOn;
                }
                else
                {
                    // Transition finished
                    screenState = ScreenState.Active;
                }
            }
        }


        /// <summary>
        /// Helper for updating the screen transition position.
        /// </summary>
        /// <returns>
        /// True if a transition is in progress and false otherwise.
        /// </returns>
        bool UpdateTransition(GameTime gameTime, TimeSpan time, int direction)
        {
            // Calculate by how much the transition position should be updated
            float transitionDelta;

            if (time == TimeSpan.Zero)
                transitionDelta = 1;
            else
                transitionDelta = (float)(gameTime.ElapsedGameTime.TotalMilliseconds / time.TotalMilliseconds);

            // Update the transition position
            transitionPosition += transitionDelta * direction;

            // Did we reach the end of the transition?
            if ((transitionPosition <= 0) || (transitionPosition >= 1))
            {
                transitionPosition = MathHelper.Clamp(transitionPosition, 0, 1);
                return false;
            }

            return true;
        }


        /// <summary>
        /// Allows the screen to handle input. This method is only called when the screen is active.
        /// </summary>
        public virtual void HandleInput() { }


        /// <summary>
        /// This is called when the screen should draw itself.
        /// </summary>
        public abstract void Draw(GameTime gameTime);

        #endregion

        #region Public Methods

        /// <summary>
        /// Tells the screen to go away. This will put the screen into a transition off state, after which
        /// the screen will be removed from the screen list.
        /// </summary>
        public void ExitScreen()
        {
            if (TransitionOffTime == TimeSpan.Zero)
            {
                // In this case, just remove it immediately
                ScreenManager.RemoveScreen(this);
            }
            else
            {
                // Otherwise let it run through the transition off animation
                isExiting = true;
            }
        }

        #endregion
    }
}