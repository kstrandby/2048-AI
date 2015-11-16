using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        // stats
        public StreamWriter writer;

        public MonteCarlo(GameEngine gameEngine, int simulations, double constant)
        {
            this.gameEngine = gameEngine;
            this.simulations = simulations;
            this.c = constant;
            this.random = new Random();
            //writer = new StreamWriter(@"C:\Users\Kristine\Documents\Visual Studio 2013\Projects\2048console\MCTSNumThreadsTest.txt", true);
        }

        public State Run(bool print)
        {
            State rootState = null;
            
            while (true)
            {
                

                rootState = new State(GridHelper.CloneGrid(this.gameEngine.grid), this.gameEngine.scoreController.getScore(), GameEngine.PLAYER);

                //DIRECTION result = RootParallelizationMCTS(rootState, this.simulations, 8);
                DIRECTION result = RootParallelizationMCTSTimeLimited(rootState, 100, 8);
                PlayerMove move = new PlayerMove();
                move.Direction = result;
                if (result == (DIRECTION)(-1))
                {
                    // game over
                    Console.WriteLine("GAME OVER, final score = " + gameEngine.scoreController.getScore());
                    return rootState;
                }
                gameEngine.SendUserAction(move);

                //Node result = MCTS(rootState, this.simulations);
                //if (result == null)
                //{
                //    // game over
                //    Console.WriteLine("GAME OVER, final score = " + gameEngine.scoreController.getScore());
                //    //writer.Close();
                //    return rootState;
                //}
                //gameEngine.SendUserAction((PlayerMove)result.GeneratingMove);

                if (print)
                {
                    Program.CleanConsole();
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine(GridHelper.ToString(rootState.Grid));
                }
            
            }
            
        }

        public DIRECTION RootParallelizationMCTSTimeLimited(State rootState, int timeLimit, int numOfThreads)
        {
            ConcurrentBag<Node> allChildren = new ConcurrentBag<Node>();
            int numOfChildren = rootState.GetMoves().Count;

            Stopwatch timer = new Stopwatch();
            timer.Start();
            Parallel.For(0, numOfThreads, i =>
            {
                Node resultRoot = TimeLimitedMCTS(rootState, timeLimit, timer);
                foreach (Node child in resultRoot.Children)
                {
                    allChildren.Add(child);
                }
            });
            timer.Stop();

            List<int> totalVisits = new List<int>(4) { 0, 0, 0, 0 };
            List<double> totalResults = new List<double>(4) { 0, 0, 0, 0 };

            foreach (Node child in allChildren)
            {
                int direction = (int)((PlayerMove)child.GeneratingMove).Direction;
                totalVisits[direction] += child.Visits;
                totalResults[direction] += child.Results;
            }

            double best = Double.MinValue;
            int bestDirection = -1;
            for (int k = 0; k < 4; k++)
            {

                double avg = totalResults[k] / totalVisits[k];
                if (avg > best)
                {
                    best = avg;
                    bestDirection = k;
                }

            }

            if (bestDirection == -1) return (DIRECTION)(-1);
            return (DIRECTION)bestDirection;
        }

        private Node TimeLimitedMCTS(State rootState, int timeLimit, Stopwatch timer)
        {
            Node rootNode = new Node(null, null, rootState);
            int count = 0;
            while(timer.ElapsedMilliseconds < timeLimit)
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
                // state = SimulateGame(state);


                // 4: Backpropagation
                while (node != null)
                {
                    node.Update(state.GetResult());
                    node = node.Parent;
                }
                count++;
            }
            //Console.WriteLine(count + " simulations");
            return rootNode;
        }

        public DIRECTION RootParallelizationMCTS(State rootState, int iterations, int numOfThreads)
        {
            ConcurrentBag<Node> allChildren = new ConcurrentBag<Node>();
            int numOfChildren = rootState.GetMoves().Count;
            
            Parallel.For(0, numOfThreads, i =>
            {
                Node resultRoot = MonteCarloTreeSearch(rootState, iterations);
                foreach (Node child in resultRoot.Children)
                {
                    allChildren.Add(child);
                }
            });

            List<int> totalVisits = new List<int>(4) {0,0,0,0};
            List<double> totalResults = new List<double>(4){0,0,0,0};

            foreach (Node child in allChildren)
            {
                int direction = (int)((PlayerMove)child.GeneratingMove).Direction;
                totalVisits[direction] += child.Visits;
                totalResults[direction] += child.Results;
            }
            
            double best = Double.MinValue;
            int bestDirection = -1;
            for (int k = 0; k < 4; k++)
            {
                
                double avg = totalResults[k] / totalVisits[k];
                if (avg > best)
                {
                    best = avg;
                    bestDirection = k;
                }

            }

            if (bestDirection == -1) return (DIRECTION)(-1);
            return (DIRECTION)bestDirection;
        }

        public Node MCTS(State rootState, int iterations)
        {
            Node rootNode = MonteCarloTreeSearch(rootState, iterations);
            Node bestNode = FindBestChild(rootNode.Children);
            return bestNode;
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
               // state = SimulateGame(state);


                // 4: Backpropagation
                while (node != null)
                {
                    node.Update(state.GetResult());
                    node = node.Parent;
                }
            }
            return rootNode;
            
        }

        private State SimulateGame(State state)
        {
            List<Move> moves = state.GetMoves();

            while (moves.Count != 0)
            {
                if (moves.Count == 1)
                {
                    state = state.ApplyMove(moves[0]);
                }
                else
                {
                    if (state.Player == GameEngine.PLAYER)
                    {
                        Move down = moves.Find(x => ((PlayerMove)x).Direction == DIRECTION.DOWN);
                        if (down != null)
                        {
                            state = state.ApplyMove(down);
                        }
                        else
                        {
                            Move left = moves.Find(x => ((PlayerMove)x).Direction == DIRECTION.LEFT);
                            if (left != null)
                            {
                                state = state.ApplyMove(left);
                            }
                            else
                            {
                                Move right = moves.Find(x => ((PlayerMove)x).Direction == DIRECTION.RIGHT);
                                if (right != null)
                                {
                                    state = state.ApplyMove(right);
                                }

                            }
                        }

                    }
                    else
                    {
                        state = state.ApplyMove(state.GetRandomMove());
                    }
                }
                moves = state.GetMoves();
               
            }
            return state;
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
