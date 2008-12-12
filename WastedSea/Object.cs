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

namespace WastedSea {
    public enum ObjectType {
        DEBRIS,
        OIL,
        BOAT,
        BIRD,
        ROBOT,
        POWERMETER
    }

    public enum Direction {
        UP,
        DOWN,
        LEFT,
        RIGHT
    }

    /** The base class for all game objects that go doubleo the object lists. */
    public class Object {
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

        public Object(int x, int y, Texture2D texture, SpriteBatch spriteBatch) {
            this.texture = texture;
            this.x = x;
            this.y = y;
            this.spriteBatch = spriteBatch;
            pixels_x = x * grid_to_pixels;                  //Starting pixel locations of object.
            pixels_y = y * grid_to_pixels;                  //Starting pixel locations of object.
            time = 0;
            ran = new Random(x * y);
        }

        public void SetPosition(int x, int y) {
            this.x = x;
            this.y = y;
            pixels_x = Pixels(x);
            pixels_y = Pixels(y);
        }

        /** Bounds check on an index. */
        public int LegalX(int num) {
            num = Math.Max(num, 0);
            num = Math.Min(num, x_max);
            return num;
        }

        /** Bounds check on an index. */
        public int LegalY(int num) {
            num = Math.Max(num, 0);
            num = Math.Min(num, y_max);
            return num;
        }

        /** Converts grid coordinate to pixel coordinate. */
        public int Pixels(int grid) {
            return grid * 25;
        }

        /** Converts pixel coordinate to grid coordinate. */
        public int Grid(int pixels) {
            return pixels / 25;
        }

        /** Draw this object. */
        public virtual void Draw() {
            //spriteBatch.Draw(texture, new Rectangle(x * grid_to_pixels, y * grid_to_pixels, texture.Width, texture.Height), Color.White);
            spriteBatch.Draw(texture, new Rectangle(pixels_x, pixels_y, texture.Width, texture.Height), Color.White);
        }

        public void MoveUp(TimeSpan elapsed_game_time) {
            time += (elapsed_game_time.Milliseconds + speed);

            pixels_y -= time / grid_to_pixels;
            time = time % grid_to_pixels;

            if(pixels_y > y_max * grid_to_pixels) {
                pixels_y = y_max * grid_to_pixels;
            }

            y = (int)Math.Floor((double)(pixels_y / 25));
        }

        public void MoveDown(TimeSpan elapsed_game_time) {

            time += (elapsed_game_time.Milliseconds + speed);

            pixels_y += time / grid_to_pixels;
            time = time % grid_to_pixels;

            if(pixels_y > y_max * grid_to_pixels) {
                pixels_y = y_max * grid_to_pixels;
            }

            y = (int)Math.Floor((double)(pixels_y / 25));
        }

        public void MoveLeft(TimeSpan elapsed_game_time) {
            time += (elapsed_game_time.Milliseconds + speed);

            pixels_x -= time / grid_to_pixels;
            time = time % grid_to_pixels;

            if(pixels_x < 0) {
                pixels_x = 0;
            }

            x = (int)Math.Floor((double)(pixels_x / 25));
        }

        public void MoveRight(TimeSpan elapsed_game_time) {
            time += (elapsed_game_time.Milliseconds + speed);

            pixels_x += time / grid_to_pixels;
            time = time % grid_to_pixels;

            if(pixels_x > x_max * grid_to_pixels) {
                pixels_x = x_max * grid_to_pixels;
            }

            x = (int)Math.Floor((double)(pixels_x / 25));
        }

        public virtual void Think(TimeSpan elapsed_game_time) { }
    }

