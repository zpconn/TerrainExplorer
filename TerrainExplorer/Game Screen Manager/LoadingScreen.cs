#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace TerrainExplorer
{
    /// <summary>
    /// The loading screen pauses the screen transition system between the transitioning of one screen to another
    /// to display a loading message.
    /// </summary>
    class LoadingScreen : GameScreen
    {
        #region Fields

        bool otherScreensAreGone;
        EventHandler<EventArgs> loadNextScreen;

        #endregion

        #region Initialization

        /// <summary>
        /// We make the constructor private to force use of the static Load method instead.
        /// </summary>
        private LoadingScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(0.5f);
        }

        /// <summary>
        /// Activates the loading screen and loads up the next screen with the provided callback.
        /// </summary>
        public static void Load(ScreenManager screenManager, EventHandler<EventArgs> loadNextScreen)
        {
            // Tell all the current screens to transition off.
            foreach (GameScreen screen in screenManager.GetScreens())
                screen.ExitScreen();

            // Create and activate the loading screen.
            LoadingScreen loadingScreen = new LoadingScreen();
            loadingScreen.loadNextScreen = loadNextScreen;

            screenManager.AddScreen(loadingScreen);
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Updates the loading screen.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            // If all other screens have transitioned off, we can actually start performing the load.
            if (otherScreensAreGone)
            {
                ScreenManager.RemoveScreen(this);

                loadNextScreen(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Draws the loading screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // If all the previous screens have transitioned off, then this will be the only active screen.
            // This is our cue to begin loading the next screen.

            if ((ScreenState == ScreenState.Active) && ScreenManager.GetScreens().Length == 1)
                otherScreensAreGone = true;
        }

        #endregion
    }
}