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
    public class Object
    {
        public int x;
        public int y;
        public Texture2D texture;

        public Object(int x, int y, Texture2D texture)
        {
            this.texture = texture;
            this.x = x;
            this.y = y;
        }
    }

    public class Boat : Object
    {
        public Boat(int x, int y, Texture2D texture) : base(x,y,texture)
        {
            this.texture = texture;
            this.x = x;
            this.y = y;
        }

        public void MoveRight()
        {
            x++;
            if (x > 31)
            {
                x = 31;
            }
        }

        public void MoveLeft(){
            x--;
            if (x < 0)
            {
                x = 0;
            }
        }
    }

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        List<Object> dynamic_objects;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Map main_map;
        Boat object_boat;
        public Texture2D boat;           //Dynamic object textures.

        //Variables to keep track of key releases.
        bool LEFT_PRESSED = false;
        bool RIGHT_PRESSED = false;

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
            main_map.Initialize(GraphicsDevice, Content);
            boat = Content.Load<Texture2D>(@"Boat");
            object_boat = new Boat(10, 2, boat);
            dynamic_objects.Add(object_boat);
            // TODO: use this.Content to load your game content here
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

            if (ks.IsKeyDown(Keys.Right))
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

            if (ks.IsKeyDown(Keys.Left))
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
                spriteBatch.Draw(o.texture, new Rectangle(o.x * 25, o.y * 25, o.texture.Width, o.texture.Height), Color.White);
                spriteBatch.End();
            }
        }
    }
}
