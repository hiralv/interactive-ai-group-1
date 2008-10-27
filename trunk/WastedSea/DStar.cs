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

    class DStar
    {
        Square goal;
        Square start;
        Square[,] node_array;
        int[,] actual_cost_array;

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

                   /* if (PerceptualModel.mapArray[y, x] != 0)
                    {
                        actual_cost_array[y, x] = 999999;
                    }
                    else
                    {
                        actual_cost_array[y, x] = 1;
                    }*/
                }
            }
        }

        public void DrawDStarPath()
        {
            foreach (Square s in node_list)
            {
               // int offset = 7;
                //i_font.Draw(s.j * 25 + offset, s.i * 25 + offset, "[X]");
            }
        }

        public void Think()
        {
            //Begin setup.
            goal = node_array[2, 1];
            start = node_array[0, 0];

            Insert(goal, 0);

            int k_min = 0;

            while (start.tag != Tag.CLOSED && k_min != -1)
            {
                k_min = ProcessState();
            }

            Square current = start;

            //Begin the dynamic portion of D*.   Procede until you reach goal.
            while (!Equal(current, goal))
            {
                Square next_state = current.parent;

                int estimated_cost = next_state.Gcost;
                int actual_cost = actual_cost_array[next_state.i, next_state.j];

                if (estimated_cost != actual_cost)
                {
                    //Update the cost model to reflect actual cost.
                    ModifyCost(next_state, current, actual_cost);

                    //Propogate the cost change out.
                    int kk_min = ProcessState();
                    int h_cost = current.Hcost;

                    while (kk_min < h_cost)
                    {
                        kk_min = ProcessState();
                        h_cost = current.Hcost;
                    }
                }
                else
                {
                    node_list.Add(current);
                    current = next_state;
                    next_state = next_state.parent;
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
            else if (k_old == X.Hcost)
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
            Y.Gcost = cval;
            //Y.Gcost = cval;

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

        /** ... */
        int Min(int x, int y)
        {
            if (x < y)
            {
                return x;
            }

            return y;
        }

        /** Cost funtion. */
        int c(Square X, Square Y)
        {
            return node_array[Y.i, Y.j].Gcost;
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
                // if (square.tag != Tag.CLOSED && square.tag != Tag.OPEN)
                //{
                //square.parent = current;
                successors.Add(square);
                // }
            }

            //Left
            if ((x - 1) >= 0)
            {
                Square square = node_array[y, x - 1];
                //if (square.tag != Tag.CLOSED && square.tag != Tag.OPEN)
                //{
                // square.parent = current;
                successors.Add(square);
                //}
            }

            //Up
            if ((y + 1) <= 23)
            {
                Square square = node_array[y + 1, x];
                //if (square.tag != Tag.CLOSED && square.tag != Tag.OPEN)
                //{
                // square.parent = current;
                successors.Add(square);
                //}
            }

            //Down
            if ((y - 1) >= 0)
            {
                Square square = node_array[y - 1, x];
                //if (square.tag != Tag.CLOSED && square.tag != Tag.OPEN)
                //{
                //square.parent = current;
                successors.Add(square);
                //}
            }

            return successors;
        } 
    }


}