    /** The moveable automated robot. */
    public class Robot : Object {
        #region Variables
        public bool launched;
        public int timeSinceLaunched;
        public Powermeter power;
        public int minDepth;
        public int maxDepth;
        public int maxOilRange;
        public int depth;
        public static List<Oil> sensedOil = new List<Oil>();
        public static List<Oil> removeOil = new List<Oil>();
        public float retenergy;
        public DStar dstar;
        int[,] actual_cost_array;
        public int[,] areaKnowledge;
        public int[,] step;
        public int boatx, boaty;
        int dstar_timer;                                    //Stores time since last D* update request.
        int dstar_interval;                                 //How often to request a move from D* (interpolate inbetween).
        Square last_dstar_move;                             //Stores the last move received from D*.
        int prev_direc;
        int score;                                          //Stores scored points for the main update to add to the score each cycle.
        float power_to_move;

        #endregion

        public Robot(int x, int y, Texture2D texture, SpriteBatch spriteBatch)
            : base(x, y, texture, spriteBatch) {
            type = ObjectType.ROBOT;
            launched = false;
            speed = 100;
            dstar = new DStar();
            dstar_interval = 100;
            dstar_timer = -1;
            score = 0;
            //power_to_move = 0.25f;
            power_to_move = 0.50f;
        }

        /** Laund the robot. */
        public void Launch(int x, int y) {
            this.x = x;
            this.y = y + 1;
            pixels_x = x * grid_to_pixels;
            pixels_y = y * grid_to_pixels;
            launched = true;
            timeSinceLaunched = 0;
            areaKnowledge = new int[24, 32];
            prev_direc = -1;
            step = new int[24, 32];

            for(int i = 0; i < 24; i++) {
                for(int j = 0; j < 32; j++) {
                    areaKnowledge[i, j] = -99999;
                }
            }

            for(int i = LegalY(minDepth + 5); i < LegalY(maxDepth + 5); i++) {
                for(int j = 0; j < 32; j++) {
                    areaKnowledge[i, j] = 1;
                }
            }

        }

        /** Retrieves the current score points. */
        public int GetScore() {
            int v = score;
            score = 0;
            return v;
        }

        /** Return home. */
        public void Retun() {
            launched = false;
        }

        /** Subsumption architecture. */
        public override void Think(TimeSpan elapsed_game_time) {
            depth = y - 4;

            if(launched) {
                if(retenergy > power.energy) {
                    actual_cost_array = new int[24, 32];
                    dstar = new DStar();
                    dstar.Start(pixels_x / 25, pixels_y / 25, (int)Math.Floor((double)boatx / 25), (int)Math.Floor((double)boaty / 25));
                    Retun();
                }
                //This would be changed to the oil value presumably
                else if(depth < minDepth){
                    power.energy -= 0.0025f;
                    MoveDown(elapsed_game_time);
                }
                //This would be changed to the oil value presumably
                else if(depth > maxDepth){
                    power.energy -= 0.0025f;
                    MoveUp(elapsed_game_time);
                }
                //Tells how close agent is to oil
                else if(Robot.sensedOil.Count > 0){
                    Oil oil = Robot.sensedOil[0];

                    if(x == oil.x && oil.y == y) {
                        Robot.removeOil.Add(oil);
                        Robot.sensedOil.Remove(oil);
                        Oil.oil_list.Remove(oil);
                    } else {

                        if(oil.x > x)
                            Move(elapsed_game_time, 1);
                        else if(oil.x < x)
                            Move(elapsed_game_time, 0);
                        else if(oil.y > y)
                            Move(elapsed_game_time, 3);
                        else if(oil.y < y)
                            Move(elapsed_game_time, 2);
                    } 
                }else{
                    SenseOil();
                    int direction = GetRandomDirecion();

                    Random ran = new Random();
                    if(prev_direc == direction) {
                        direction = ran.Next(0, 8);
                    }

                    Move(elapsed_game_time, direction);
                    prev_direc = direction;
                }
            }

            if(dstar.STARTED) {
                dstar_timer += (int)elapsed_game_time.Milliseconds;

                //If enough time has passed, get next move from D*.
                if(dstar_timer > dstar_interval || dstar_timer == -1) {
                    power.Reset();
                    SenseDerbis();
                    Square move = dstar.Think(actual_cost_array);
                    if(move != null) {
                        SetPosition(move.j, move.i);
                        //last_dstar_move = move;
                    }
                    dstar_timer = 0;
                }
                //Else interpolate the D* move to the pixel level from the grid level.
                else {
                    if(last_dstar_move != null) {
                        int x1 = (pixels_x);
                        int y1 = (pixels_y);
                        int x2 = last_dstar_move.j * grid_to_pixels;
                        int y2 = last_dstar_move.i * grid_to_pixels;

                        int dx = x2 - x1;
                        int dy = y2 - y1;

                        float interval = dstar_timer / dstar_interval;

                        pixels_x = (int)(x1 + (interval * dx));
                        pixels_y = (int)(y1 + (interval * dy));

                        x = pixels_x / grid_to_pixels;
                        y = pixels_y / grid_to_pixels;
                    }

                    //SetPosition(move.j, move.i);
                }
            }
        }

