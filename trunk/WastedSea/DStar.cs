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
    public enum Tag
    {
        NEW,
        CLOSED,
        OPEN
    };

    //    Used to represent values for each square.
    public class Square
    {
        public int i = 0;         // x location
        public int j = 0;         // y location
        public Square parent = null;      // Parent with lowest cost
        public int Gcost = 0;         // Distance cost
        public int Hcost = 0;         // Heuristic
        public int value = 0;         // V = G + H
        public bool passed = false;     // Open list marker
        public Tag tag = Tag.NEW;
    }

    public class DStar
    {
        Square goal;
        Square start;
        public Square[,] node_array;
        public int[,] actual_cost_array;
        public bool STARTED;
        int k_min;
        Square current;

        List<Square> node_list;
        public DStar()
        {
            goal = new Square();
            start = new Square();

            node_list = new List<Square>();
            node_array = new Square[24, 32];
            actual_cost_array = new int[24, 32];

            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    node_array[y, x] = new Square();
                    node_array[y, x].i = y;
                    node_array[y, x].j = x;
                    node_array[y, x].tag = Tag.NEW;
                    node_array[y, x].Gcost = 1;
                    node_array[y, x].Hcost = 0;
                }
            }

            STARTED = false;
        }

        public void DrawDStarPath()
        {
            foreach (Square s in node_list)
            {

            }
        }

        //Does the initial D* setup.
        public void Start(int start_x, int start_y, int end_x, int end_y)
        {
            //Begin setup.
            goal = node_array[end_y, end_x];
            start = node_array[start_y, start_x];
            Insert(goal, 0);

            int k_min = 0;

            while (start.tag != Tag.CLOSED && k_min != -1)
            {
                k_min = ProcessState();
            }

            current = start;
            STARTED = true;
            this.k_min = 0;
        }

        public Square Think(int[,] actual_cost_array)
        {
            //Updates with percepts.
            this.actual_cost_array = actual_cost_array;
            Square move = null;

            //Begin the dynamic portion of D*.   Procede until you reach goal.
            if (!Equal(current, goal))
            {
                bool found_move = false;

                //D* will loop till it can make one move.
                while (!found_move) 
                {
                    int estimated_cost = current.Hcost - current.parent.Hcost;
                    int actual_cost = actual_cost_array[current.parent.i, current.parent.j];

                    if (estimated_cost != actual_cost)
                    {
                        //Update the cost model to reflect actual cost.
                        ModifyCost(current.parent, current, actual_cost);

                        //Propogate the cost change out.
                        int kk_min = -2;
                        int h_cost = current.Hcost;

                        while (kk_min < h_cost)
                        {
                            if (kk_min != -1)
                            {
                                kk_min = ProcessState();
                                h_cost = current.Hcost;
                            }
                            else
                            {
                                //Error,  path cannot be found.    
                                break;
                            }
                        }
                    }

                    move = current;
                    node_list.Add(current);         //Make the move.
                    current = current.parent;
                   
                    UpdateWithPercepts(current, actual_cost_array);
                    found_move = true;
                }

            }
            else
            {
                STARTED = false;
            }

            return move;
        }

        //Updates the cells around Zippy within his percept range based on percept knowledge.
        public void UpdateWithPercepts(Square s, int[,] actual_cost_array)
        {
            int x_start = s.j - 2;
            if (x_start < 0)
            {
                x_start = 0;
            }

            int x_end = s.j + 3;
            if (x_end > 32)
            {
                x_end = 32;
            }

            int y_start = s.i - 2;
            if (y_start < 0)
            {
                y_start = 0;
            }
            int y_end = s.i + 3;
            if (y_end > 24)
            {
                y_end = 24;
            }

            for (int y = y_start; y < y_end; y++)
            {
                for (int x = x_start; x < x_end; x++)
                {
                    if (actual_cost_array[y, x] == 0)
                    {
                        node_array[y, x].Gcost = 1;
                    }
                    else
                    {
                        node_array[y, x].Gcost = 768;
                    }
                }
            }
        }

        /** Core function of the D* search algorithm. Propogates g-costs.*/
        int ProcessState()
        {
            Square X = MinState();

            if (X == null)
            {
                return -1;
            }

            List<Square> neighbors = Successors(X);

            int k_old = GetKMin();
            Delete(X);						//Removes min-state from open list.

            if (k_old < X.Hcost)
            {
                foreach (Square Y in neighbors)
                {
                    if (Y.Hcost <= k_old && X.Hcost > (Y.Hcost + c(Y, X)))
                    {
                        X.parent = Y;
                        X.Hcost = Y.Hcost + c(Y, X);
                    }
                }
            }
            if (k_old == X.Hcost)
            {
                foreach (Square Y in neighbors)
                {
                    if ((Y.tag == Tag.NEW) ||
                        ((Y.parent == X) && Y.Hcost != (X.Hcost + c(X, Y))) ||
                        ((Y.parent != X) && Y.Hcost > (X.Hcost + c(X, Y))))
                    {

                        Y.parent = X;
                        Insert(Y, X.Hcost + c(X, Y));
                    }
                }
            }
            else
            {
                foreach (Square Y in neighbors)
                {
                    if (Y.tag == Tag.NEW ||
                        ((Y.parent == X) && (Y.Hcost != (X.Hcost + c(X, Y)))))
                    {
                        Y.parent = X;
                        Insert(Y, X.Hcost + c(X, Y));
                    }
                    else
                    {
                        if ((Y.parent != X) && (Y.Hcost > (X.Hcost + c(X, Y))))
                        {
                            Insert(X, X.Hcost);
                        }
                        else
                        {
                            if ((Y.parent != X) &&
                                (X.Hcost > (Y.Hcost + c(Y, X))) &&
                                (Y.tag == Tag.CLOSED) &&
                                (Y.Hcost > k_old))
                            {
                                Insert(Y, Y.Hcost);
                            }
                        }
                    }
                }
            }

            return GetKMin();
        }

        /** Updates the cost function for a particular node arc. */
        int ModifyCost(Square X, Square Y, int cval)
        {
            X.Gcost = cval;

            if (X.tag == Tag.CLOSED)
            {
                Insert(X, X.Hcost);
            }

            return GetKMin();
        }

        //Returns the state with the lowest k-value in the open node list.
        Square MinState()
        {
            Square min_state = null;
            float key = 9999999.0f;

            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    Square s = node_array[y, x];

                    if (s.value < key && s.tag == Tag.OPEN)
                    {
                        key = s.value;
                        min_state = s;
                    }
                }
            }

            return min_state;
        }

        /** Returns the lowest k-value on the open list.*/
        int GetKMin()
        {
            Square min_state = MinState();
            if (min_state != null)
            {
                return MinState().value;
            }

            return -1;
        }

        /** Adds a node to the open list. */
        void Insert(Square state, int h_new)
        {
            if (state.tag == Tag.NEW)
            {
                state.value = h_new;
            }
            else if (state.tag == Tag.OPEN)
            {

                state.value = Min(state.value, h_new);
            }
            else if (state.tag == Tag.CLOSED)
            {
                state.value = Min(state.Hcost, h_new);
            }

            state.Hcost = h_new;
            state.tag = Tag.OPEN;

        }

        /** Removes a node from the open list. */
        void Delete(Square state)
        {
            node_array[state.i, state.j].tag = Tag.CLOSED;
        }

        /** Returns the minimum of two values. */
        int Min(int x, int y)
        {
            if (x < y)
            {
                return x;
            }

            return y;
        }

        /** Returns the arc cost from y to x. */
        int c(Square X, Square Y)
        {
            return node_array[X.i, X.j].Gcost;
            //return 1;
        }

        /** Compares to squares by (x,y) location. */
        bool Equal(Square one, Square two)
        {
            if (one.i == two.i && one.j == two.j)
            {
                return true;
            }

            return false;
        }

        /** Returns all possible successor states of the current state. */
        List<Square> Successors(Square current)
        {
            List<Square> successors = new List<Square>();

            int x = current.j;
            int y = current.i;

            //Right
            if ((x + 1) <= 31)
            {
                Square square = node_array[y, x + 1];
                successors.Add(square);
            }

            //Left
            if ((x - 1) >= 0)
            {
                Square square = node_array[y, x - 1];
                successors.Add(square);
            }

            //Up
            if ((y + 1) <= 23)
            {
                Square square = node_array[y + 1, x];
                successors.Add(square);
            }

            //Down
            if ((y - 1) >= 0)
            {
                Square square = node_array[y - 1, x];
                successors.Add(square);
            }

            return successors;
        } 
    }


}
