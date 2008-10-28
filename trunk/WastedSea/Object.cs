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
    //The base class for all game objects that go doubleo the object lists.
    public class Object
    {
        public int x_max = 31;
        public int y_max = 23;

        public int x;
        public int y;

        public int offset_x;    //Converts the grid coordinates to pixel coordinates for smoother animations.
        public int offset_y;

        public Random ran;
        public int pixels_x;
        public int pixels_y;

        public Texture2D texture;
        public int time;
        public int speed;
        public SpriteBatch spriteBatch;

        public Object(int x, int y, Texture2D texture, SpriteBatch spriteBatch)
        {
            this.texture = texture;
            this.x = x;
            this.y = y;
            this.spriteBatch = spriteBatch;     
            pixels_x = x * 25;                  //Starting pixel locations of object.
            pixels_y = y * 25;                  //Starting pixel locations of object.
        }

        public virtual void Draw()
        {
            spriteBatch.Draw(texture, new Rectangle(x * 25, y * 25, texture.Width, texture.Height), Color.White);
        }

        public void MoveDown(TimeSpan elapsed_game_time)
        {
            time += (elapsed_game_time.Milliseconds + speed);

            pixels_y += time / 25;
            time = time % 25;

            if (pixels_y > 23 * 25)
            {
                pixels_y = 23 * 25;
            }
        }
        public void MoveLeft(TimeSpan elapsed_game_time)
        {
            time += (elapsed_game_time.Milliseconds + speed);

            pixels_x -= time / 25;
            time = time % 25;

            if (pixels_x < 0)
            {
                pixels_x = 0;
            }
        }

        public void MoveRight(TimeSpan elapsed_game_time)
        {
            time += (elapsed_game_time.Milliseconds + speed);

            pixels_x += time / 25;
            time = time % 25;

            if (pixels_x > 31 * 25)
            {
                pixels_x = 31 * 25;
            }
        }

        public virtual void Think(TimeSpan elapsed_game_time) { }
    }

    //The moveable automated robot.
    public class Robot : Object
    {
        bool launched;
        bool moving_left;

        public Robot(int x, int y, Texture2D texture, SpriteBatch spriteBatch)
            : base(x, y, texture, spriteBatch)
        {
            
            launched = false;
            moving_left = true;
            time = 0;
            speed = 100;
        }

        public void Launch(int x, int y)
        {
            this.x = x;
            this.y = y + 1;
            pixels_x = x * 25;
            pixels_y = y * 25;
            launched = true;
        }

        
        public override void Think(TimeSpan elapsed_game_time)
        {
            if (launched)
            {
                MoveDown(elapsed_game_time);
                time = 0;
            
                if (pixels_y == 23 * 25)
                {
                    if (moving_left)
                    {
                        if (pixels_x == 0)
                        {
                            moving_left = false;
                            MoveRight(elapsed_game_time);
                        }
                        else
                        {
                            MoveLeft(elapsed_game_time);
                        }
                    }
                    else
                    {
                        if (pixels_x == 31 * 25)
                        {
                            moving_left = true;
                            MoveLeft(elapsed_game_time);
                        }
                        else
                        {
                            MoveRight(elapsed_game_time);
                        }
                    }
                }   
            }
        }

        public override void Draw()
        {
            spriteBatch.Draw(texture, new Rectangle(pixels_x, pixels_y, texture.Width, texture.Height), Color.White);
        }
    }

    //The moveable player-controlled boat.
    public class Boat : Object
    {
        public Boat(int x, int y, Texture2D texture, SpriteBatch spriteBatch)
            : base(x, y, texture, spriteBatch)
        {
            this.texture = texture;
            this.x = x;
            this.y = y;
            time = 0;
            pixels_x = x * 25;
            speed = 2;
        }



        public override void Draw()
        {
            spriteBatch.Draw(texture, new Rectangle(pixels_x, y * 25, texture.Width, texture.Height), Color.White);
        }
    }

    //Floating debris that require our agent to use D* on the way home.
    public class Debris : Object
    {
        public Debris(int x, int y, Texture2D texture, SpriteBatch spriteBatch)
            : base(x, y, texture, spriteBatch)
        {
            this.texture = texture;
            this.x = x;
            this.y = y;
            time = 0;
            pixels_x = x * 25;

            ran = new Random(x * y);
            speed = ran.Next(1, 10);
        }

        //Causes the debri to float right to left and reset on the right side of the screen.
        public override void Think(TimeSpan elapsed_game_time)
        {
            time += (elapsed_game_time.Milliseconds + speed);

            pixels_x -= time / 25;
            time = time % 25;

            if (pixels_x < 0)
            {
                pixels_x = 31 * 25;
            }
        }

        public override void Draw()
        {
            spriteBatch.Draw(texture, new Rectangle(pixels_x, y * 25, texture.Width, texture.Height), Color.White);
        }
    }
}
