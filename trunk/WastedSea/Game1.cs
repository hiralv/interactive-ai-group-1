using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace WastedSea
{
    

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        SpriteFont Font1;
        Vector2 FontPos;

        List<Object> dynamic_objects;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Map main_map;
        Boat object_boat;
        Robot object_robot;
        public Texture2D boat, debris,oil,robot;           //Dynamic object textures.
       

        //Variables to keep track of key releases.
        bool LEFT_PRESSED = false;
        bool RIGHT_PRESSED = false;
        bool SPACE_PRESSED = false;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            main_map = new Map(this);
            dynamic_objects = new List<Object>();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Font1 = Content.Load<SpriteFont>("SpriteFont1");
            FontPos = new Vector2(graphics.GraphicsDevice.Viewport.Width / 2, graphics.GraphicsDevice.Viewport.Height / 2);

            main_map.Initialize(GraphicsDevice, Content);
            boat = Content.Load<Texture2D>(@"Boat");
            debris = Content.Load<Texture2D>(@"Debris");
            oil = Content.Load<Texture2D>(@"Oil");
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
            robot = Content.Load<Texture2D>(@"Robot");
            object_robot = new Robot(35, 35, robot, spriteBatch);
            dynamic_objects.Add(object_robot);

        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

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

           
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);

            main_map.Draw(spriteBatch);

            base.Draw(gameTime);

            

            foreach (Object o in dynamic_objects)
            {
                spriteBatch.Begin();
                o.Draw();
                spriteBatch.End();
            }

            DrawString("Score: 0", 0, 0);
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
    }
}