        // Get a random direction to move
        private int GetRandomDirecion() {
            int direction = ran.Next(0, 8);
            int[] moves = new int[8] { -99999, -99999, -99999, -99999, -99999, -99999, -99999, -99999 };

            int i = 1;

            if((x - i) > 0) {
                if(moves[0] == -99999) {
                    moves[0] = 0;
                }
                moves[0] += areaKnowledge[y, x - i] * step[y, x - i];
                areaKnowledge[y, x - i] -= 5;
            } else {
                moves[0] = -99999;
            }

            if((x + i) < 32) {
                if(moves[1] == -99999) {
                    moves[1] = 0;
                }
                moves[1] += areaKnowledge[y, x + i] * step[y, x + i];
                areaKnowledge[y, x + i] -= 5;
            } else {
                moves[1] = -99999;
            }

            if((y - i) > minDepth + 5) {
                if(moves[2] == -99999) {
                    moves[2] = 0;
                }
                moves[2] += areaKnowledge[y - i, x] * step[y - i, x];
                areaKnowledge[y - i, x] -= 5;
            } else {
                moves[2] = -99999;
            }
            


            if((y + i) < 24) {
                if(moves[3] == -99999) {
                    moves[3] = 0;
                }
                moves[3] += areaKnowledge[y + i, x] * step[y + i, x];
                areaKnowledge[y + i, x] -= 5;
            } else {
                moves[3] = -99999;
            }
         

            if((x + 1) < 32) {
                if(y + 1 < 24) {
                    if(moves[7] == -99999) {
                        moves[7] = 0;
                    }
                    moves[7] += areaKnowledge[y + 1, x + 1] * step[y + 1, x + 1];
                    areaKnowledge[y + 1, x] -= 5;
                }

                if(y - 1 > minDepth + 5) {
                    if(moves[6] == -99999) {
                        moves[6] = 0;
                    }
                    moves[6] += areaKnowledge[y - 1, x + 1] * step[y - 1, x + 1];
                    areaKnowledge[y - 1, x] -= 5;
                }
            }

            if((x - 1) > 0) {
                if(y + 1 < 24) {
                    if(moves[5] == -99999) {
                        moves[5] = 0;
                    }
                    moves[5] += areaKnowledge[y + 1, x - 1] * step[y + 1, x - 1];
                    areaKnowledge[y + 1, x] -= 5;
                }

                if(y - 1 > minDepth + 5) {
                    if(moves[4] == -99999) {
                        moves[4] = 0;
                    }
                    moves[4] += areaKnowledge[y - 1, x - 1] * step[y - 1, x - 1];
                    areaKnowledge[y - 1, x] -= 5;
                }
            }

            int max = moves[direction];

            for(int j = 0; j < 4; j++) {
                if(moves[j] > max) {
                    max = moves[j];
                    direction = j;
                }
            }

            return direction;
        }

