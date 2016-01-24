using _2048console.GeneticAlgorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2048console
{
    // Node class used by Monte Carlo Tree Search
    class Node
    {
        // Constants used for Tree Policy
        private const int UCT_POLICY = 1;
        private const int PROG_BIAS_POLICY = 2;
        private int TREE_POLICY = PROG_BIAS_POLICY;
        private Random random;

        // State of the node
        public State state { get; set; }

        // move that resulted in this node (null for root node)
        private Move generatingMove; 
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

        // parent node (null if root node)
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
        
        // sum of all results of simulations where this node has been visited
        // prior to simulation
        private double results;
        public double Results
        {
            get
            {
                return this.results;
            }
        }

        // number of times this node has been visited during the search
        private int visits;
        public int Visits
        {
            get
            {
                return this.visits;
            }
        }

        // child nodes
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

        // moves that still hasn't been explored
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
            this.random = new Random();
            this.state = state;
            this.generatingMove = move;
            this.parent = parent;
            this.results = 0;
            this.visits = 0;
            this.children = new List<Node>();
            this.untriedMoves = state.GetMoves();
        }

        // called during backpropagation
        // updates the statistics of the node
        public void Update(double result) 
        {
            this.visits += 1;
            this.results += result; 
        }

        // adds a child node to the list of children
        // after exploring a move - removes the move from untried
        public Node AddChild(Move move, State state)
        {
            Node child = new Node(move, this, state); 
            this.untriedMoves.Remove(move);
            this.children.Add(child);
            return child;
        } 

        // Selects a child based on the TREE POLICY
        public Node SelectChild()
        {
            if (this.state.Player == GameEngine.PLAYER)
            {

            Node selected = null;
            double best = Double.MinValue;

            if (TREE_POLICY == UCT_POLICY) // Plain UCT
            {
                double c = this.state.Points;

                foreach (Node child in children)
                {
                    double UCT = child.results / child.visits + 2 * c * Math.Sqrt(2 * Math.Log(this.visits) / child.visits);
                    if (UCT > best)
                    {
                        selected = child;
                        best = UCT;
                    }
                }
            }
            else if (TREE_POLICY == PROG_BIAS_POLICY) // UCT with progressive bias
            {
                double c = this.state.Points;

                foreach (Node child in children)
                {
                    double f = AI.Evaluate(child.state) / (child.visits + 1);
                    double UCT = child.results / child.visits + 2 * c * Math.Sqrt(2 * Math.Log(this.visits) / child.visits) + f;
                    if (UCT > best)
                    {
                        selected = child;
                        best = UCT;
                    }
                }
            }
            
            return selected;

            }
            else // a chance node
            {
                return this.children[random.Next(0, this.children.Count)];
            }
        }
    }
}
