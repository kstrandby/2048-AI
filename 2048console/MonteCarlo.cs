using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _2048console
{
    public class MonteCarlo
    {
        GameEngine gameEngine;
        int simulations;
        double c; // constant used for UCT
        Random random;
        public static Dictionary<int, int> minPoints = new Dictionary<int, int>
        {
            {512, 4608},
            {1024, 10240},
            {2048, 22528},
            {4096, 53248},
            {8192, 106494}
        };

        public MonteCarlo(GameEngine gameEngine, int simulations, double constant)
        {
            this.gameEngine = gameEngine;
            this.simulations = simulations;
            this.c = constant;
            this.random = new Random();
        }

        public State Run(bool print)
        {
            State rootState = null;
            while (true)
            {
                rootState = new State(GridHelper.CloneGrid(this.gameEngine.grid), this.gameEngine.scoreController.getScore(), GameEngine.PLAYER);
                Node result = MonteCarloTreeSearch(rootState, this.simulations);
                if (result == null)
                {
                    // game over
                    Console.WriteLine("GAME OVER, final score = " + gameEngine.scoreController.getScore());
                    return rootState;
                }
                
                gameEngine.SendUserAction((PlayerMove)result.GeneratingMove);

                if (print)
                {
                    Program.CleanConsole();
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine(GridHelper.ToString(rootState.Grid));
                }
            
            }
            
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
                    node = node.SelectChild();
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

            double bestResults = 0;
            Node best = null;
            foreach (Node child in children)
            {
                if (child.Results / child.Visits > bestResults)
                {
                    best = child;
                    bestResults = child.Results / child.Visits;
                }
            }
            return best;
        }
    }
}
