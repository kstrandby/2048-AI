using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _2048console
{
    class MonteCarlo
    {

        const double c = 0.2; // constant used for UCT
        Random random = new Random();

        public void Run(bool p)
        {
            throw new NotImplementedException();
        }

        public Node MonteCarloTreeSearch(State rootState, int iterations)
        {
            Node rootNode = new Node(null, null, rootState);

            for (int i = 0; i < iterations; i++)
            {
                Node node = rootNode;
                State state = rootState.Clone();

                // 1: Select
                while (node.UntriedMoves.Count == 0 && node.Children.Count != 0)
                {
                    node = node.SelectChild(c);
                    state = state.ApplyMove(node.GeneratingMove);
                }

                // 2: Expand
                if (node.UntriedMoves.Count != 0)
                {
                    Move randomMove = node.UntriedMoves[random.Next(0, node.UntriedMoves.Count)];
                    state = state.ApplyMove(randomMove);
                    node = node.AddChild(randomMove, state);
                }

                // 3: Simulation
                while (state.GetMoves().Count != 0)
                {
                    state = state.ApplyMove(state.GetRandomMove());
                }

                // 4: Backpropagation
                while (node != null)
                {
                    node.Update(state.GetResult());
                    node = node.Parent;
                }
            }

            Node bestNode = FindBestChild(rootNode.Children);
            return bestNode;
        }

        // best child is node with most wins - can be tweaked to use most visits (should be the same) or other strategy
        private Node FindBestChild(List<Node> children)
        {
            int mostWins = 0;
            Node best = null;
            foreach (Node child in children)
            {
                if (child.Wins > mostWins)
                {
                    best = child;
                    mostWins = child.Wins;
                }
            }
            return best;
        }
    }
}
