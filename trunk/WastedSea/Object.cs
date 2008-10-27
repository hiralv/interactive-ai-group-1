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
    //The base class for all game objects that go into the object lists.
    public class Object
    {
        public int x;
        public int y;
        public Texture2D texture;
        public double time;
        public double speed;

        public Object(int x, int y, Texture2D texture)
        {
            this.texture = texture;
            this.x = x;
            this.y = y;
        }

        public void MoveDown()
        {
            y++;
            if (y > 23)
            {
                y = 23;
            }
        }
        public void MoveRight()
        {
            x++;
            if (x > 31)
            {
                x = 31;
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
      
        public Robot(int x, int y, Texture2D texture)
            : base(x, y, texture)
        {
            this.texture = texture;
            this.x = x;
            this.y = y;
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
    }

    //The moveable player-controlled boat.
    public class Boat : Object
    {
        public Boat(int x, int y, Texture2D texture)
            : base(x, y, texture)
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

        public Debris(int x, int y, Texture2D texture)
            : base(x, y, texture)
        {
            this.texture = texture;
            this.x = x;
            this.y = y;
            time = 0;

            ran = new Random(x * y);
            speed = ran.Next(300, 1000);
        }

        //Causes the debri to float right to left and reset on the right side of the screen.
        public override void Think(TimeSpan elapsed_game_time)
        {
            time = time + elapsed_game_time.Milliseconds;

            if (time > speed)
            {
                //Moves left, but wraps.
                x--;
                if (x < 0)
                {
                    x = 31;
                }

                time = 0;
            }
        }
    }
}