        /** Moves int the given direction. */
        private void Move(TimeSpan elapsed_game_time, int direction) {
            switch(direction) {
                case 0:
                    MoveLeft(elapsed_game_time);
                    areaKnowledge[y, x] -= 15;
                    step[y, x] += 2;
                    break;

                case 1:
                    MoveRight(elapsed_game_time);
                    areaKnowledge[y, x] -= 15;
                    step[y, x] += 2;
                    break;

                case 2:
                    MoveUp(elapsed_game_time);
                    areaKnowledge[y, x] -= 15;
                    step[y, x] += 2;
                    break;

                case 3:
                    MoveDown(elapsed_game_time);
                    areaKnowledge[y, x] -= 15;
                    step[y, x] += 2;
                    break;

                case 4:
                    MoveLeft(elapsed_game_time);
                    areaKnowledge[y, x] -= 15;
                    step[y, x] += 2;

                    MoveUp(elapsed_game_time);
                    areaKnowledge[y, x] -= 15;
                    step[y, x] += 2;
                    break;

                case 5:
                    MoveLeft(elapsed_game_time);
                    areaKnowledge[y, x] -= 15;
                    step[y, x] += 2;

                    MoveDown(elapsed_game_time);
                    areaKnowledge[y, x] -= 15;
                    step[y, x] += 2;
                    break;

                case 6:
                    MoveRight(elapsed_game_time);
                    areaKnowledge[y, x] -= 15;
                    step[y, x] += 2;

                    MoveDown(elapsed_game_time);
                    areaKnowledge[y, x] -= 15;
                    step[y, x] += 2;
                    break;

                case 7:
                    MoveRight(elapsed_game_time);
                    areaKnowledge[y, x] -= 15;
                    step[y, x] += 2;

                    MoveUp(elapsed_game_time);
                    areaKnowledge[y, x] -= 15;
                    step[y, x] += 2;
                    break;
            }
        }

        // Tells if any oil is in the range
        private void SenseOil() {
            float cost_to_sense = 0.05f;

            int xdiff, ydiff;
            foreach(Oil oil in Oil.oil_list) {
                xdiff = Math.Abs(x - oil.x);
                ydiff = Math.Abs(y - oil.y);

                if(xdiff < maxOilRange && ydiff < maxOilRange) {
                    if(oil.y > minDepth + 5 && oil.y < maxDepth + 5) {
                        power.Consume(cost_to_sense);
                        Robot.sensedOil.Add(oil);
                    }
                }
            }
        }

        // Senses the derbis in the way
        private void SenseDerbis() {
            int sensor_distance = 5;

            for(int j = LegalX(x - sensor_distance); j < LegalX(x + sensor_distance); j++) {
                for(int i = LegalY(y - sensor_distance); i < LegalY(y + sensor_distance); i++) {
                    foreach(Object o in Debris.derbislist) {
                        if(o.pixels_x / 25 == j && o.pixels_y / 25 == i) {
                            actual_cost_array[i, j] = 99;
                            break;
                        }
                    }
                }
            }
        }

        /** Draws this object. */
        public override void Draw() {
            spriteBatch.Draw(texture, new Rectangle(pixels_x, pixels_y, texture.Width, texture.Height), Color.White);
        }

        // Get the direction to move
        private int GetDirection(int x, int y, int range) {
            int currx = x;
            int curry = y;
            int[] direction = new int[4] { 0, 0, 0, 0 };
            int max, ret;

            for(int i = 0; i < range; i++) {
                if(currx > 0) {
                    direction[0] += areaKnowledge[y, currx];
                    areaKnowledge[y, currx] -= 1;
                    currx--;
                }
            }

            currx = x;
            for(int i = 0; i < range; i++) {
                if(currx < 31) {
                    direction[1] += areaKnowledge[y, currx];
                    areaKnowledge[y, currx] -= 1;
                    currx++;
                }

            }

            for(int i = 0; i < range; i++) {
                if(curry > 5) {
                    direction[2] += areaKnowledge[curry, x];
                    areaKnowledge[curry, x] -= 1;
                    curry--;
                }
            }

            curry = y;
            for(int i = 0; i < range; i++) {
                if(curry < 22) {
                    direction[3] += areaKnowledge[curry, x];
                    areaKnowledge[curry, x] -= 1;
                    curry++;
                }
            }
            ret = ran.Next(4);
            max = direction[ret];

            for(int i = 0; i < 4; i++) {
                if(direction[i] > max) {
                    max = direction[i];
                    ret = i;
                }
            }

            return ret;
        }

