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
        #region Variables
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
        #endregion

        #region Methods
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

        public void MoveUp(TimeSpan elapsed_game_time)
        {
            time += (elapsed_game_time.Milliseconds + speed);

            pixels_y -= time / grid_to_pixels;
            time = time % grid_to_pixels;

            if (pixels_y > y_max * grid_to_pixels)
            {
                pixels_y = y_max * grid_to_pixels;
            }

            y = (int)Math.Floor((double)(pixels_y / 25));
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

            y = (int)Math.Floor((double)(pixels_y / 25));
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
            x = (int)Math.Floor((double)(pixels_x / 25));
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

            x = (int)Math.Floor((double)(pixels_x / 25));
        }

        public virtual void Think(TimeSpan elapsed_game_time) { } 
        #endregion
    }

    //The moveable automated robot.
    public class Robot : Object
    {
        #region Variables
        public bool launched;
        bool moving_left;
        public int timeSinceLaunched;
        public Powermeter power;
        public int minDepth;
        public int maxDepth;
        public int maxOilRange;
        public int depth;
        public static List<Oil> sensedOil = new List<Oil>();
        public static List<Oil> removeOil = new List<Oil>();
        DStar dstar;
        int[,] actual_cost_array;
        public int[,] areaKnowledge;
        public int boatx, boaty;
        int dstar_timer;
        
        #endregion

        public Robot(int x, int y, Texture2D texture, SpriteBatch spriteBatch)
            : base(x, y, texture, spriteBatch)
        {
            type = ObjectType.ROBOT;
            launched = false;
            moving_left = true;
            speed = 100;
            dstar = new DStar();
        }

        public void Launch(int x, int y)
        {
            this.x = x;
            this.y = y + 1;
            pixels_x = x * grid_to_pixels;
            pixels_y = y * grid_to_pixels;
            launched = true;
            timeSinceLaunched = 0;
            maxDepth = y_max - 4;
            areaKnowledge = new int[24, 32];
            

            for (int i = 0; i < 24; i++)
            {
                for (int j = 0; j < 32; j++)
                    areaKnowledge[i, j] = -99999;
            }

            for (int i = minDepth + 5; i < maxDepth + 5; i++)
            {
                for (int j = 0; j < 32; j++)
                    areaKnowledge[i, j] = 0;
            }
        }

        public void Retun()
        {
            launched = false;
        }

        
        public override void Think(TimeSpan elapsed_game_time)
        {
            #region Move Left Right
            //if (launched)
            //{
            //    SenseOil(elapsed_game_time);
            //    MoveDown(elapsed_game_time);
            //    time = 0;

            //    if (pixels_y == y_max * grid_to_pixels)
            //    {
            //        if (moving_left)
            //        {
            //            if (pixels_x == 0)
            //            {
            //                moving_left = false;
            //                MoveRight(elapsed_game_time);
            //            }
            //            else
            //            {
            //                MoveLeft(elapsed_game_time);
            //            }
            //        }
            //        else
            //        {
            //            if (pixels_x == x_max * grid_to_pixels)
            //            {
            //                moving_left = true;
            //                MoveLeft(elapsed_game_time);
            //            }
            //            else
            //            {
            //                MoveRight(elapsed_game_time);
            //            }
            //        }
            //    }
            //}
            #endregion
            if (timeSinceLaunched % 120 == 0 && launched)
                power.energy--;

            depth = y - 4;

            if (launched)
            {
                if (power.energy < 3)
                {
                    Retun();
                    dstar.Start(pixels_x / 25, pixels_y / 25, (int)Math.Floor((double)boatx / 25), (int)Math.Floor((double)boaty / 25));
                    
                    //RETURNTOBOAT!
                }
                else if (depth < minDepth)//This would be changed to the oil value presumably
                {
                    MoveDown(elapsed_game_time);
                }
                //else if (maxDepth < 100)//This would be changed to the oil value presumably
                //{
                //    MoveUp(elapsed_game_time);
                //}
                else if (Robot.sensedOil.Count > 0)//Tells how close agent is to oil
                {

                    Oil oil = Robot.sensedOil[0];
                    //Robot.sensedOil.Remove(oil);

                    if (oil.x > x)
                        Move(elapsed_game_time, 1);
                    else if (oil.x < x)
                        Move(elapsed_game_time, 0);
                    else if (oil.y > y)
                        Move(elapsed_game_time, 3);
                    else if (oil.y < y)
                        Move(elapsed_game_time,2);


                    if (x == oil.x && oil.y == y)
                    {
                        Robot.sensedOil.Remove(oil);
                        Robot.removeOil.Add(oil);
                        //areaKnowledge[oil.x, oil.y] -= 500;
                    }


                    //foreach (Oil oil in Robot.sensedOil)
                    //{
                    //    if (x == oil.x && y == oil.y)
                    //    {
                    //        Robot.removeOil.Add(oil);
                    //        areaKnowledge[y, x] = -500;
                    //    }
                    //}

                    //foreach (Oil oil in Robot.removeOil)
                    //{
                    //    Robot.sensedOil.Remove(oil);
                    //}

                    //removeOil.Clear();
                    //CLEANOIL
                }
                else
                {
                    SenseOil();

                    //int direction = GetDirection(x, y, maxOilRange);

                    int direction = GetRandomDirecion();
                    //Random ran = new Random();
                    //int direction = ran.Next(0, 4);
                    Move(elapsed_game_time, direction);

                    

                    #region Move Left Right
                    //if (moving_left)
                    //{
                    //    if (pixels_x == 0)
                    //    {
                    //        moving_left = false;
                    //        MoveRight(elapsed_game_time);
                    //    }
                    //    else
                    //    {
                    //        MoveLeft(elapsed_game_time);
                    //    }
                    //}
                    //else
                    //{
                    //    if (pixels_x == x_max * grid_to_pixels)
                    //    {
                    //        moving_left = true;
                    //        MoveLeft(elapsed_game_time);
                    //    }
                    //    else
                    //    {
                    //        MoveRight(elapsed_game_time);
                    //    }
                    //} 
                    #endregion
               }
            }

            #region Find Path
            if (dstar.STARTED)
            {
                dstar_timer += (int)elapsed_game_time.Milliseconds;
                if (dstar_timer > 100)
                {
                    SenseDerbis();
                    Square move = dstar.Think(actual_cost_array);
                    if (move != null)
                    {
                        SetPosition(move.j, move.i);
                    }
                    dstar_timer = 0;
                }
            } 
            #endregion
        }

        private int GetRandomDirecion()
        {
            int direction = ran.Next(0, 4);
            int[] moves = new int[4] { -99999, -99999, -99999, -99999 };
            //int[] moves = new int[4];
            
            #region Old Method
            //currx = x;
            //for (int i = 0; i < currx; i++)
            //{
            //    if (currx > 0)
            //    {
            //        moves[0] += areaKnowledge[y, currx--];
            //    }
            //}

            //currx = x;

            //for (int i = currx; i > x_max; i--)
            //{
            //    if (currx < 31)
            //    {
            //        moves[1] += areaKnowledge[y, currx++];
            //    }
            //}

            //curry = y;

            //for (int i = 0; i < curry; i++)
            //{
            //    if (curry > 5)
            //    {
            //        moves[2] += areaKnowledge[curry--, x];
            //    }
            //}

            //curry = y;

            //for (int i = curry; i > y_max; i--)
            //{
            //    if (curry < 22)
            //    {
            //        moves[3] += areaKnowledge[curry++, x];
            //    }
            //}

            
            
            //int max = moves[direction];
            //for (int i = 0; i < 4; i++)
            //{
            //    if (moves[i] > max)
            //    {
            //        max = moves[i];
            //        direction = i;
            //    }
            //}
            #endregion

            for (int i = 1; i < 3; i++)
            {
                if ((x - i) > 0)
                {
                    if (moves[0] == -99999)
                    {
                        moves[0] = 0;
                    }
                    moves[0] += areaKnowledge[y, x - i];
                    areaKnowledge[y, x - i]--;
                }
                else
                {
                    moves[0] = -99999;
                    break;
                }
                 
            }

            for (int i = 1; i < 3; i++)
            {
                if ((x + i) < 32)
                {
                    if (moves[1] == -99999)
                    {
                        moves[1] = 0;
                    }
                    moves[1] += areaKnowledge[y, x + i];
                    areaKnowledge[y, x + i]--;
                }
                else
                {
                    moves[1] = -99999;
                    break;
                } 
            }

            for (int i = 1; i < 3; i++)
            {
                if ((y - i) > 5)
                {
                    if (moves[2] == -99999)
                    {
                        moves[2] = 0;
                    }
                    moves[2] += areaKnowledge[y - i, x];
                    areaKnowledge[y - i, x]--;
                }
                else
                {
                    moves[2] = -99999;
                    break;
                }
            }

            for (int i = 1; i < 3; i++)
            {
                if ((y + i) < 24)
                {
                    if (moves[3] == -99999)
                    {
                        moves[3] = 0;
                    }
                    moves[3] += areaKnowledge[y + i, x];
                    areaKnowledge[y + i, x]--;
                }
                else
                {
                    moves[3] = -99999;
                    break;
                }
            }

            int max = moves[direction];

            for (int j = 0; j < 4; j++)
            {
                if (moves[j] > max)
                {
                    max = moves[j];
                    direction = j;
                }
            }

            return direction;
        }

        private void Move(TimeSpan elapsed_game_time, int direction)
        {
            switch (direction)
            {
                case 0:
                    MoveLeft(elapsed_game_time);
                    break;

                case 1:
                    MoveRight(elapsed_game_time);
                    break;

                case 2:
                    MoveUp(elapsed_game_time);
                    break;

                case 3:
                    MoveDown(elapsed_game_time);
                    break;

            }
            areaKnowledge[y, x] -= 15;
        }

        private void SenseOil()
        {
       
            int xdiff, ydiff;
            foreach (Oil oil in Oil.oil_list)
            {
                xdiff = Math.Abs(x - oil.x);
                ydiff = Math.Abs(y - oil.y);

                if (xdiff <= maxOilRange && ydiff <= maxOilRange)
                {
                    //if (oil.x > minDepth && oil.x < maxDepth)
                    if(oil.x > minDepth)
                    {
                        //power.energy = power.energy - Math.Max(xdiff, ydiff);
                        power.energy = power.energy--;
                        Robot.sensedOil.Add(oil);
                        //areaKnowledge[oil.y, oil.x] = 500;
                    }
                }
            }

            //foreach (Oil oil in Robot.sensedOil)
            //{
            //    Oil.oil_list.Remove(oil);
            //}
        }

        private void SenseDerbis()
        {
            actual_cost_array = new int[24, 32];

            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    actual_cost_array[y, x] = 0;

                    foreach (Object o in Debris.derbislist)
                    {
                        if (o.pixels_x / 25 == x && o.pixels_y / 25 == y)
                        {
                            //Debris is in the way.
                            actual_cost_array[y, x] = 99;
                            break;
                        }
                    }
                }
            }
        }

        public override void Draw()
        {
            spriteBatch.Draw(texture, new Rectangle(pixels_x, pixels_y, texture.Width, texture.Height), Color.White);
        }

        private int GetDirection(int x, int y, int range)
        {
            int currx = x;
            int curry = y;
            int[] direction = new int[4] {0, 0, 0, 0 };
            int max,ret;

            for (int i = 0; i < range; i++)
            {
                if (currx > 0)
                {
                    direction[0] += areaKnowledge[y, currx];
                    areaKnowledge[y, currx] -= 1;
                    currx--;
                }
            }

            currx = x;
            for (int i = 0; i < range; i++)
            {
                if (currx < 31)
                {
                    direction[1] += areaKnowledge[y, currx];
                    areaKnowledge[y, currx] -= 1;
                    currx++;
                }

            }

            for (int i = 0; i < range; i++)
            {
                if (curry > 5)
                {
                    direction[2] += areaKnowledge[curry, x];
                    areaKnowledge[curry, x] -= 1;
                    curry--;
                }
            }

            curry = y;
            for (int i = 0; i < range; i++)
            {
                if (curry < 22)
                {
                    direction[3] += areaKnowledge[curry, x];
                    areaKnowledge[curry, x] -= 1;
                    curry++;
                }
            }
            ret = ran.Next(4);
            max = direction[ret];

            for (int i = 0; i < 4; i++)
            {
                if (direction[i] > max)
                {
                    max = direction[i];
                    ret = i;
                }
            }

                return ret;
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
        public static List<Debris> derbislist = new List<Debris>();

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
        //public int energy;
        public Texture2D power;
        public float energy;
        public float max_energy;
        
        public Powermeter(int x, int y, Texture2D texture, SpriteBatch spriteBatch, Texture2D power, int energy)
            : base(x, y, texture, spriteBatch)
        {
            this.energy = energy = 1.0f;
            type = ObjectType.POWERMETER;
            this.power = power;
        }

        public override void Think(TimeSpan elapsed_game_time)
        {

        }

        public override void Draw()
        {
            spriteBatch.Draw(texture, new Rectangle(pixels_x, y * grid_to_pixels, 100, 25), Color.White);
            spriteBatch.Draw(power, new Rectangle(pixels_x + 5, pixels_y + 4, (int)(energy * (100-12)), 15), Color.White);
        }
    }
}
