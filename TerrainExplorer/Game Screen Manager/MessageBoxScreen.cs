#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace TerrainExplorer
{
    /// <summary>
    /// A popup message box screen, usually used to display "Are you sure?" confirmation message but also used for
    /// things like the credits.
    /// </summary>
    class MessageBoxScreen : GameScreen
    {
        #region Fields

        string message;
        string backgroundContentName;
        Texture2D gradientTexture;

        InputState inputState;

        #endregion

        #region Events

        public event EventHandler<EventArgs> Accepted;
        public event EventHandler<EventArgs> Cancelled;

        #endregion

        #region Initialization

        /// <summary>
        /// Creates a new message box and addes usage information to the beginning of the message.
        /// </summary>
        /// <param name="message">The string to display</param>
        /// <param name="backgroundContentName">The name (without extension) of the texture content to use as 
        /// the background of this message box.</param>
        public MessageBoxScreen(string message, string backgroundContentName)
        {
            this.backgroundContentName = backgroundContentName;

            const string usageText = "\nA = Ok" + "\nB = Cancel";
            this.message = message + usageText;

            IsPopup = true;

            TransitionOnTime = TimeSpan.FromSeconds(0.2);
            TransitionOffTime = TimeSpan.FromSeconds(0.2);

            inputState = new InputState();
        }


        /// <summary>
        /// Loads graphics for this screen using the shared ContentManager in the ScreenManager.
        /// </summary>
        public override void LoadGraphicsContent(bool loadAllContent)
        {
            if (loadAllContent)
            {
                gradientTexture = ScreenManager.Content.Load<Texture2D>(backgroundContentName);
            }
        }

        #endregion

        #region Handle Input

        /// <summary>
        /// Responds to user input, accepting or cancelling the message box.
        /// </summary>
        public override void HandleInput()
        {
            inputState.Update();

            if (inputState.MenuSelect)
            {
                // Raise the accepted event, then exit the message box
                if (Accepted != null)
                    Accepted(this, EventArgs.Empty);

                ExitScreen();
            }
            else 
            if (inputState.MenuCancel)
            {
                // Raise the cancelled event, then exit the message box
                if (Cancelled != null)
                    Cancelled(this, EventArgs.Empty);

                ExitScreen();
            }
        }

        #endregion

        #region Draw

        /// <summary>
        /// Draws the message box
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // Darken any screens drawn beneath this popup
            ScreenManager.FadeBackBufferToBlack(ScreenManager.SpriteBatch, TransitionAlpha * 2 / 3);

            // Center the message text in the viewport
            Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
            Vector2 viewportSize = new Vector2(viewport.Width, viewport.Height);
            Vector2 textSize = ScreenManager.Font.MeasureString(message);
            Vector2 textPosition = (viewportSize - textSize) / 2;

            // Inluce a border somewhat larger than the text itself
            int hPad = (int)textSize.X;
            int vPad = (int)textSize.Y;

            Rectangle backgroundRectangle = new Rectangle((int)textPosition.X - 32,
                                                          (int)textPosition.Y - 32,
                                                          viewport.Width - 2 * ((int)textPosition.X - 32),
                                                          (int)textPosition.Y + 32);

            // Fade the popup alpha during transition
            Color color = new Color(255, 255, 255, TransitionAlpha);

            ScreenManager.SpriteBatch.Begin();

            // Draw the background rectangle
            ScreenManager.SpriteBatch.Draw(gradientTexture, backgroundRectangle, color);

            // Draw the message box text
            ScreenManager.SpriteBatch.DrawString(ScreenManager.Font, message, textPosition, color);

            ScreenManager.SpriteBatch.End();
        }

        #endregion
    }
}