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

namespace WastedSea {
    enum GAMESTATE {
        SUBSYSTEM,
        OTHER
    }   

    /** State manager class. */
    class GameplayScreen : GameScreen {
       
        const int CawDuration = 9000;

        #region Fields

        int[] sub_params;                  //Stores inputs to the subsumption system.
        GAMESTATE game_state;               //The current game state for switch() in update.
        int dstar_timer;                    //Slows down D* to make it watchable.
        ContentManager content;
        SpriteBatch spriteBatch;
        SpriteFont Font1;                   //Used to draw all text on the screen.
        Vector2 FontPos;
        Map main_map;                       //Stores and draws static tile map.
        DStar dstar;
        Boat object_boat;
        Robot object_robot;
        LivesLeftBar object_lives_left_bar;
        //Powermeter object_powermeter;
        Game1 cur = new Game1();
        List<Object> dynamic_objects;
        public Texture2D boat, debris, oil, robot, bird, sub_system, sub_selector, powermeter, power1, power2, power3;
        Point sub_selector_loc;
        int selectorGlow = 1;
        int[,] actual_cost_array;           //Stores the sensed data to send to the D*.
        int redX = (11 * 25) + 9;
        int redY = (8 * 25) - 7;
        int which = 1;
        int energyValue;
        int minValue;
        int maxValue;
        int oilRangeValue;
        int score;        

        Random random = new Random();
        DateTime lastCaw = DateTime.Now;   //seagulls
        int nextCaw = -7000;
        bool failSoundPlayed;

        //Variables to keep track of key releases.
        bool SPACE_PRESSED = false;
        bool UP_PRESSED = false;
        bool DOWN_PRESSED = false;
        bool LEFT_PRESSED = false;
        bool RIGHT_PRESSED = false;

        // points for redX/Y
        Point[] redSquares = new Point[]{
            new Point(284,193),
            new Point(285,238),
            new Point(285,278),
            new Point(285,316)};

        #endregion

        #region Initialization

        /** Constructor. */
        public GameplayScreen() {
            TransitionOnTime = TimeSpan.FromSeconds(1.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);

            main_map = new Map(cur);
            dynamic_objects = new List<Object>();
            dstar_timer = 0;
            sub_params = new int[4];
            game_state = GAMESTATE.OTHER;
            actual_cost_array = new int[24, 32];
        }

        // Load graphics content for the game.
        public override void LoadContent() {

            if(content == null) {
                content = new ContentManager(ScreenManager.Game.Services, "Content");
            }

            Font1 = content.Load<SpriteFont>("SpriteFont1");
            FontPos = new Vector2(ScreenManager.GraphicsDevice.Viewport.Width / 2, ScreenManager.GraphicsDevice.Viewport.Height / 2);
            spriteBatch = ScreenManager.SpriteBatch;
            main_map.Initialize(ScreenManager.GraphicsDevice, content);
            boat = content.Load<Texture2D>(@"Boat");
            object_boat = new Boat(10, 4, boat, spriteBatch);
            debris = content.Load<Texture2D>(@"vDebris");
            oil = content.Load<Texture2D>(@"Oil");
            object_boat = new Boat(10, 4, boat, spriteBatch);
            dynamic_objects.Add(object_boat);

            sub_selector_loc = new Point(0, 0);
            sub_selector = content.Load<Texture2D>(@"selector");
            sub_system = content.Load<Texture2D>(@"agent_subsumption");

            Random ran_number = new Random();

            //Create all of the debris.
            int MAX_DEBRIS = 18;
            Debris new_debris;
            for(int i = 0; i < MAX_DEBRIS; i++) {
                int ran_x = ran_number.Next(0, 31);
                int ran_y = ran_number.Next(5, 7);
                new_debris = new Debris(ran_x, ran_y, debris, spriteBatch);
                dynamic_objects.Add(new_debris);
                Debris.derbislist.Add(new_debris);
            }

            bird = content.Load<Texture2D>(@"vBird");
            dynamic_objects.Add(new Bird(ran_number.Next(0, 31), ran_number.Next(1, 3), bird, spriteBatch));
            dynamic_objects.Add(new Bird(ran_number.Next(0, 31), ran_number.Next(1, 3), bird, spriteBatch));
            dynamic_objects.Add(new Bird(ran_number.Next(0, 31), ran_number.Next(1, 2), bird, spriteBatch));

            //Create all of the oil.
            int MAX_OIL = 30;
            Object new_oil;
            ran_number = new Random(1);
            for(int i = 0; i < MAX_OIL; i++) {
                int ran_x = ran_number.Next(0, 31);
                int ran_y = ran_number.Next(8, 22);
                new_oil = new Oil(ran_x, ran_y, oil, spriteBatch);
                dynamic_objects.Add(new_oil);
                Oil.oil_list.Add((Oil)new_oil);
            }

            //Create robot.
            robot = content.Load<Texture2D>(@"vRobot");
            object_robot = new Robot(35, 35, robot, spriteBatch);
            dynamic_objects.Add(object_robot);

            //Add powermeter
            powermeter = content.Load<Texture2D>(@"powermeter");
            power1 = content.Load<Texture2D>(@"power1");
            power2 = content.Load<Texture2D>(@"power2");
            power3 = content.Load<Texture2D>(@"power3");
            object_robot.power = new Powermeter(3, 0, powermeter, spriteBatch, power1,power2, power3);
            dynamic_objects.Add(object_robot.power);

            //Create the lives left bar.
            object_lives_left_bar = new LivesLeftBar(10, 0, robot, spriteBatch);
            dynamic_objects.Add(object_lives_left_bar);

            //Default values for the subsumption system.
            energyValue = 20;
            minValue = 2;
            maxValue = 17;
            oilRangeValue = 2;

            score = 0;

            Thread.Sleep(1000);
            ScreenManager.Game.ResetElapsedTime();
        }