        public void MoveUp(TimeSpan elapsed_game_time) {
            if(power.energy >= power_to_move){
                power.Consume(power_to_move);

                time += (elapsed_game_time.Milliseconds + speed);

                pixels_y -= time / grid_to_pixels;
                time = time % grid_to_pixels;

                if(pixels_y > y_max * grid_to_pixels) {
                    pixels_y = y_max * grid_to_pixels;
                }

                y = (int)Math.Floor((double)(pixels_y / 25));
            }
        }

        public void MoveDown(TimeSpan elapsed_game_time) {
            if(power.energy >= power_to_move) {
                power.Consume(power_to_move);
                time += (elapsed_game_time.Milliseconds + speed);

                pixels_y += time / grid_to_pixels;
                time = time % grid_to_pixels;

                if(pixels_y > y_max * grid_to_pixels) {
                    pixels_y = y_max * grid_to_pixels;
                }

                y = (int)Math.Floor((double)(pixels_y / 25));
            }
        }

        public void MoveLeft(TimeSpan elapsed_game_time) {
            if(power.energy >= power_to_move) {
                power.Consume(power_to_move);
                time += (elapsed_game_time.Milliseconds + speed);

                pixels_x -= time / grid_to_pixels;
                time = time % grid_to_pixels;

                if(pixels_x < 0) {
                    pixels_x = 0;
                }

                x = (int)Math.Floor((double)(pixels_x / 25));
            }
        }

        public void MoveRight(TimeSpan elapsed_game_time) {
            if(power.energy >= power_to_move) {
                power.Consume(power_to_move);
                time += (elapsed_game_time.Milliseconds + speed);

                pixels_x += time / grid_to_pixels;
                time = time % grid_to_pixels;

                if(pixels_x > x_max * grid_to_pixels) {
                    pixels_x = x_max * grid_to_pixels;
                }

                x = (int)Math.Floor((double)(pixels_x / 25));
            }
        }
    }

    /** The moveable player-controlled boat. */
    public class Boat : Object {
        public Boat(int x, int y, Texture2D texture, SpriteBatch spriteBatch)
            : base(x, y, texture, spriteBatch) {
            type = ObjectType.BOAT;
            speed = 2;
        }

        public override void Draw() {
            spriteBatch.Draw(texture, new Rectangle(pixels_x, y * grid_to_pixels, texture.Width, texture.Height), Color.White);
        }
    }

    /** The flying bird object. */
    public class Bird : Object {
        public Bird(int x, int y, Texture2D texture, SpriteBatch spriteBatch)
            : base(x, y, texture, spriteBatch) {
            type = ObjectType.BIRD;
            speed = -3;
            current_dir = Direction.UP;
        }

        public override void Think(TimeSpan elapsed_game_time) {
            time += (elapsed_game_time.Milliseconds + speed);
            pixels_x -= time / grid_to_pixels;
            time = time % grid_to_pixels;

            if(pixels_x < 0) {
                pixels_x = x_max * grid_to_pixels;
            }
        }

        public override void Draw() {
            spriteBatch.Draw(texture, new Rectangle(pixels_x, pixels_y, texture.Width, texture.Height), Color.White);
        }
    }

    /** Floating debris that require our agent to use D* on the way home. */
    public class Debris : Object {
        public static List<Debris> derbislist = new List<Debris>();

        public Debris(int x, int y, Texture2D texture, SpriteBatch spriteBatch)
            : base(x, y, texture, spriteBatch) {
            type = ObjectType.DEBRIS;
            speed = ran.Next(1, 10);
        }

