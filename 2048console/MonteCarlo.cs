using _2048console.GeneticAlgorithm;
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
    // class used for monte carlo tree searches
    class MonteCarlo
    {
        // Constants for default policy
        private const int RANDOM_POLICY = 1;
        private const int BEST_EVAL_POLICY = 2;
        private const int EXPECTIMAX_POLICY = 3;

        private GameEngine gameEngine;
        private Random random;
        private static WeightVectorAll weights = new WeightVectorAll { Corner = 0, Empty_cells = 0, Highest_tile = 0, Monotonicity = 0, Points = 0, Smoothness = 0, Snake = 1, Trapped_penalty = 0 };

        public MonteCarlo(GameEngine gameEngine)
        {
            this.gameEngine = gameEngine;
            this.random = new Random();
        }


        // Runs an entire game using MCTS limited by number of iterations
        public State RunIterationLimitedMCTS(bool print, int iterationLimit)
        {
            State rootState = null;

            while (true)
            {
                rootState = new State(BoardHelper.CloneBoard(this.gameEngine.board), this.gameEngine.scoreController.getScore(), GameEngine.PLAYER);

                if (print)
                {
                    Program.PrintState(rootState);
                }


                Node result = IterationLimitedMCTS(rootState, iterationLimit);
                if (result == null)
                {
                    // game over
                    return rootState;
                }
                gameEngine.SendUserAction((PlayerMove)result.GeneratingMove);
            }
        }

        // Runs an entire game using MCTS limited by time
        public State RunTimeLimitedMCTS(bool print, int timeLimit)
        {
            State rootState = null;

            while (true)
            {
                rootState = new State(BoardHelper.CloneBoard(this.gameEngine.board), this.gameEngine.scoreController.getScore(), GameEngine.PLAYER);

                if (print)
                {
                    Program.PrintState(rootState);
                }

                Node result = TimeLimitedMCTS(rootState, timeLimit);
                if (result == null)
                {
                    // game over
                    return rootState;
                }
                gameEngine.SendUserAction((PlayerMove)result.GeneratingMove);
            }
        }

        // Runs an entire game using parallelized MCTS limited by number of iterations
        public State RunRootParallelizationIterationLimitedMCTS(bool print, int iterationLimit, int numThreads)
        {
            State rootState = null;

            while (true)
            {
                rootState = new State(BoardHelper.CloneBoard(this.gameEngine.board), this.gameEngine.scoreController.getScore(), GameEngine.PLAYER);

                if (print)
                {
                    Program.PrintState(rootState);
                }
                DIRECTION result = RootParallelizationMCTS(rootState, iterationLimit, numThreads);
                PlayerMove move = new PlayerMove();
                move.Direction = result;
                if (result == (DIRECTION)(-1))
                {
                    // game over
                    return rootState;
                }
                gameEngine.SendUserAction(move);
            }
        }

        // Runs an entire game using parallelized MCTS limited by time
        public State RunRootParallelizationTimeLimitedMCTS(bool print, int timeLimit, int numThreads)
        {
            State rootState = null;

            while (true)
            {
                rootState = new State(BoardHelper.CloneBoard(this.gameEngine.board), this.gameEngine.scoreController.getScore(), GameEngine.PLAYER);

                if (print)
                {
                    Program.PrintState(rootState);
                }
                DIRECTION result = RootParallelizationMCTSTimeLimited(rootState, timeLimit, numThreads);
                PlayerMove move = new PlayerMove();
                move.Direction = result;
                if (result == (DIRECTION)(-1))
                {
                    // game over
                    return rootState;
                }
                gameEngine.SendUserAction(move);
            }
        }

        // Runs a root-parallelized Monte Carlo Tree Search
        // The root parallelization basically means that separate searches are started in separate threads
        // from the root
        // For each thread, the search runs for the same number of iterations, given as input
        // When each thread has finished, the results are combined and the best child node is found based on
        // the combined results
        public DIRECTION RootParallelizationMCTS(State rootState, int iterations, int numOfThreads)
        {
            ConcurrentBag<Node> allChildren = new ConcurrentBag<Node>();
            int numOfChildren = rootState.GetMoves().Count;
            
            Parallel.For(0, numOfThreads, i =>
            {
                Node resultRoot = IterationLimited(rootState, iterations / numOfThreads);
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

        // Runs a root-parallelized Monte Carlo Tree Search in the same way as the RootParallelizationMCTS,
        // but limited by a time limit instead of number of iterations
        public DIRECTION RootParallelizationMCTSTimeLimited(State rootState, int timeLimit, int numOfThreads)
        {
            ConcurrentBag<Node> allChildren = new ConcurrentBag<Node>();
            int numOfChildren = rootState.GetMoves().Count;

            Stopwatch timer = new Stopwatch();
            timer.Start();
            
            Parallel.For(0, numOfThreads, i =>
            {
                Node resultRoot = TimeLimited(rootState, timeLimit, timer);
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

        // Starts the iteration limited Monte Carlo Tree Search and returns the best child node
        // resulting from the search
        public Node IterationLimitedMCTS(State rootState, int iterations)
        {
            Node rootNode = IterationLimited(rootState, iterations);
            Node bestNode = FindBestChild(rootNode.Children);
            return bestNode;
        }

        // Starts the time limited Monte Carlo Tree Search and returns the best child node
        // resulting from the search
        public Node TimeLimitedMCTS(State rootState, int timeLimit)
        {
            Stopwatch timer = new Stopwatch();
            Node bestNode = null;
            while (bestNode == null && !rootState.IsGameOver())
            {
                timer.Start();
                Node rootNode = TimeLimited(rootState, timeLimit, timer);
                bestNode = FindBestChild(rootNode.Children);
                timeLimit += 10;
                timer.Reset();
            }
            
            return bestNode;
        }

        // Runs a Monte Carlo Tree Search limited by a given time limit
        public Node TimeLimited(State rootState, int timeLimit, Stopwatch timer)
        {
            Node rootNode = new Node(null, null, rootState);
            while (true)
            {
                if (timer.ElapsedMilliseconds > timeLimit)
                {
                    if (FindBestChild(rootNode.Children) == null && !rootNode.state.IsGameOver())
                    {
                        timeLimit += 10;
                        timer.Restart();
                    }
                    else
                    {
                        return rootNode;
                    }
                }
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
                state = SimulateGame(state, EXPECTIMAX_POLICY);

                // 4: Backpropagation
                while (node != null)
                {
                    node.Update(state.GetResult());
                    node = node.Parent;
                }
            }
        }

        // Runs a Monte Carlo Tree Search from the given root node
        // Limited by number of iterations
        public Node IterationLimited(State rootState, int iterations)
        {
            Node rootNode = new Node(null, null, rootState);

            for (int i = 0; i < iterations; i++)
            {
                Node node = rootNode;
                State state = rootState.Clone();

                // 1: Select
                while (node.UntriedMoves.Count == 0 && node.Children.Count != 0)
                {
                    if (state.Player == GameEngine.COMPUTER)
                    {
                        Move move = state.GetRandomMove();
                        state = state.ApplyMove(move);
                        Node parent = node;
                        node = new Node(move, parent, state);
                    }
                    else
                    {
                        node = node.SelectChild();
                        state = state.ApplyMove(node.GeneratingMove);
                    }
                }

                // 2: Expand
                if (node.UntriedMoves.Count != 0)
                {
                    Move randomMove = node.UntriedMoves[random.Next(0, node.UntriedMoves.Count)];
                    state = state.ApplyMove(randomMove);
                    node = node.AddChild(randomMove, state);
                }

                // 3: Simulation
                state = SimulateGame(state, EXPECTIMAX_POLICY);

                // 4: Backpropagation
                while (node != null)
                {
                    node.Update(state.GetResult());
                    node = node.Parent;
                }
            }
            return rootNode; 
        }

        // Simulates a game to the end (game over) based on the default policy
        // Returns the game over state
        private State SimulateGame(State state, int POLICY)
        {
            if (POLICY == RANDOM_POLICY)
            {
                while (state.GetMoves().Count != 0)
                {
                    state = state.ApplyMove(state.GetRandomMove());
                }
                return state;
            }
            else if (POLICY == BEST_EVAL_POLICY)
            {
                List<Move> moves = state.GetMoves();
                while (moves.Count != 0)
                {
                    // random move for computer
                    if (state.Player == GameEngine.COMPUTER)
                    {
                        state = state.ApplyMove(state.GetRandomMove());
                    }
                    else
                    {
                        // find the move that results in the best child state, based on the evaluation function
                        State bestState = null;
                        double bestScore = Double.MinValue;
                        foreach (Move move in moves)
                        {
                            State result = state.ApplyMove(move);
                            double score = AI.Evaluate(result);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestState = result;
                            }
                        }
                        state = bestState;
                    }
                    moves = state.GetMoves();
                }
                return state;
            }
            else if (POLICY == EXPECTIMAX_POLICY)
            {
                Expectimax expectimax = new Expectimax(gameEngine, 2);
                while (state.GetMoves().Count != 0) {
                    if (state.Player == GameEngine.COMPUTER)
                    {
                        state = state.ApplyMove(state.GetRandomMove());
                    }
                    else
                    {
                        Move move = expectimax.ExpectimaxAlgorithm(state, 2, weights);
                        state = state.ApplyMove(move);
                    }
                }
                return state;
            }
            else throw new Exception();
        }
       
        // Called at the end of a MCTS to decide on the best child
        // Best child is the child with the highest average score
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
