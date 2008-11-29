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
    enum GAMESTATE
    {
        SUBSYSTEM,
        OTHER
    }
    /// <summary>
    /// This screen implements the actual game logic. It is just a
    /// placeholder to get the idea across: you'll probably want to
    /// put some more interesting gameplay in here!
    /// </summary>
    class GameplayScreen : GameScreen
    {
        #region Fields

        int [] sub_params;
        GAMESTATE game_state;
        int dstar_timer;
        ContentManager content;
        SpriteBatch spriteBatch;
        SpriteFont Font1;
        Vector2 FontPos;
        Map main_map;
        DStar dstar;
        Boat object_boat;
        Robot object_robot;
        //Powermeter object_powermeter;
        Game1 cur = new Game1();
        List<Object> dynamic_objects;
        List<Object> button_objects;
        public Texture2D boat, debris, oil, robot, bird,sub_system, sub_selector,powermeter,power;
        Point sub_selector_loc;
        int[,] actual_cost_array;                           //Stores the sensed data to send to the D*.
        int redX = (11 * 25) + 9;
        int redY = (8 * 25) - 7;
        int which = 2;
        int energyValue = 0;
        int minValue = 0;
        int maxValue = 0;
        int oilRangeValue = 0;
       
        Random random = new Random();

        //Variables to keep track of key releases.
        bool SPACE_PRESSED = false;
        bool UP_PRESSED = false;
        bool DOWN_PRESSED = false;
        bool LEFT_PRESSED = false;
        bool RIGHT_PRESSED = false;

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
            button_objects = new List<Object>();
            dstar_timer = 0;
            sub_params = new int[4];
            game_state = GAMESTATE.OTHER;
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

            sub_selector_loc = new Point(0, 0);
            sub_selector = content.Load<Texture2D>(@"selector");
            sub_system = content.Load<Texture2D>(@"agent_subsumption");
            
            Random ran_number = new Random();

            //Create all of the debris.
            int MAX_DEBRIS = 200;
            Debris new_debris;
            for (int i = 0; i < MAX_DEBRIS; i++)
            {
                int ran_x = ran_number.Next(0, 31);
                int ran_y = ran_number.Next(5, 7);
                new_debris = new Debris(ran_x, ran_y, debris, spriteBatch);
                dynamic_objects.Add(new_debris);
                Debris.derbislist.Add(new_debris);
            }

            bird = content.Load<Texture2D>(@"vBird");
            dynamic_objects.Add(new Bird(ran_number.Next(0, 31), ran_number.Next(0, 2), bird, spriteBatch));
            dynamic_objects.Add(new Bird(ran_number.Next(0, 31), ran_number.Next(0, 2), bird, spriteBatch));
            dynamic_objects.Add(new Bird(ran_number.Next(0, 31), ran_number.Next(0, 2), bird, spriteBatch));

            //Create all of the oil.
            int MAX_OIL = 30;
            Object new_oil;
            for (int i = 0; i < MAX_OIL; i++)
            {
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
            power = content.Load<Texture2D>(@"power");
            object_robot.power = new Powermeter(3, 0, powermeter, spriteBatch, power, energyValue);
            dynamic_objects.Add(object_robot.power);

            dstar = new DStar();
            Thread.Sleep(1000);
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
            if (object_robot.launched)
                object_robot.timeSinceLaunched++;

            if (object_robot.timeSinceLaunched % 120 == 0 && object_robot.launched)
                energyValue--;

            object_robot.power.energy = energyValue;
            object_robot.minDepth = minValue;
            object_robot.maxDepth = maxValue;
            object_robot.maxOilRange = oilRangeValue;
            object_robot.boatx = object_boat.pixels_x;
            object_robot.boaty = object_boat.pixels_y;

            foreach (Oil oil in Robot.removeOil)
            {
                dynamic_objects.Remove(oil);
            }

            Robot.sensedOil.Clear();
            Robot.removeOil.Clear();

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            dstar_timer += gameTime.ElapsedGameTime.Milliseconds;

            if (IsActive)
            {
                KeyboardState ks = Keyboard.GetState();

                switch (game_state)
                {
                    case GAMESTATE.OTHER:
                        {
                            if (ks.IsKeyDown(Keys.Right))       //Right arrow.
                            {
                                object_boat.MoveRight(gameTime.ElapsedGameTime);
                            }

                            if (ks.IsKeyDown(Keys.Left))            //Left arrow.
                            {
                                object_boat.MoveLeft(gameTime.ElapsedGameTime);
                            }

                            if (ks.IsKeyDown(Keys.Space))            //Space bar.
                            {
                                SPACE_PRESSED = true;
                            }
                            else
                            {
                                if (SPACE_PRESSED)
                                {
                                    if (game_state == GAMESTATE.SUBSYSTEM)
                                    {
                                        game_state = GAMESTATE.OTHER;
                                    }
                                    else
                                    {
                                        game_state = GAMESTATE.SUBSYSTEM;
                                    }
                                    SPACE_PRESSED = false;
                                }
                            }

                            if (ks.IsKeyDown(Keys.Up))            //Space bar.
                            {
                                UP_PRESSED = true;
                            }
                            else
                            {
                                if (UP_PRESSED)
                                {
                                    if (!dstar.STARTED && object_robot.launched)
                                    {
                                        object_robot.Retun();
                                        //Run D* and bring robot home.
                                        dstar.Start(object_robot.pixels_x / 25, object_robot.pixels_y / 25, object_boat.pixels_x / 25, object_boat.pixels_y / 25);
                                    }
                                    UP_PRESSED = false;
                                }
                            }

                            if (ks.IsKeyDown(Keys.Down))            //Space bar.
                            {
                                DOWN_PRESSED = true;
                            }
                            else
                            {
                                if (DOWN_PRESSED)
                                {
                                    object_robot.Launch(object_boat.pixels_x / 25, object_boat.pixels_y / 25);
                                    DOWN_PRESSED = false;
                                }
                            }

                            if (dstar.STARTED)
                            {
                                if (dstar_timer > 200)
                                {
                                    Sense();
                                    Square move = dstar.Think(actual_cost_array);
                                    if (move != null)
                                    {
                                        object_robot.SetPosition(move.j, move.i);
                                    }

                                    dstar_timer = 0;
                                }
                            }



                            //Allow our dynamic game objects their think cycle.
                            foreach (Object o in dynamic_objects)
                            {
                                o.Think(gameTime.ElapsedGameTime);
                            }

                            break;
                        }

                    case GAMESTATE.SUBSYSTEM:
                        {
                            if (ks.IsKeyDown(Keys.Down))
                            {
                                DOWN_PRESSED = true;
                            }
                            else
                            {
                                //Move Selector to pick levels
                                if (DOWN_PRESSED)
                                {
                                    if (which == 2)
                                    {
                                        redY += 45;
                                        redX += 1;                                        
                                        which++;
                                    }
                                    else if (which == 3)
                                    {
                                        redY += 40;
                                        which++;
                                    }
                                    else if (which == 4)
                                    {
                                        redY += 38;
                                        which = 1;
                                    }
                                    else
                                    {
                                        redY = (8 * 25) - 7;
                                        redX -=1;
                                        which++;
                                    }
                                    DOWN_PRESSED = false;
                                }
                            }

                            //Adjust values
                            if (ks.IsKeyDown(Keys.Left))
                            {
                                LEFT_PRESSED = true;
                            }
                            else
                            {
                                if (LEFT_PRESSED)
                                {
                                    if (which == 2) //EnergyValue
                                    {
                                        if (energyValue > 0)
                                        {
                                            energyValue--;
                                        }
                                    }
                                    else if (which == 3) //Min Depth
                                    {
                                        if (minValue > 0)
                                        {
                                            minValue--;
                                        }
                                    }
                                    else if (which == 4) //Max Depth
                                    {
                                        if (maxValue > 0)
                                        {
                                            maxValue--;
                                        }
                                    }
                                    else //Oil Range
                                    {
                                        if (oilRangeValue > 0)
                                        {
                                            oilRangeValue--;
                                        }
                                    }

                                    LEFT_PRESSED = false;
                                }
                            }

                            if (ks.IsKeyDown(Keys.Right))
                            {
                                RIGHT_PRESSED = true;
                            }
                            else
                            {
                                if (RIGHT_PRESSED)
                                {
                                    if (which == 2) //Energy Value
                                    {
                                        if (energyValue < 10)
                                        {
                                            energyValue++;
                                        }
                                    }
                                    else if (which == 3) //Min Value
                                    {
                                        if (minValue < 10)
                                        {
                                            minValue++;
                                        }
                                    }
                                    else if (which == 4) //Max Value
                                    {
                                        if (maxValue < 10)
                                        {
                                            maxValue++;
                                        }
                                    }
                                    else //Oil Range
                                    {
                                        if (oilRangeValue < 10)
                                        {
                                            oilRangeValue++;
                                        }
                                    }
                                }
                                RIGHT_PRESSED = false;
                            }
                            
                            
                            if(ks.IsKeyDown(Keys.Space))
                            {
                                SPACE_PRESSED = true;
                            }else
                            {
                                if (SPACE_PRESSED)
                                {
                                    game_state = GAMESTATE.OTHER;
                                    SPACE_PRESSED = false;
                                }
                            }

                            //Subsumption Architecture hurr...
                            if (energyValue < 4)
                            {
                                //RETURNTOBOAT!
                            }
                            else if (minValue > 3)//This would be changed to the oil value presumably
                            {
                                object_robot.MoveDown(gameTime.ElapsedGameTime);
                            }
                            else if (maxValue < 100)//This would be changed to the oil value presumably
                            {
                                //RISE
                            }
                            else if (oilRangeValue < 3)//Tells how close agent is to oil
                            {
                                //CLEANOIL
                            }
                            else
                            {
                                //WALK_RANDOMLY
                            }

                            break;
                        }
                }
            }

        }

        public void Sense()
        {
            actual_cost_array = new int[24, 32];

            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    actual_cost_array[y, x] = 0;

                    foreach (Object o in dynamic_objects)
                    {
                        if (o.type == ObjectType.DEBRIS)
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

            foreach (Object o in button_objects)
            {
                spriteBatch.Begin();
                o.Draw();
                spriteBatch.End();
            }

            DrawString("Score: 0", 0, 0);
            DrawString(redX.ToString(), 12, 12);
            //DrawString(Mouse.GetState().ToString(), 0, 0);
            //DrawString(object_buttonl.x.ToString(), 0, 20);
            //DrawString(object_buttonl.y.ToString(), 0, 30);
            
            // If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition > 0)
                ScreenManager.FadeBackBufferToBlack(255 - TransitionAlpha);

            if (dstar.STARTED)
            {
                DrawCostGrid();
            }

            if (game_state == GAMESTATE.SUBSYSTEM)
            {
                
                spriteBatch.Begin();
                spriteBatch.Draw(sub_system, new Rectangle((6 * 25) + 0, (6 * 25) + 0, sub_system.Width, sub_system.Height), Color.White);
                spriteBatch.Draw(sub_selector, new Rectangle(redX, redY, sub_selector.Width, sub_selector.Height), Color.White);
                spriteBatch.End();

                DrawString(energyValue.ToString(), 289,195);
                DrawString(minValue.ToString(), 289, 240);
                DrawString(maxValue.ToString(), 289, 280);
                DrawString(oilRangeValue.ToString(), 289, 318);
            }
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

        public void DrawCostGrid(){
            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    int draw_x = dstar.node_array[y, x].j;
                    int draw_y = dstar.node_array[y, x].i;

                    DrawString(Convert.ToString(dstar.node_array[y, x].Gcost), draw_x * 25, draw_y * 25);
                }
            }
        }


        #endregion
    }
}
