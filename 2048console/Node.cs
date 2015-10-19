/*
 * Node class used by Monte Carlo Tree Search
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2048console
{
    public class Node
    {
        
        private Move generatingMove; // move that resulted in this node (null for root node)
        public Move GeneratingMove
        {
            get
            {
                return this.generatingMove;
            }
            set
            {
                this.generatingMove = value;
            }
        } 
        private Node parent;
        public Node Parent
        {
            get
            {
                return this.parent;
            }
            set
            {
                this.parent = value;
            }
        }
        private int wins, visits;
        public int Wins
        {
            get
            {
                return this.wins;
            }
        }
        public int Visits
        {
            get
            {
                return this.visits;
            }
        }


        private List<Node> children;
        public List<Node> Children
        {
            get
            {
                return this.children;
            }
            set
            {
                this.children = value;
            }
        }
        private List<Move> untriedMoves;
        public List<Move> UntriedMoves
        {
            get
            {
                return this.untriedMoves;
            }
            set
            {
                this.untriedMoves = value;
            }
        }

        public Node(Move move, Node parent,  State state)
        {
            this.generatingMove = move;
            this.parent = parent;
            this.wins = 0;
            this.visits = 0;
            this.children = new List<Node>();
            this.untriedMoves = state.GetMoves();
        }

        public void Update(int result) // result 1 for win, 0 for lose
        {
            this.visits += 1;
            this.wins += result; 
        }

        public Node AddChild(Move move, State state)
        {
            Node child = new Node(move, this, state); 
            this.untriedMoves.Remove(move);
            this.children.Add(child);
            return child;
        } 

        public Node SelectChild(double c)
        {
            Node selected = null;
            double best = Double.MinValue;
            foreach (Node child in children)
            {
                double UCT = child.wins / child.visits + 2 * c * Math.Sqrt(2 * Math.Log(this.visits) / child.visits);
                if (UCT > best)
                {
                    selected = child;
                    best = UCT;
                }
            }
            return selected;
        }
         
    }
}
