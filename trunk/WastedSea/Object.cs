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
    public enum ObjectType
    {
        DEBRIS,
        OIL,
        BOAT,
        BIRD,
        ROBOT,
        POWERMETER
    }

    public enum Direction
    {
        UP,
        DOWN,
        LEFT,
        RIGHT
    }

    //The base class for all game objects that go doubleo the object lists.
    public class Object
    {
        public ObjectType type;
        public int x_max = 31;
        public int y_max = 22;
        public int grid_to_pixels = 25;

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

        public Direction current_dir;

        public Object(int x, int y, Texture2D texture, SpriteBatch spriteBatch)
        {
            this.texture = texture;
            this.x = x;
            this.y = y;
            this.spriteBatch = spriteBatch;     
            pixels_x = x * grid_to_pixels;                  //Starting pixel locations of object.
            pixels_y = y * grid_to_pixels;                  //Starting pixel locations of object.
            time = 0;
            ran = new Random(x * y);
        }

        public void SetPosition(int x, int y)
        {
            this.x = x;
            this.y = y;
            pixels_x = x * 25;
            pixels_y = y * 25;
        }

        public virtual void Draw()
        {
            spriteBatch.Draw(texture, new Rectangle(x * grid_to_pixels, y * grid_to_pixels, texture.Width, texture.Height), Color.White);
        }

        public void MoveDown(TimeSpan elapsed_game_time)
        {
            time += (elapsed_game_time.Milliseconds + speed);

            pixels_y += time / grid_to_pixels;
            time = time % grid_to_pixels;

            if (pixels_y > y_max * grid_to_pixels)
            {
                pixels_y = y_max * grid_to_pixels;
            }
        }
        public void MoveLeft(TimeSpan elapsed_game_time)
        {
            time += (elapsed_game_time.Milliseconds + speed);

            pixels_x -= time / grid_to_pixels;
            time = time % grid_to_pixels;

            if (pixels_x < 0)
            {
                pixels_x = 0;
            }
        }

        public void MoveRight(TimeSpan elapsed_game_time)
        {
            time += (elapsed_game_time.Milliseconds + speed);

            pixels_x += time / grid_to_pixels;
            time = time % grid_to_pixels;

            if (pixels_x > x_max * grid_to_pixels)
            {
                pixels_x = x_max * grid_to_pixels;
            }
        }

        public virtual void Think(TimeSpan elapsed_game_time) { }
    }

    //The moveable automated robot.
    public class Robot : Object
    {
        public bool launched;
        bool moving_left;
        public int timeSinceLaunched;
        public Powermeter power;

        public Robot(int x, int y, Texture2D texture, SpriteBatch spriteBatch)
            : base(x, y, texture, spriteBatch)
        {
            type = ObjectType.ROBOT;
            launched = false;
            moving_left = true;
            speed = 100;
        }

        public void Launch(int x, int y)
        {
            this.x = x;
            this.y = y + 1;
            pixels_x = x * grid_to_pixels;
            pixels_y = y * grid_to_pixels;
            launched = true;
            timeSinceLaunched = 0;
        }

        public void Retun()
        {
            launched = false;
        }

        
        public override void Think(TimeSpan elapsed_game_time)
        {
            if (launched)
            {
                SenseOil(elapsed_game_time);
                MoveDown(elapsed_game_time);
                time = 0;
            
                if (pixels_y == y_max * grid_to_pixels)
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
                        if (pixels_x == x_max * grid_to_pixels)
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

        private void SenseOil(TimeSpan elapsed_game_time)
        {
            int MAX_SENSE_RANGE = 5;
            int xdiff, ydiff;
            foreach (Oil oil in Oil.oil_list)
            {
                xdiff = Math.Abs(x - oil.x);
                ydiff = Math.Abs(y - oil.y);

                if (xdiff < MAX_SENSE_RANGE && ydiff < MAX_SENSE_RANGE)
                    power.energy = power.energy - Math.Max(xdiff, ydiff);
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
            type = ObjectType.BOAT;
            speed = 2;
        }

        public override void Draw()
        {
            spriteBatch.Draw(texture, new Rectangle(pixels_x, y * grid_to_pixels, texture.Width, texture.Height), Color.White);
        }
    }

    //The bird.
    public class Bird : Object
    {
        public Bird(int x, int y, Texture2D texture, SpriteBatch spriteBatch)
            : base(x, y, texture, spriteBatch)
        {
            type = ObjectType.BIRD;
            speed = -3;
            current_dir = Direction.UP;
        }

        public override void Think(TimeSpan elapsed_game_time)
        {
            time += (elapsed_game_time.Milliseconds + speed);

            pixels_x -= time / grid_to_pixels;

            //if (pixels_y / 25 < y)
            //{
            //    current_dir = Direction.DOWN;
            //}

            //if (pixels_y / 25 > y)
            //{
            //    current_dir = Direction.UP;
            //}

            //if (current_dir == Direction.UP)
            //{
            //    pixels_y -= ran.Next(0,2);
            //}
            //else
            //{
            //    pixels_y += ran.Next(0, 2);
            //}

            time = time % grid_to_pixels;

            if (pixels_x < 0)
            {
                pixels_x = x_max * grid_to_pixels;
            }
        }

        public override void Draw()
        {
            spriteBatch.Draw(texture, new Rectangle(pixels_x, pixels_y, texture.Width, texture.Height), Color.White);
        }
    }

    //Floating debris that require our agent to use D* on the way home.
    public class Debris : Object
    {
        public Debris(int x, int y, Texture2D texture, SpriteBatch spriteBatch)
            : base(x, y, texture, spriteBatch)
        {
            type = ObjectType.DEBRIS;
            speed = ran.Next(1, 10);
        }

        //Causes the debri to float right to left and reset on the right side of the screen.
        public override void Think(TimeSpan elapsed_game_time)
        {
            time += (elapsed_game_time.Milliseconds + speed);

            pixels_x -= time / grid_to_pixels;
            time = time % grid_to_pixels;

            if (pixels_x < 0)
            {
                pixels_x = x_max * grid_to_pixels;
            }
        }

        public override void Draw()
        {
            spriteBatch.Draw(texture, new Rectangle(pixels_x, y * grid_to_pixels, texture.Width, texture.Height), Color.White);
        }
    }

    //Oil the floating oil.
    public class Oil : Object
    {
        static public List<Oil> oil_list = new List<Oil>();

        public Oil(int x, int y, Texture2D texture, SpriteBatch spriteBatch)
            : base(x, y, texture, spriteBatch)
        {
            type = ObjectType.OIL;
        }

        //Causes the debri to float right to left and reset on the right side of the screen.
        public override void Think(TimeSpan elapsed_game_time)
        {
           
        }

        public override void Draw()
        {
            spriteBatch.Draw(texture, new Rectangle(pixels_x, y * grid_to_pixels, texture.Width, texture.Height), Color.White);
        }
    }

    public class Button : Object
    {
        public Button(int x, int y, Texture2D texture, SpriteBatch spriteBatch)
            : base(x, y, texture, spriteBatch)
        {

        }

        public override void Draw()
        {
            spriteBatch.Draw(texture, new Rectangle(pixels_x, y * grid_to_pixels, texture.Width, texture.Height), Color.White);
        }
    }

    public class Powermeter : Object
    {
        public int energy;
        public Texture2D power;
        
        public Powermeter(int x, int y, Texture2D texture, SpriteBatch spriteBatch, Texture2D power, int energy)
            : base(x, y, texture, spriteBatch)
        {
            this.energy = energy;
            type = ObjectType.POWERMETER;
            this.power = power;
        }

        public override void Think(TimeSpan elapsed_game_time)
        {

        }

        public override void Draw()
        {
            spriteBatch.Draw(texture, new Rectangle(pixels_x, y * grid_to_pixels, texture.Width, texture.Height), Color.White);

            for (int i = 0; i < energy; i++)
            {
                spriteBatch.Draw(power, new Rectangle(pixels_x + (i*power.Width+1), pixels_y + 4, power.Width, power.Height), Color.White);
            }
        }
    }
}
