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
        
        private Move move; // move that resulted in this node (null for root node)
        private Node parent;
        private int wins, visits;
        private List<Node> children;
        private List<Move> untriedMoves;

        public Node(Move move, Node parent,  State state)
        {
            this.move = move;
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

        public void AddChild(Move move, State state)
        {
            Node child = new Node(move, this, state); 
            this.untriedMoves.Remove(move);
            this.children.Add(child);
        } 

        public Node SelectChild()
        {
            Node selected = null;
            double best = Double.MinValue;
            foreach (Node child in children)
            {
                 
            }

            return selected;
        }
         
    }
}