        //Causes the debri to float right to left and reset on the right side of the screen.
        public override void Think(TimeSpan elapsed_game_time) {
            time += (elapsed_game_time.Milliseconds + speed);

            pixels_x -= time / grid_to_pixels;
            time = time % grid_to_pixels;

            if(pixels_x < 0) {
                pixels_x = x_max * grid_to_pixels;
            }
        }

        public override void Draw() {
            spriteBatch.Draw(texture, new Rectangle(pixels_x, y * grid_to_pixels, texture.Width, texture.Height), Color.White);
        }
    }

    /** Floating debris that require our agent to use D* on the way home. */
    public class LivesLeftBar : Object {
        int lives_left;

        public LivesLeftBar(int x, int y, Texture2D texture, SpriteBatch spriteBatch)
            : base(x, y, texture, spriteBatch) {
            type = ObjectType.DEBRIS;
            lives_left = 3;
        }

        /** No thought. */
        public override void Think(TimeSpan elapsed_game_time) {}

        /** Draw the 0 to 3 lives in the life bar. */
        public override void Draw() {
            for(int i = 0; i < lives_left; i++) {
                spriteBatch.Draw(texture, new Rectangle(pixels_x + (i * texture.Width / 2 + 2) - 5, pixels_y + 5, texture.Width / 2, texture.Height / 2), Color.White);
            }
        }

        public void UseLife() {
            if(lives_left > 0) {
                lives_left -= 1;
            }
        }

        public void Reset() {
            lives_left = 3;  
        }
    }

    /** Oil the floating oil. */
    public class Oil : Object {
        static public List<Oil> oil_list = new List<Oil>();

        public Oil(int x, int y, Texture2D texture, SpriteBatch spriteBatch)
            : base(x, y, texture, spriteBatch) {
            type = ObjectType.OIL;
        }

        //Causes the debri to float right to left and reset on the right side of the screen.
        public override void Think(TimeSpan elapsed_game_time) {

        }

        public override void Draw() {
            spriteBatch.Draw(texture, new Rectangle(pixels_x, y * grid_to_pixels, texture.Width, texture.Height), Color.White);
        }
    }

    /** Shows the current power of the robot. */
    public class Powermeter : Object {
        public Texture2D current, power1,power2,power3;                         //Graphic to display for the power bar.
        public float energy;                            //The current energy displayed available for consumption.
        public float max_energy;                        //Max possible energy.

        /** Overrides default constructor to set default energy setting. */
        public Powermeter(int x, int y, Texture2D texture, SpriteBatch spriteBatch, Texture2D power1, Texture2D power2, Texture2D power3)
            : base(x, y, texture, spriteBatch) {
            
            type = ObjectType.POWERMETER;
            this.power1 = power1;
            this.power2 = power2;
            this.power3 = power3;
            this.energy = 100.0f;
            this.max_energy = 100.0f;
            this.current = power1;
        }

        public override void Think(TimeSpan elapsed_game_time) {
            //The power bar does not think.
        }

        /** Draws the power sprites on the interface. */
        public override void Draw() {
            if(energy < 33) {
                current = power3;
            }else if(energy < 66) {
                current = power2;
            } else {
                current = power1;
            }

            spriteBatch.Draw(texture, new Rectangle(pixels_x, y * grid_to_pixels, 190, 25), Color.White);
            spriteBatch.Draw(current, new Rectangle(pixels_x + 5, pixels_y + 4, (int)((energy/max_energy) * (190 - 14)), 15), Color.White);
        }

        /** Called when an action takes place which requires energy. */ 
        public void Consume(float ammount) {
            if(ammount > 0.0f && ammount <= max_energy) {
                //energy -= (percent * max_energy);
                energy -= ammount;
            }
        }

        /** Resets the power bar (called after a round ends). */
        public void Reset(){
            energy = max_energy;
        }
    }
}
