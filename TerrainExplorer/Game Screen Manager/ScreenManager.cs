#region Using Statements
using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace TerrainExplorer
{
    /// <summary>
    /// The screen manager is a game component that manages a stack of GameScreen objects and promotes
    /// loose coupling between them by providing high-level transition logic and routing input to the 
    /// top-most active screen.
    /// </summary>
    public class ScreenManager : DrawableGameComponent
    {
        #region Fields

        List<GameScreen> screens = new List<GameScreen>();
        List<GameScreen> screensToUpdate = new List<GameScreen>();

        ContentManager content;
        IGraphicsDeviceService graphicsDeviceService;

        Texture2D blankTexture;
        SpriteBatch spriteBatch;
        SpriteFont spriteFont;

        #endregion

        #region Properties

        /// <summary>
        /// Expose access to the Game instance, which is protected in the default GameComponent class.
        /// </summary>
        new public Game Game
        {
            get { return base.Game; }
        }


        /// <summary>
        /// Expose access to the graphics device, which is protected in the default GameComponent class.
        /// </summary>
        new public GraphicsDevice GraphicsDevice
        {
            get { return base.GraphicsDevice; }
        }


        /// <summary>
        /// Gets a content manager used to load data that is shared between multiple screens.
        /// </summary>
        public ContentManager Content
        {
            get { return content; }
        }


        /// <summary>
        /// A sprite batch shared by all the screens.
        /// </summary>
        public SpriteBatch SpriteBatch
        {
            get { return spriteBatch; }
        }


        /// <summary>
        /// A font shared by all the screens for easy text rendering.
        /// </summary>
        public SpriteFont Font
        {
            get { return spriteFont; }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Constructs a new screen manager component.
        /// </summary>
        public ScreenManager(Game game)
            : base(game)
        {
            content = new ContentManager(game.Services);

            graphicsDeviceService = (IGraphicsDeviceService)game.Services.GetService(typeof(IGraphicsDeviceService));

            if (graphicsDeviceService == null)
                throw new InvalidOperationException("No graphics device service.");

            SetupInputMap();
        }


        protected override void LoadGraphicsContent(bool loadAllContent)
        {
            if (loadAllContent)
            {
                // Load content belonging to the screen manager
                blankTexture = content.Load<Texture2D>("Content\\Textures\\blank");
                spriteFont = content.Load<SpriteFont>("Content\\Textures\\menuFont");
                spriteBatch = new SpriteBatch(GraphicsDevice);
            }

            // Let each screen to load its content
            foreach (GameScreen screen in screens)
            {
                screen.LoadGraphicsContent(loadAllContent);
            }
        }


        protected override void UnloadGraphicsContent(bool unloadAllContent)
        {
            if (unloadAllContent)
            {
                // Unload content belonging to the screen manager
                content.Unload();
            }

            // Let each screen to unload its content
            foreach (GameScreen screen in screens)
            {
                screen.UnloadGraphicsContent(unloadAllContent);
            }
        }


        /// <summary>
        /// Sets all the input bindings used for navigating the menu
        /// </summary>
        private void SetupInputMap()
        {
            //XGameInput inputMap = (XGameInput)Game.Services.GetService(typeof(XGameInput));

            //inputMap.AddPlayerControl(PlayerIndex.One, "Menu Up", XGameControlInput.ControlInputType.Switch);
            //inputMap.AssignControlInput(PlayerIndex.One, "Menu Up", XGameInput.XGameInputPad.Up);

            //inputMap.AddPlayerControl(PlayerIndex.One, "Menu Down", XGameControlInput.ControlInputType.Switch);
            //inputMap.AssignControlInput(PlayerIndex.One, "Menu Down", XGameInput.XGameInputPad.Down);

            //inputMap.AddPlayerControl(PlayerIndex.One, "Menu Accept", XGameControlInput.ControlInputType.Switch);
            //inputMap.AssignControlInput(PlayerIndex.One, "Menu Accept", XGameInput.XGameInputPad.A);

            //inputMap.AddPlayerControl(PlayerIndex.One, "Menu Cancel", XGameControlInput.ControlInputType.Switch);
            //inputMap.AssignControlInput(PlayerIndex.One, "Menu Cancel", XGameInput.XGameInputPad.B);
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Allows each screen to run logic.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            // Copy the master screen list to avoid confusion if the process of updating one screen adds or removes others
            screensToUpdate.Clear();

            foreach (GameScreen screen in screens)
                screensToUpdate.Add(screen);

            bool otherScreenHasFocus = !Game.IsActive;
            bool coveredByOtherScreen = false;

            while (screensToUpdate.Count > 0)
            {
                // Pop the topmost screen off the waiting list
                GameScreen screen = screensToUpdate[screensToUpdate.Count - 1];
                screensToUpdate.RemoveAt(screensToUpdate.Count - 1);

                // Update the screen
                screen.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

                if (screen.ScreenState == ScreenState.TransitionOn ||
                    screen.ScreenState == ScreenState.Active)
                {
                    // If this screen is active or becoming active and has the focus, let it handle input
                    if (!otherScreenHasFocus)
                    {
                        screen.HandleInput();

                        // Since this screen now has the focus, other screens updated subsequently in this loop do not.
                        otherScreenHasFocus = true;
                    }

                    // If this is not a pop-up, inform any subsequent screens that they are covered by it
                    if (!screen.IsPopup)
                        coveredByOtherScreen = true;
                }
            }
        }


        /// <summary>
        /// Tells each screen to draw itself.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            foreach (GameScreen screen in screens)
            {
                if (screen.ScreenState == ScreenState.Hidden)
                    continue;

                screen.Draw(gameTime);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a new screen to the screen manager.
        /// </summary>
        public void AddScreen(GameScreen screen)
        {
            screen.ScreenManager = this;

            // If we have a graphics device, tell the screen to load its content
            if ((graphicsDeviceService != null) && (graphicsDeviceService.GraphicsDevice != null))
            {
                screen.LoadGraphicsContent(true);
            }

            screens.Add(screen);
        }


        /// <summary>
        /// Removes a screen from the screen manager. To make the screen transition off, 
        /// call screen.ExitScreen() instead of this method.
        /// </summary>
        public void RemoveScreen(GameScreen screen)
        {
            // If we have a graphics device, tell the screen to unload its content
            if ((graphicsDeviceService != null) && (graphicsDeviceService.GraphicsDevice != null))
            {
                screen.UnloadGraphicsContent(true);
            }

            screens.Remove(screen);
            screensToUpdate.Remove(screen);
        }


        /// <summary>
        /// Exposes the screen list as an array. We return a copy to ensure that the master list is only ever
        /// changed directly through the interface to ScreenManager.
        /// </summary>
        public GameScreen[] GetScreens()
        {
            return screens.ToArray();
        }


        /// <summary>
        /// A helper method for drawing a translucent black fullscreen sprite, used for fading screens in and out 
        /// and for darkening the background behind popups.
        /// </summary>
        /// <param name="alpha"></param>
        public void FadeBackBufferToBlack(SpriteBatch spriteBatch, int alpha)
        {
            Viewport viewport = GraphicsDevice.Viewport;

            spriteBatch.Begin();

            spriteBatch.Draw(blankTexture,
                             new Rectangle(0, 0, viewport.Width, viewport.Height),
                             new Color(0, 0, 0, (byte)alpha));

            spriteBatch.End();
        }

        #endregion
    }
}