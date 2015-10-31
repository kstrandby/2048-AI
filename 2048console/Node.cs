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
        private State state;
        
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
        private int visits;
        private double results;
        public double Results
        {
            get
            {
                return this.results;
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
            this.state = state;
            this.generatingMove = move;
            this.parent = parent;
            this.results = 0;
            this.visits = 0;
            this.children = new List<Node>();
            this.untriedMoves = state.GetMoves();
        }

        public void Update(double result) 
        {
            this.visits += 1;
            this.results += result; 
        }

        public Node AddChild(Move move, State state)
        {
            Node child = new Node(move, this, state); 
            this.untriedMoves.Remove(move);
            this.children.Add(child);
            return child;
        } 

        public Node SelectChild()
        {
            Node selected = null;
            double best = Double.MinValue;

            double c = this.state.Points + 2000;

            foreach (Node child in children)
            {
                double UCT = child.results / child.visits + 2 * c * Math.Sqrt(2 * Math.Log(this.visits) / child.visits);
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
