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
        }

        public virtual void Draw()
        {
            spriteBatch.Draw(texture, new Rectangle(x * 25, y * 25, texture.Width, texture.Height), Color.White);
        }

        public void MoveDown()
        {
            y++;
            if (y > y_max)
            {
                y = y_max;
            }
        }
        public void MoveRight()
        {
            x++;
            if (x > x_max)
            {
                x = x_max;
            }
        }
        public void MoveLeft()
        {
            x--;
            if (x < 0)
            {
                x = 0;
            }
        }

        public virtual void Think(TimeSpan elapsed_game_time) { }
    }

    //The moveable automated robot.
    public class Robot : Object
    {
        bool launched;
        bool moving_left;
        int pixels_x;
        int pixels_y;

        public Robot(int x, int y, Texture2D texture, SpriteBatch spriteBatch)
            : base(x, y, texture, spriteBatch)
        {
            this.texture = texture;
            this.x = x;
            this.y = y;
            pixels_x = x * 25;
            pixels_y = y * 25;
            launched = false;
            moving_left = true;
            time = 0;
            speed = 100;
        }

        public void Launch(int x, int y)
        {
            this.x = x;
            this.y = y + 1;
            launched = true;
        }

        
        public override void Think(TimeSpan elapsed_game_time)
        {
            if (launched)
            {
                time = time + elapsed_game_time.Milliseconds;

                if (time > speed)
                {
                    MoveDown();
                    time = 0;
                

                    if (y == 23)
                    {
                        if (moving_left)
                        {
                            if (x == 0)
                            {
                                moving_left = false;
                                MoveRight();
                            }
                            else
                            {
                                MoveLeft();
                            }
                        }
                        else
                        {
                            if (x == 31)
                            {
                                moving_left = true;
                                MoveLeft();
                            }
                            else
                            {
                                MoveRight();
                            }
                        }
                    }
                }
            }
        }

        //public override void Draw()
        //{
        //    spriteBatch.Draw(texture, new Rectangle(pixels_x, pixels_y, texture.Width, texture.Height), Color.White);
        //}
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
        }
    }

    //Floating debris that require our agent to use D* on the way home.
    public class Debris : Object
    {
        Random ran;
        int pixels_x;

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

            if (time > 25)
            {
                pixels_x -= time / 25;
                time = time % 25;
            }

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