        //Unload graphics content used by the game.
        public override void UnloadContent() {
            content.Unload();
        }

        #endregion

        #region Update and Draw

        /** The base update function. */
        public override void  Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen) {

            TimeSpan deltaCaw = DateTime.Now.Subtract(lastCaw);
            if ((int)deltaCaw.TotalMilliseconds >= (CawDuration + nextCaw))
            {
                this.SoundBank.PlayCue("seagull");
                lastCaw = DateTime.Now;

                nextCaw = random.Next(2000, 5000);
            }
            
            object_robot.minDepth = minValue;
            object_robot.maxDepth = maxValue;
            object_robot.maxOilRange = oilRangeValue;
            object_robot.boatx = object_boat.pixels_x;
            object_robot.boaty = object_boat.pixels_y;

            if(Robot.removeOil.Count > 0) {
                foreach(Oil oil in Robot.removeOil) {
                    dynamic_objects.Remove(oil);
                    score += 50;

                    this.SoundBank.PlayCue("clean_oil");
                }
                Robot.removeOil.Clear();
            }


            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            dstar_timer += gameTime.ElapsedGameTime.Milliseconds;

            if(IsActive) {
                KeyboardState ks = Keyboard.GetState();
                GamePadState gameControllerState = GamePad.GetState(PlayerIndex.One);

                switch(game_state) {
                    case GAMESTATE.OTHER: {
                            if(ks.IsKeyDown(Keys.Right) || gameControllerState.DPad.Right == ButtonState.Pressed) {         //Right arrow.
                                if(!object_robot.launched) {
                                    object_boat.MoveRight(gameTime.ElapsedGameTime);
                                }
                            }

                            if(ks.IsKeyDown(Keys.Left) || gameControllerState.DPad.Left == ButtonState.Pressed) {            //Left arrow.
                                if(!object_robot.launched) {
                                    object_boat.MoveLeft(gameTime.ElapsedGameTime);
                                }
                            }

                            if(ks.IsKeyDown(Keys.Space) || gameControllerState.Buttons.B == ButtonState.Pressed) {            //Space bar.
                                SPACE_PRESSED = true;
                            } else {
                                if(SPACE_PRESSED) {
                                    if(game_state == GAMESTATE.SUBSYSTEM) {
                                        this.SoundBank.PlayCue("menu_back");
                                        game_state = GAMESTATE.OTHER;
                                    } else {
                                        game_state = GAMESTATE.SUBSYSTEM;
                                    }
                                    SPACE_PRESSED = false;
                                }
                            }

                            //Launches the robot.
                            if(ks.IsKeyDown(Keys.Down) || gameControllerState.Buttons.A == ButtonState.Pressed) {
                                DOWN_PRESSED = true;
                            } else {
                                if(DOWN_PRESSED) {
                                    if(!object_robot.launched) {

                                        this.SoundBank.PlayCue("robot");
                                        
                                        object_robot.retenergy = (float)(energyValue);
                                        object_robot.power.Reset();
                                        object_lives_left_bar.UseLife();
                                        object_robot.Launch(object_boat.pixels_x / 25, object_boat.pixels_y / 25);
                                        failSoundPlayed = false;
                                    }
                                    DOWN_PRESSED = false;
                                }
                            }

                            //Allow our dynamic game objects their think cycle.
                            foreach (Object o in dynamic_objects)
                            {
                                o.Think(gameTime.ElapsedGameTime);
                            }

                            if (object_robot.power.energy <= 1.0 && !failSoundPlayed)
                            {
                                this.SoundBank.PlayCue("power_down");
                                failSoundPlayed = true;
                            }

                            break;
                        }

                    case GAMESTATE.SUBSYSTEM: {
                            if(ks.IsKeyDown(Keys.Down) || gameControllerState.DPad.Down == ButtonState.Pressed) {
                                UP_PRESSED = false;
                                DOWN_PRESSED = true;
                            } else {
                                //Move Selector to pick levels
                                if(DOWN_PRESSED) {

                                    this.SoundBank.PlayCue("menu_select");

                                    which++;
                                    if (which > 4)
                                        which = 1;

                                    redX = redSquares[which-1].X;
                                    redY = redSquares[which-1].Y;

                                    UP_PRESSED = false;
                                    DOWN_PRESSED = false;
                                }
                            }

                            if(ks.IsKeyDown(Keys.Up) || gameControllerState.DPad.Up == ButtonState.Pressed) {
                                DOWN_PRESSED = false;
                                UP_PRESSED = true;
                            } else {
                                //Move Selector to pick levels
                                if(UP_PRESSED) {

                                    this.SoundBank.PlayCue("menu_select");

                                    which--;
                                    if (which < 1)
                                        which = 4;

                                    redX = redSquares[which-1].X;
                                    redY = redSquares[which-1].Y;

                                    DOWN_PRESSED = false;
                                    UP_PRESSED = false;
                                }
                            }

                            //Adjust values
                            if(ks.IsKeyDown(Keys.Left) || gameControllerState.DPad.Left == ButtonState.Pressed) {
                                LEFT_PRESSED = true;
                            } else {
                                if(LEFT_PRESSED) {

                                    this.SoundBank.PlayCue("click");

                                    if(which == 1) //EnergyValue
                                    {
                                        if(energyValue > 0) {
                                            energyValue--;
                                        }
                                    } else if(which == 2) //Min Depth
                                    {
                                        if(minValue > 2) {
                                            minValue--;
                                        }
                                    } else if(which == 3) //Max Depth
                                    {
                                        if(maxValue > minValue) {
                                            maxValue--;
                                        }
                                    } else //Oil Range
                                    {
                                        if(oilRangeValue > 0) {
                                            oilRangeValue--;
                                        }
                                    }

                                    LEFT_PRESSED = false;
                                }
                            }

                            if(ks.IsKeyDown(Keys.Right) || gameControllerState.DPad.Right == ButtonState.Pressed) {
                                RIGHT_PRESSED = true;
                            } else {
                                if(RIGHT_PRESSED) {

                                    this.SoundBank.PlayCue("click");

                                    if(which == 1) //Energy Value
                                    {
                                        if (energyValue < 100)
                                        {
                                            energyValue++;
                                            object_robot.retenergy = (float)(energyValue);
                                        }                                            

                                    } else if(which == 2) //Min Value
                                    {
                                        if(minValue < 16 && minValue < maxValue) {
                                            minValue++;
                                        }
                                    } else if(which == 3) //Max Value
                                    {
                                        if(maxValue < 18) {
                                            maxValue++;
                                        }
                                    } else //Oil Range
                                    {
                                        if(oilRangeValue < 5) {
                                            oilRangeValue++;
                                        }
                                    }
                                }
                                RIGHT_PRESSED = false;
                            }


                            if(ks.IsKeyDown(Keys.Space) || gameControllerState.Buttons.B == ButtonState.Pressed) {
                                SPACE_PRESSED = true;
                            } else {
                                if(SPACE_PRESSED) {

                                    this.SoundBank.PlayCue("menu_back");

                                    game_state = GAMESTATE.OTHER;
                                    SPACE_PRESSED = false;
                                }
                            }

                            break;
                        }
                }
            }
        }

