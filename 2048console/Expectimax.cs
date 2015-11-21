using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace _2048console
{
    // class used for expectimax searches
    class Expectimax
    {

        private GameEngine gameEngine;
        private ScoreController scoreController;
        private int chosenDepth;
        private State currentState;

        public Expectimax(GameEngine game, int depth)
        {
            this.gameEngine = game;
            this.scoreController = gameEngine.scoreController;
            this.chosenDepth = depth;
        }

        private int evaluatedStates;

        public State RunClassicExpectimax(bool print)
        {
            while (true)
            {
                // update state
                currentState = new State(BoardHelper.CloneBoard(gameEngine.board), scoreController.getScore(), GameEngine.PLAYER);

                if (print)
                {
                    Program.PrintState(currentState);
                }

                // run algorithm and send action choice to game engine
                Move move = ExpectimaxAlgorithm(currentState, chosenDepth);
                if (((PlayerMove)move).Direction == (DIRECTION)(-1))
                {
                    // game over
                    return currentState;
                }
                gameEngine.SendUserAction((PlayerMove)move);
            }
        }


        public State RunStar1Expectimax(bool print)
        {
            while (true)
            {
                // update state
                currentState = new State(BoardHelper.CloneBoard(gameEngine.board), scoreController.getScore(), GameEngine.PLAYER);

                if (print)
                {
                    Program.PrintState(currentState);
                }

                // run algorithm and send action choice to game engine
                Move move = Star1Expectimax(currentState, Double.MinValue, Double.MaxValue, chosenDepth);
                if (((PlayerMove)move).Direction == (DIRECTION)(-1))
                {
                    // game over
                    return currentState;
                }
                gameEngine.SendUserAction((PlayerMove)move);
            }
        }

        public State RunParallelClassicExpectimax(bool print)
        {
            while (true)
            {
                // update state
                currentState = new State(BoardHelper.CloneBoard(gameEngine.board), scoreController.getScore(), GameEngine.PLAYER);

                if (print)
                {
                    Program.PrintState(currentState);
                }

                // run algorithm and send action choice to game engine
                Move move = ParallelExpectimax(currentState, chosenDepth);
                if (((PlayerMove)move).Direction == (DIRECTION)(-1))
                {
                    // game over
                    return currentState;
                }
                gameEngine.SendUserAction((PlayerMove)move);
            }
        }

        public State RunIterativeDeepeningExpectimax(bool print, int timeLimit)
        {
            while (true)
            {
                // update state
                currentState = new State(BoardHelper.CloneBoard(gameEngine.board), scoreController.getScore(), GameEngine.PLAYER);

                if (print)
                {
                    Program.PrintState(currentState);
                }

                // run algorithm and send action choice to game engine
                Move move = IterativeDeepening(currentState, timeLimit);
                if (((PlayerMove)move).Direction == (DIRECTION)(-1))
                {
                    // game over
                    return currentState;
                }
                gameEngine.SendUserAction((PlayerMove)move);
            }
        }

        public State RunParallelIterativeDeepeningExpectimax(bool print, int timeLimit)
        {
            while (true)
            {
                // update state
                currentState = new State(BoardHelper.CloneBoard(gameEngine.board), scoreController.getScore(), GameEngine.PLAYER);

                if (print)
                {
                    Program.PrintState(currentState);
                }

                // run algorithm and send action choice to game engine
                Move move = ParallelIterativeDeepeningExpectimax(currentState, timeLimit);
                if (((PlayerMove)move).Direction == (DIRECTION)(-1))
                {
                    // game over
                    return currentState;
                }
                gameEngine.SendUserAction((PlayerMove)move);
            }
        }

        // Run Expectimax simulation from given state until game over state is reached
        // returns the game over state
        public State RunFromState(State state) {
            
            State currentState = state;
            if (state.Player == GameEngine.COMPUTER) currentState = state.ApplyMove(state.GetRandomMove());
            while(true) {
                
                Move move = ExpectimaxAlgorithm(currentState, chosenDepth);
                if (((PlayerMove)move).Direction == (DIRECTION)(-1))
                {
                    return state;
                }
                else
                {
                    currentState = currentState.ApplyMove(move);
                    currentState = currentState.ApplyMove(currentState.GetRandomMove());
                }
            }
        }

        // Runs a parallel expectimax search to speed up search
        // A search is started in a separate thread for each child of the given root node
        // This method should only be called for the the root, where depth will always 
        // be > 0 and player will always be GameEngine.PLAYER - the recursion is started
        // for the children of the root using standard Expectimax algorithm
        private Move ParallelExpectimax(State state, int depth)
        {
            Move bestMove = new PlayerMove();

            List<Move> moves = state.GetMoves();
            ConcurrentBag<Tuple<double, Move>> scores = new ConcurrentBag<Tuple<double, Move>>();

            if (moves.Count == 0)
            {
                // game over
                return bestMove;
            }

            // create the resulting states before starting the threads
            List<State> resultingStates = new List<State>();
            foreach (Move move in moves)
            {
                State resultingState = state.ApplyMove(move);
                resultingStates.Add(resultingState);
            }

            // start a thread for each child
            Parallel.ForEach(resultingStates, resultingState =>
            {
                double score = ExpectimaxAlgorithm(resultingState, depth - 1).Score;
                scores.Add(new Tuple<double, Move>(score, resultingState.GeneratingMove));
            });
            // find the best score
            double highestScore = Double.MinValue;
            foreach (Tuple<double, Move> score in scores)
            {
                PlayerMove move = (PlayerMove)score.Item2;
                if (score.Item1 > highestScore)
                {
                    highestScore = score.Item1;
                    bestMove = score.Item2;
                }
            }
            return bestMove;

        }

        // Parallel version of Expectimax with Star1 pruning
        // Only called at the root, where depth will always be > 0 and player will always be player
        private Move ThreadingStar1(State state, int depth)
        {
            Move bestMove = new PlayerMove();

            List<Move> moves = state.GetMoves();
            ConcurrentBag<Tuple<double,Move>> scores = new ConcurrentBag<Tuple<double,Move>>();

            if (moves.Count == 0)
            {
                // game over
                return bestMove;
            }

            // create the resulting states before starting the threads
            List<State> resultingStates = new List<State>();
            foreach (Move move in moves)
            {
                State resultingState = state.ApplyMove(move);
                resultingStates.Add(resultingState);
            }

            Parallel.ForEach(resultingStates, resultingState =>
            {
                double score = Star1Expectimax(resultingState, Double.MinValue, Double.MaxValue, depth - 1).Score;
                scores.Add(new Tuple<double, Move>(score, resultingState.GeneratingMove));
            });
            // find the best score
            double highestScore = Double.MinValue;
            foreach (Tuple<double, Move> score in scores)
            {
                PlayerMove move = (PlayerMove)score.Item2;
                if (score.Item1 > highestScore)
                {
                    highestScore = score.Item1;
                    bestMove = score.Item2;
                }
            }
            return bestMove;
            
        }

        // Runs a parallel version of iterative deepening
        // A search is started in a separate thread for each child of root node 
        private Move ParallelIterativeDeepeningExpectimax(State state, int timeLimit)
        {
            Move bestMove = new PlayerMove();

            List<Move> moves = state.GetMoves();
            ConcurrentBag<Tuple<double, Move>> scores = new ConcurrentBag<Tuple<double, Move>>();

            if (moves.Count == 0)
            {
                // game over
                return bestMove;
            }

            // create the resulting states before starting the threads
            List<State> resultingStates = new List<State>();
            foreach (Move move in moves)
            {
                State resultingState = state.ApplyMove(move);
                resultingStates.Add(resultingState);
            }

            Parallel.ForEach(resultingStates, resultingState =>
            {
                double score = IterativeDeepening(resultingState, timeLimit).Score;
                scores.Add(new Tuple<double, Move>(score, resultingState.GeneratingMove));
            });
            // find the best score
            double highestScore = Double.MinValue;
            foreach (Tuple<double, Move> score in scores)
            {
                PlayerMove move = (PlayerMove)score.Item2;
                if (score.Item1 > highestScore)
                {
                    highestScore = score.Item1;
                    bestMove = score.Item2;
                }
            }
            return bestMove;
        }

        // Iterative Deepening Expectimax search
        private Move IterativeDeepening(State state, double timeLimit)
        {
            int depth = 0;
            Stopwatch timer = new Stopwatch();
            Move bestMove = null;
            // start the search
            timer.Start();
            while (timer.ElapsedMilliseconds < timeLimit)
            {
                Tuple<Move, Boolean> result = IterativeDeepeningExpectimax(state, depth, timeLimit, timer);
                if (result.Item2) bestMove = result.Item1; // only update bestMove if full recursion
                depth++;
                

            }
            return bestMove;
        }

        // Recursive part of iterative deepening Expectimax
        private Tuple<Move, Boolean> IterativeDeepeningExpectimax(State state, int depth, double timeLimit, Stopwatch timer)
        {
            Move bestMove;

            if (depth == 0 || state.IsGameOver())
            {
                if (state.Player == GameEngine.PLAYER)
                {
                    bestMove = new PlayerMove(); // dummy action, as there will be no valid move
                    bestMove.Score = AI.Evaluate(gameEngine, state);
                    return new Tuple<Move, Boolean>(bestMove, true);
                }
                else if (state.Player == GameEngine.COMPUTER)
                {
                    bestMove = new ComputerMove(); // dummy action, as there will be no valid move
                    bestMove.Score = AI.Evaluate(gameEngine, state);
                    return new Tuple<Move, Boolean>(bestMove, true);
                }
                else throw new Exception();
            }
            if (state.Player == GameEngine.PLAYER) // AI's turn 
            {
                bestMove = new PlayerMove();
                double highestScore = Double.MinValue, currentScore = Double.MinValue;

                List<Move> moves = state.GetMoves();
                foreach (Move move in moves)
                {
                    State resultingState = state.ApplyMove(move);
                    currentScore = IterativeDeepeningExpectimax(resultingState, depth - 1, timeLimit, timer).Item1.Score;
                    
                    if (currentScore > highestScore)
                    {
                        highestScore = currentScore;
                        bestMove = move;
                    }
                    if (timer.ElapsedMilliseconds > timeLimit)
                    {
                        bestMove.Score = highestScore;
                        return new Tuple<Move, Boolean>(bestMove, false); // recursion not completed, return false
                    }
                }
                bestMove.Score = highestScore;
                return new Tuple<Move, Boolean>(bestMove, true);
            }
            else if (state.Player == GameEngine.COMPUTER) // computer's turn  (the random event node)
            {
                bestMove = new ComputerMove();

                // return the weighted average of all the child nodes's scores
                double average = 0;
                List<Cell> availableCells = state.GetAvailableCells();
                List<Move> moves = state.GetAllComputerMoves(availableCells);
                int moveCheckedSoFar = 0;
                foreach (Move move in moves)
                {
                    State resultingState = state.ApplyMove(move);

                    average += StateProbability(((ComputerMove)move).Tile) * IterativeDeepeningExpectimax(resultingState, depth - 1, timeLimit, timer).Item1.Score;
                    moveCheckedSoFar++;
                    if (timer.ElapsedMilliseconds > timeLimit)
                    {
                        bestMove.Score = average / moveCheckedSoFar;
                        return new Tuple<Move, Boolean>(bestMove, false); // recursion not completed, return false
                    }
                }
                bestMove.Score = average / moves.Count;
                return new Tuple<Move, Boolean>(bestMove, true);
            }
            else throw new Exception();

        }

        // Classic Expectimax search
        private Move ExpectimaxAlgorithm(State state, int depth)
        {
            //evaluatedStates++;
            Move bestMove;

            if (depth == 0 || state.IsGameOver())
            {
                if (state.Player == GameEngine.PLAYER)
                {
                    bestMove = new PlayerMove(); // dummy action, as there will be no valid move
                    bestMove.Score = AI.Evaluate(gameEngine, state);
                    return bestMove;
                }
                else if (state.Player == GameEngine.COMPUTER)
                {
                    bestMove = new ComputerMove(); // dummy action, as there will be no valid move
                    bestMove.Score = AI.Evaluate(gameEngine, state);
                    return bestMove;
                }
                else throw new Exception();
            }
            if (state.Player == GameEngine.PLAYER) // AI's turn 
            {
                bestMove = new PlayerMove();
                double highestScore = Double.MinValue, currentScore = Double.MinValue;

                List<Move> moves = state.GetMoves();
                foreach (Move move in moves)
                {
                    State resultingState = state.ApplyMove(move);
                    currentScore = ExpectimaxAlgorithm(resultingState, depth - 1).Score;
                    if (currentScore > highestScore)
                    {
                        highestScore = currentScore;
                        bestMove = move;
                    }
                }
                bestMove.Score = highestScore;
                return bestMove;
            }
            else if (state.Player == GameEngine.COMPUTER) // computer's turn  (the random event node)
            {
                bestMove = new ComputerMove();

                // return the weighted average of all the child nodes's scores
                double average = 0;
                List<Cell> availableCells = state.GetAvailableCells();
                List<Move> moves = state.GetAllComputerMoves(availableCells);
                foreach (Move move in moves)
                {
                    State resultingState = state.ApplyMove(move);

                    average += StateProbability(((ComputerMove)move).Tile) * ExpectimaxAlgorithm(resultingState, depth - 1).Score;
                }
                bestMove.Score = average / moves.Count;
                return bestMove;
            }
            else throw new Exception();
        }
        
        // Expectimax search with Star1 pruning
        private Move Star1Expectimax(State state, double alpha, double beta, int depth)
        {
            evaluatedStates++;
            Move bestMove;

            if (depth == 0 || state.IsGameOver())
            {
                if (state.Player == GameEngine.PLAYER)
                {
                    bestMove = new PlayerMove(); // dummy action, as there will be no valid move
                    bestMove.Score = AI.Evaluate(gameEngine, state);
                    return bestMove;
                }
                else if (state.Player == GameEngine.COMPUTER)
                {
                    bestMove = new ComputerMove(); // dummy action, as there will be no valid move
                    bestMove.Score = AI.Evaluate(gameEngine, state);
                    return bestMove;
                }
                else throw new Exception();
            }
            if (state.Player == GameEngine.PLAYER)
            {
                bestMove = new PlayerMove();
                double highestScore = Double.MinValue, currentScore = Double.MinValue;

                List<Move> moves = state.GetMoves();
                foreach (Move move in moves)
                {
                    State resultingState = state.ApplyMove(move);
                    currentScore = Star1Expectimax(resultingState, alpha, beta, depth - 1).Score;
                    if (currentScore > highestScore)
                    {
                        highestScore = currentScore;
                        bestMove = move;
                    }
                    alpha = Math.Max(alpha, highestScore);
                    if (beta <= alpha)
                    { // beta cut-off
                        break;
                    }
                }

                bestMove.Score = highestScore;
                return bestMove;
            }
            else if (state.Player == GameEngine.COMPUTER) // computer's turn  (the random event node)
            {
                bestMove = new ComputerMove();

                List<Cell> availableCells = state.GetAvailableCells();
                List<Move> moves = state.GetAllComputerMoves(availableCells);

                int numSuccessors = moves.Count;
                double upperBound = AI.GetUpperBound();
                double lowerBound = AI.GetLowerBound();
                double curAlpha = numSuccessors * (alpha - upperBound) + upperBound;
                double curBeta = numSuccessors * (beta - lowerBound) + lowerBound;

                double scoreSum = 0;
                int i = 1;
                foreach (Move move in moves)
                {
                    double sucAlpha = Math.Max(curAlpha, lowerBound);
                    double sucBeta = Math.Min(curBeta, upperBound);

                    State resultingState = state.ApplyMove(move);

                    double score = StateProbability(((ComputerMove)move).Tile) * Star1Expectimax(resultingState, sucAlpha, sucBeta, depth - 1).Score;
                    scoreSum += score;
                    if (score <= curAlpha)
                    {
                        scoreSum += upperBound * (numSuccessors - i);
                        bestMove.Score = scoreSum / numSuccessors;
                        return bestMove; // pruning
                    }
                    if (score >= curBeta)
                    {
                        scoreSum += lowerBound * (numSuccessors - i);
                        bestMove.Score = scoreSum / numSuccessors;
                        return bestMove; // pruning
                    }
                    curAlpha += upperBound - score;
                    curBeta += lowerBound - score;

                    i++;

                }
                bestMove.Score = scoreSum / numSuccessors;
                return bestMove;
            }

            else throw new Exception();
        }
        
        // Given a tile value, returns the probability that a random tile generated by the
        // computer will take this value
        private double StateProbability(int tileValue)
        {
            if (tileValue == 2) // tile 2 spawn
            {
                return 0.9;
            }
            else if (tileValue == 4)// tile 4 spawn
            {
                return 0.1;
            }
            else throw new Exception();
        }


        // not used - way too slow
        private Move Star2Expectimax(State state, double alpha, double beta, int depth)
        {
            Move bestMove;

            if (depth == 0 || state.IsGameOver())
            {
                if (state.Player == GameEngine.PLAYER)
                {
                    bestMove = new PlayerMove(); // dummy action, as there will be no valid move
                    bestMove.Score = AI.Evaluate(gameEngine, state);
                    return bestMove;
                }
                else if (state.Player == GameEngine.COMPUTER)
                {
                    bestMove = new ComputerMove(); // dummy action, as there will be no valid move
                    bestMove.Score = AI.Evaluate(gameEngine, state);
                    return bestMove;
                }
                else throw new Exception();
            }
            if (state.Player == GameEngine.PLAYER)
            {
                bestMove = new PlayerMove();
                double highestScore = Double.MinValue, currentScore = Double.MinValue;

                List<Move> moves = state.GetMoves();
                foreach (Move move in moves)
                {
                    State resultingState = state.ApplyMove(move);
                    currentScore = Star2Expectimax(resultingState, alpha, beta, depth - 1).Score;
                    if (currentScore > highestScore)
                    {
                        highestScore = currentScore;
                        bestMove = move;
                    }
                    alpha = Math.Max(alpha, highestScore);
                    if (beta <= alpha)
                    { // beta cut-off
                        break;
                    }
                }

                bestMove.Score = highestScore;
                return bestMove;
            }
            else if (state.Player == GameEngine.COMPUTER) // computer's turn  (the random event node)
            {
                bestMove = new ComputerMove();
                List<Cell> availableCells = state.GetAvailableCells();
                List<Move> moves = state.GetAllComputerMoves(availableCells);

                int numSuccessors = moves.Count;
                double upperBound = AI.GetUpperBound();
                double lowerBound = AI.GetLowerBound();

                double curAlpha = numSuccessors * (alpha - upperBound);
                double curBeta = numSuccessors * (beta - lowerBound);

                double sucAlpha = Math.Max(curAlpha, lowerBound);

                double[] probeValues = new double[numSuccessors];

                // probing phase
                double vsum = 0;
                int i = 1;
                foreach (Move move in moves)
                {
                    curBeta += lowerBound;
                    double sucBeta = Math.Min(curBeta, upperBound);

                    State resultingState = state.ApplyMove(move);
                    probeValues[i - 1] = Probe(resultingState, sucAlpha, sucBeta, depth - 1);
                    vsum += probeValues[i - 1];
                    if (probeValues[i - 1] >= curBeta)
                    {
                        vsum += lowerBound * (numSuccessors - i);
                        bestMove.Score = vsum / numSuccessors;
                        return bestMove; // pruning
                    }
                    curBeta -= probeValues[i - 1];
                    i++;
                }

                // search phase
                vsum = 0;
                i = 1;
                foreach (Move move in moves)
                {
                    curAlpha += upperBound;
                    curBeta += probeValues[i - 1];
                    sucAlpha = Math.Max(curAlpha, lowerBound);
                    double sucBeta = Math.Min(curBeta, upperBound);

                    State resultingState = state.ApplyMove(move);
                    double score = StateProbability(((ComputerMove)move).Tile) * Star2Expectimax(resultingState, sucAlpha, sucBeta, depth - 1).Score;
                    vsum += score;

                    if (score <= curAlpha)
                    {
                        vsum += upperBound * (numSuccessors - i);
                        bestMove.Score = vsum / numSuccessors;
                        return bestMove; // pruning
                    }
                    if (score >= curBeta)
                    {
                        vsum += lowerBound * (numSuccessors - i);
                        bestMove.Score = vsum / numSuccessors;
                        return bestMove; // pruning
                    }

                    curAlpha -= score;
                    curBeta -= score;

                    i++;
                }
                bestMove.Score = vsum / numSuccessors;
                return bestMove;
            }
            else
            {
                throw new Exception();
            }
        }

        private double Probe(State state, double alpha, double beta, int depth)
        {

            if (depth == 0 || state.IsGameOver())
            {
                return AI.Evaluate(gameEngine, state);
            }
            else
            {
                State choice = PickSuccessor(state);
                return Star2Expectimax(choice, alpha, beta, depth - 1).Score;
            }
        }


        private State PickSuccessor(State state)
        {
            List<Move> moves = state.GetMoves();

            int numSuccessors = moves.Count;
            if (numSuccessors < 2) return state.ApplyMove(moves[0]);
            else
            {
                State choice = state.ApplyMove(moves[0]);
                double best = AI.Points(choice);
                for (int i = 1; i < numSuccessors; i++)
                {
                    State resultingState = state.ApplyMove(moves[i]);
                    double score = AI.Points(resultingState);
                    if (score > best)
                    {
                        best = score;
                        choice = resultingState;
                    }
                }
                return choice;
            }
        }
    }   
}
