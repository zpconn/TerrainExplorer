#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace TerrainExplorer
{
    /// <summary>
    /// Base class for screens that contain a menu of options.
    /// </summary>
    abstract class MenuScreen : GameScreen
    {
        #region Fields

        List<string> menuEntries = new List<string>();
        int selectedEntry = 0;

        InputState inputState;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the list of menu entry strings, so derived classes can add or change menu content.
        /// </summary>
        protected IList<string> MenuEntries
        {
            get { return menuEntries; }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Creates a new MenuScreen.
        /// </summary>
        public MenuScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);

            inputState = new InputState();
        }

        #endregion

        #region Handle Input

        /// <summary>
        /// Responds to user input, changing the selected entry and accepting or cancelling the menu.
        /// </summary>
        public override void HandleInput()
        {
            if (inputState.MenuUp)
            {
                selectedEntry--;

                if (selectedEntry < 0)
                    selectedEntry = menuEntries.Count - 1;
            }

            if (inputState.MenuDown)
            {
                selectedEntry++;

                if (selectedEntry >= menuEntries.Count)
                    selectedEntry = 0;
            }

            if (inputState.MenuSelect)
                OnSelectEntry(selectedEntry);
            else if (inputState.MenuCancel)
                OnCancel();
        }


        /// <summary>
        /// Notifies derived classes that a menu entry has been chosen
        /// </summary>
        /// <param name="entryIndex"></param>
        protected abstract void OnSelectEntry(int entryIndex);


        /// <summary>
        /// Notifies derived classes that the menu has been canceled.
        /// </summary>
        protected abstract void OnCancel();

        #endregion

        #region Draw and Update

        /// <summary>
        /// Updates the input state.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            inputState.Update();
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }


        /// <summary>
        /// Draws the menu.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            Vector2 position = new Vector2(1024 / 2 - 50, 768 / 2 + 100);
            Viewport viewport = ScreenManager.GraphicsDevice.Viewport;

            // Make the menu slide into place during transitions.

            // Since 0 <= TransitionPosition <= 1, squaring TransitionPosition will make the animation start out fast
            // and then slow down at the end without taking the value out of its default range.
            float transitionOffset = (float)Math.Pow(TransitionPosition, 2);

            if (ScreenState == ScreenState.TransitionOn)
                position.X -= (position.X - viewport.X + 150) * transitionOffset;
            else
                position.X += (viewport.X + viewport.Width - position.X) * transitionOffset;

            // Remember the current X position so that we can make the selected item "swivel" left and right relative to it
            float positionXFix = position.X;

            // Draw each menu entry in turn
            ScreenManager.SpriteBatch.Begin();

            for (int i = 0; i < menuEntries.Count; ++i)
            {
                Color color;
                float scale;

                if (IsActive && (i == selectedEntry))
                {
                    // The selected entry is white and as a pulsating size
                    double time = gameTime.TotalGameTime.TotalSeconds;

                    float pulsate = (float)Math.Sin(time * 6);

                    color = Color.White;
                    scale = 1 + pulsate * 0.05f;
                    position.X = positionXFix + pulsate * 4;
                }
                else
                {
                    // Other entries are green
                    color = Color.Green;
                    scale = 1;
                    position.X = positionXFix;
                }

                color = new Color(color.R, color.G, color.B, TransitionAlpha);

                // Draw the text, centered on the middle of each line
                Vector2 origin = new Vector2(0, ScreenManager.Font.LineSpacing / 2);

                ScreenManager.SpriteBatch.DrawString(ScreenManager.Font, menuEntries[i], position, color,
                                                     0, origin, scale, SpriteEffects.None, 0);

                position.Y += ScreenManager.Font.LineSpacing;
            }

            ScreenManager.SpriteBatch.End();
        }

        #endregion
    }
}