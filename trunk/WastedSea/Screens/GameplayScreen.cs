#region File Description
//-----------------------------------------------------------------------------
// GameplayScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
#endregion

namespace WastedSea
{
    /// <summary>
    /// This screen implements the actual game logic. It is just a
    /// placeholder to get the idea across: you'll probably want to
    /// put some more interesting gameplay in here!
    /// </summary>
    class GameplayScreen : GameScreen
    {
        #region Fields

        ContentManager content;
        SpriteBatch spriteBatch;
        SpriteFont Font1;
        Vector2 FontPos;
        Map main_map;
        Boat object_boat;
        Robot object_robot;
        Game1 cur = new Game1();
        List<Object> dynamic_objects;
        public Texture2D boat, debris, oil, robot;

        Random random = new Random();

        //Variables to keep track of key releases.
        bool LEFT_PRESSED = false;
        bool RIGHT_PRESSED = false;
        bool SPACE_PRESSED = false;

        #endregion

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public GameplayScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(1.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);

            main_map = new Map(cur);
            dynamic_objects = new List<Object>();
        }


        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent()
        {
            if (content == null)
                content = new ContentManager(ScreenManager.Game.Services, "Content");

            Font1 = content.Load<SpriteFont>("SpriteFont1");
            FontPos = new Vector2(ScreenManager.GraphicsDevice.Viewport.Width/2, ScreenManager.GraphicsDevice.Viewport.Height/2);
            spriteBatch = ScreenManager.SpriteBatch;
            main_map.Initialize(ScreenManager.GraphicsDevice, content);
            boat = content.Load<Texture2D>(@"Boat");
            object_boat = new Boat(10, 4, boat, spriteBatch);
            debris = content.Load<Texture2D>(@"Debris");
            oil = content.Load<Texture2D>(@"Oil");
            object_boat = new Boat(10, 4, boat, spriteBatch);
            dynamic_objects.Add(object_boat);

            Random ran_number = new Random();

            //Create all of the debris.
            int MAX_DEBRIS = 10;
            Debris new_debris;
            for (int i = 0; i < MAX_DEBRIS; i++)
            {
                int ran_x = ran_number.Next(0, 31);
                int ran_y = ran_number.Next(5, 7);
                new_debris = new Debris(ran_x, ran_y, debris, spriteBatch);
                dynamic_objects.Add(new_debris);
            }

            //Create all of the oil.
            int MAX_OIL = 30;
            Object new_oil;
            for (int i = 0; i < MAX_OIL; i++)
            {
                int ran_x = ran_number.Next(0, 31);
                int ran_y = ran_number.Next(8, 23);
                new_oil = new Object(ran_x, ran_y, oil, spriteBatch);
                dynamic_objects.Add(new_oil);
            }

            //Create robot.
            robot = content.Load<Texture2D>(@"Robot");
            object_robot = new Robot(35, 35, robot, spriteBatch);
            dynamic_objects.Add(object_robot);


            // A real game would probably have more content than this sample, so
            // it would take longer to load. We simulate that by delaying for a
            // while, giving you a chance to admire the beautiful loading screen.
            Thread.Sleep(1000);

            // once the load has finished, we use ResetElapsedTime to tell the game's
            // timing mechanism that we have just finished a very long frame, and that
            // it should not try to catch up.
            ScreenManager.Game.ResetElapsedTime();
        }


        /// <summary>
        /// Unload graphics content used by the game.
        /// </summary>
        public override void UnloadContent()
        {
            content.Unload();
        }


        #endregion

        #region Update and Draw


        /// <summary>
        /// Updates the state of the game. This method checks the GameScreen.IsActive
        /// property, so the game will stop updating when the pause menu is active,
        /// or if you tab away to a different application.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            if (IsActive)
            {
                KeyboardState ks = Keyboard.GetState();

                if (ks.IsKeyDown(Keys.Right))       //Right arrow.
                {
                    RIGHT_PRESSED = true;
                }
                else
                {
                    if (RIGHT_PRESSED)
                    {
                        object_boat.MoveRight();
                        RIGHT_PRESSED = false;
                    }
                }

                if (ks.IsKeyDown(Keys.Left))            //Left arrow.
                {
                    LEFT_PRESSED = true;
                }
                else
                {
                    if (LEFT_PRESSED)
                    {
                        object_boat.MoveLeft();
                        LEFT_PRESSED = false;
                    }
                }

                if (ks.IsKeyDown(Keys.Space))            //Space bar.
                {
                    SPACE_PRESSED = true;
                }
                else
                {
                    if (SPACE_PRESSED)
                    {
                        object_robot.Launch(object_boat.x, object_boat.y);
                        SPACE_PRESSED = false;
                    }
                }

                //Allow our dynamic game objects their think cycle.
                foreach (Object o in dynamic_objects)
                {
                    o.Think(gameTime.ElapsedGameTime);
                }


            }
        }


        /// <summary>
        /// Lets the game respond to player input. Unlike the Update method,
        /// this will only be called when the gameplay screen is active.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            if (input.PauseGame)
            {
                // If they pressed pause, bring up the pause menu screen.
                ScreenManager.AddScreen(new PauseMenuScreen());
            }
            else
            {
            }
        }


        /// <summary>
        /// Draws the gameplay screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // Our player and enemy are both actually just text strings.                       
            main_map.Draw(spriteBatch);

            foreach (Object o in dynamic_objects)
            {
                spriteBatch.Begin();
                o.Draw();
                spriteBatch.End();
            }

            DrawString("Score: 0", 0, 0);
            // If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition > 0)
                ScreenManager.FadeBackBufferToBlack(255 - TransitionAlpha);
        }


        public void DrawString(string output, int x, int y)
        {
            spriteBatch.Begin();
            FontPos.X = x;
            FontPos.Y = y;
            //Vector2 FontOrigin = Font1.MeasureString(output) / 2;
            Vector2 FontOrigin = new Vector2(0, 0);
            spriteBatch.DrawString(Font1, output, FontPos, Color.Black, 0, FontOrigin, 1.0f, SpriteEffects.None, 0.5f);
            spriteBatch.End();
        }


        #endregion
    }
}