        /** Senses debris for the Dstar. */
        public void Sense() {
            for(int y = 0; y < 24; y++) {
                for(int x = 0; x < 32; x++) {
                    actual_cost_array[y, x] = 0;

                    foreach(Object o in dynamic_objects) {
                        if(o.type == ObjectType.DEBRIS) {
                            if(o.pixels_x / 25 == x && o.pixels_y / 25 == y) {
                                //Debris is in the way.
                                actual_cost_array[y, x] = 99;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /** Relays input to the game state manager. */
        public override void HandleInput(InputState input) {
            if(input == null)
                throw new ArgumentNullException("input");

            if(input.PauseGame) {
                // If they pressed pause, bring up the pause menu screen.
                ScreenManager.AddScreen(new PauseMenuScreen());
            } else {
            }
        }

        /** Base draw function. */
        public override void Draw(GameTime gameTime) {
            // Our player and enemy are both actually just text strings.                       
            main_map.Draw(spriteBatch);

            foreach(Object o in dynamic_objects) {
                spriteBatch.Begin();
                o.Draw();
                spriteBatch.End();
            }

            DrawString("Power:", 5, 0);
            DrawString("Score:    " + score.ToString(), 5, 25);


            // If the game is transitioning on or off, fade it out to black.
            if(TransitionPosition > 0) {
                ScreenManager.FadeBackBufferToBlack(255 - TransitionAlpha);
            }

            if(object_robot.dstar.STARTED) {
                //DrawCostGrid();
            }

            //if (object_robot.launched)
                //DrawCostGrid();

            if (game_state == GAMESTATE.SUBSYSTEM)
            {

                selectorGlow += 3;
                if (selectorGlow > 36)
                    selectorGlow = 0;


                spriteBatch.Begin();
                spriteBatch.Draw(sub_system, new Rectangle((6 * 25) + 0, (6 * 25) + 0, sub_system.Width, sub_system.Height), Color.White);
                spriteBatch.Draw(sub_selector, new Rectangle(redX, redY, sub_selector.Width, sub_selector.Height), Color.White);
                spriteBatch.Draw(sub_selector, new Rectangle(redX - selectorGlow / 9, redY - selectorGlow / 9, sub_selector.Width + selectorGlow / 9 * 2, sub_selector.Height + selectorGlow / 9 * 2), Color.White);
                spriteBatch.End();

                DrawString(energyValue.ToString(), 289, 195);
                DrawString(minValue.ToString(), 289, 240);
                DrawString(maxValue.ToString(), 289, 280);
                DrawString(oilRangeValue.ToString(), 289, 318);
            }

            if(object_robot.FAILED) {
                DrawString("You Failed!", 275, 75);
            }
        }

        /** Draws a string of text to the screen by location. */
        public void DrawString(string output, int x, int y) {
            spriteBatch.Begin();
            FontPos.X = x;
            FontPos.Y = y;
            //Vector2 FontOrigin = Font1.MeasureString(output) / 2;
            Vector2 FontOrigin = new Vector2(0, 0);
            spriteBatch.DrawString(Font1, output, FontPos, Color.Black, 0, FontOrigin, 1.0f, SpriteEffects.None, 0.5f);
            spriteBatch.End();
        }

        /** Draws the current actual cost matrix for the Dstar algorithm. */
        public void DrawCostGrid() {
            for(int y = 0; y < 24; y++) {
                for(int x = 0; x < 32; x++) {
                    int draw_x = object_robot.dstar.node_array[y, x].j;
                    int draw_y = object_robot.dstar.node_array[y, x].i;

                    //DrawString(Convert.ToString(object_robot.dstar.node_array[y, x].Gcost), draw_x * 25, draw_y * 25);
                    DrawString(Convert.ToString(object_robot.areaKnowledge[y, x]), draw_x * 25, draw_y * 25);
                }
            }
        }


        #endregion
    }
}
