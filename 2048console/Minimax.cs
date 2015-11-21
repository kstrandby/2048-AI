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
    // class used for Minimax searches
    class Minimax
    {
        private const string LOGFILE = @"Log.txt";

        private GameEngine gameEngine;
        private ScoreController scoreController;
        private State currentState;
        private List<Cell> available;
        private int chosenDepth;

        // debugging
        private Logger logger;
        bool debug = false; // switch to turn debug on/off

        public Minimax(GameEngine gameEngine, int depth)
        {
            this.gameEngine = gameEngine;
            this.scoreController = gameEngine.scoreController;
            available = new List<Cell>();
            this.chosenDepth = depth;

            // setup log file if debug is on
            if (debug)
            {
                logger = new Logger(LOGFILE, depth);
            }
        }

        public State RunClassicMinimax(bool print)
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
                Move move = MinimaxAlgorithm(currentState, chosenDepth);
                if (((PlayerMove)move).Direction == (DIRECTION)(-1))
                {
                    // game over
                    if (debug)
                    {
                        logger.WriteLog(true);
                    }
                    return currentState;
                }
                if (debug)
                {
                    logger.WriteLog(true);
                }
                gameEngine.SendUserAction((PlayerMove)move);
            }
        }

        public State RunAlphaBeta(bool print)
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
                Move move = AlphaBeta(currentState, chosenDepth, Double.MinValue, Double.MaxValue);
                if (((PlayerMove)move).Direction == (DIRECTION)(-1))
                {
                    // game over
                    if (debug)
                    {
                        logger.WriteLog(true);
                    }
                    return currentState;
                }
                if (debug)
                {
                    logger.WriteLog(true);
                }
                gameEngine.SendUserAction((PlayerMove)move);
            }
        }

        public State RunIterativeDeepeningAlphaBeta(bool print, int timeLimit)
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
                    if (debug)
                    {
                        logger.WriteLog(true);
                    }
                    return currentState;
                }
                if (debug)
                {
                    logger.WriteLog(true);
                }
                gameEngine.SendUserAction((PlayerMove)move);
            }
        }

        public State RunParallelAlphaBeta(bool print)
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
                Move move = ParallelAlphaBeta(currentState, chosenDepth, Double.MinValue, Double.MaxValue);
                if (((PlayerMove)move).Direction == (DIRECTION)(-1))
                {
                    // game over
                    if (debug)
                    {
                        logger.WriteLog(true);
                    }
                    return currentState;
                }
                if (debug)
                {
                    logger.WriteLog(true);
                }
                gameEngine.SendUserAction((PlayerMove)move);
            }
        }

        public State RunParallelIterativeDeepeningAlphaBeta(bool print, int timeLimit)
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
                Move move = ParallelIterativeDeepening(currentState, timeLimit);
                if (((PlayerMove)move).Direction == (DIRECTION)(-1))
                {
                    // game over
                    return currentState;
                }
                gameEngine.SendUserAction((PlayerMove)move);
            }
        }

        // Runs a parallel alpha-beta search
        // A search is started in a separate thread for each child node
        // Note that pruning is not done across threads
        Move ParallelAlphaBeta(State state, int depth, double alpha, double beta)
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
                double score = AlphaBeta(resultingState, depth - 1, alpha, beta).Score;
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
        private Move ParallelIterativeDeepening(State state, double timeLimit)
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

        // runs an iterative deepening minimax search limited by the given timeLimit
        private Move IterativeDeepening(State state, double timeLimit)
        {
            int depth = 0;
            Stopwatch timer = new Stopwatch();
            Move bestMove = null;
            // start the search
            timer.Start();
            while (timer.ElapsedMilliseconds < timeLimit)
            {
                Tuple<Move, Boolean> result = IterativeDeepeningAlphaBeta(state, depth, Double.MinValue, Double.MaxValue, timeLimit, timer);
                if (result.Item2) bestMove = result.Item1; // only update bestMove if full recursion
                depth++;
            }
            return bestMove;
        }

        // recursive part of the minimax algorithm when used in iterative deepening search
        // checks at each recursion if timeLimit has been reached
        // if is has, it cuts of the search and returns the best move found so far, along with a boolean indicating that the search was not fully completed
        private Tuple<Move, Boolean> IterativeDeepeningAlphaBeta(State state, int depth, double alpha, double beta, double timeLimit, Stopwatch timer)
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
                    currentScore = IterativeDeepeningAlphaBeta(resultingState, depth - 1, alpha, beta, timeLimit, timer).Item1.Score;


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
                double lowestScore = Double.MaxValue, currentScore = Double.MaxValue;

                List<Move> moves = state.GetMoves();

                foreach (Move move in moves)
                {
                    State resultingState = state.ApplyMove(move);
                    currentScore = AlphaBeta(resultingState, depth - 1, alpha, beta).Score;

                    if (currentScore < lowestScore)
                    {
                        lowestScore = currentScore;
                        bestMove = move;
                    }
                    beta = Math.Min(beta, lowestScore);
                    if (beta <= alpha)
                        break;

                    if (timer.ElapsedMilliseconds > timeLimit)
                    {
                        bestMove.Score = lowestScore;
                        return new Tuple<Move, Boolean>(bestMove, false); // recursion not completed, return false
                    }
                }
                bestMove.Score = lowestScore;
                return new Tuple<Move, Boolean>(bestMove, true);
            }
            else throw new Exception();

        }

        // runs minimax with alpha beta pruning
        Move AlphaBeta(State state, int depth, double alpha, double beta)
        {
            Move bestMove;
            if (depth == 0 || state.IsGameOver())
            {
                if (state.Player == GameEngine.PLAYER)
                {
                    bestMove = new PlayerMove();
                    bestMove.Score = AI.Evaluate(gameEngine, state);
                    return bestMove;
                }
                else if (state.Player == GameEngine.COMPUTER)
                {
                    bestMove = new ComputerMove();
                    bestMove.Score = AI.Evaluate(gameEngine, state);
                    return bestMove;
                }
                else throw new Exception();
            }
            if (state.Player == GameEngine.PLAYER)
                return Max(state, depth, alpha, beta);
            else
                return Min(state, depth, alpha, beta);
        }

        // MIN part of Minimax (with alpha-beta pruning)
        Move Min(State state, int depth, double alpha, double beta)
        {

            Move bestMove = new ComputerMove();
            double lowestScore = Double.MaxValue, currentScore = Double.MaxValue;
            
            List<Move> moves = state.GetMoves();

            if (debug)
                logger.writeParent(state, chosenDepth - depth);

            foreach (Move move in moves)
            {
                State resultingState = state.ApplyMove(move);
                currentScore = AlphaBeta(resultingState, depth - 1, alpha, beta).Score;

                if (debug)
                    logger.writeChild(resultingState, chosenDepth - depth, currentScore);

                if (currentScore < lowestScore)
                {
                    lowestScore = currentScore;
                    bestMove = move;
                }
                beta = Math.Min(beta, lowestScore);
                if (beta <= alpha)
                    break;
            }
            bestMove.Score = lowestScore;
            return bestMove;
        }

        // MAX part of Minimax (with alpha-beta pruning)
        Move Max(State state, int depth, double alpha, double beta)
        {
            Move bestMove = new PlayerMove();

            double highestScore = Double.MinValue, currentScore = Double.MinValue;

            List<Move> moves = state.GetMoves();

            if (debug)
                logger.writeParent(state, chosenDepth - depth);
            
            foreach (Move move in moves)
            {
                State resultingState = state.ApplyMove(move);
                currentScore = AlphaBeta(resultingState, depth - 1, alpha, beta).Score;
                
                if (debug)
                    logger.writeChild(resultingState, chosenDepth - depth, currentScore);
                
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

        // Standard Minimax search with no pruning
        Move MinimaxAlgorithm(State state, int depth)
        {
            Move bestMove;

            if (depth == 0 || state.IsGameOver())
            {
                if (state.Player == GameEngine.PLAYER)
                {
                    bestMove = new PlayerMove();
                    bestMove.Score = AI.Evaluate(gameEngine, state);
                    return bestMove;
                }
                else if (state.Player == GameEngine.COMPUTER)
                {
                    bestMove = new ComputerMove();
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
                    currentScore = MinimaxAlgorithm(resultingState, depth - 1).Score;

                    if (currentScore > highestScore)
                    {
                        highestScore = currentScore;
                        bestMove = move;
                    }
                }
                bestMove.Score = highestScore;
                return bestMove;
            }
            else if (state.Player == GameEngine.COMPUTER)
            {
                bestMove = new ComputerMove();

                double lowestScore = Double.MaxValue, currentScore = Double.MaxValue;
                List<Move> moves = state.GetMoves();

                foreach (Move move in moves)
                {
                    State resultingState = state.ApplyMove(move);
                    currentScore = MinimaxAlgorithm(resultingState, depth - 1).Score;

                    if (currentScore < lowestScore)
                    {
                        lowestScore = currentScore;
                        bestMove = move;
                    }
                }
                bestMove.Score = lowestScore;
                return bestMove;
            }
            else throw new Exception();
        }
    }
}
