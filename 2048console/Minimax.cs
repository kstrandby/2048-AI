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
    class Minimax
    {
        public GameEngine gameEngine;
        public ScoreController scoreController;

        public State currentState;

        public List<Cell> available;

        private int chosenDepth;

        // debugging
        private Logger logger;
        bool debug = false; // switch to turn debug on/off

        public bool running;


        public Minimax(GameEngine gameEngine, int depth)
        {
            this.gameEngine = gameEngine;
            this.scoreController = gameEngine.scoreController;
            available = new List<Cell>();
            this.chosenDepth = depth;

            // setup log file if debug is on
            if (debug)
            {
                logger = new Logger(@"C:\Users\Kristine\Desktop\log.txt", depth);
            }
        }


        public State Run(bool print)
        {
            running = true;
            //StreamWriter writer = new StreamWriter(@"C:\Users\Kristine\Documents\Visual Studio 2013\Projects\2048console\MinimaxIterativeDeepeningTL100.txt", true);

            // run main game loop
            while (true)
            {
                // update state
                currentState = new State(GridHelper.CloneGrid(gameEngine.grid), scoreController.getScore(), GameEngine.PLAYER);

                

                Stopwatch timer = new Stopwatch();
                timer.Start();
                // run algorithm and send choice action to game engine
                //Move move = AlphaBeta(currentState, chosenDepth, Double.MinValue, Double.MaxValue);
                //Move move = ThreadingAlphaBeta(currentState, chosenDepth, Double.MinValue, Double.MaxValue);
                Move move = IterativeDeepening(currentState, 100);
                
                //Move move = MinimaxAlgorithm(currentState, chosenDepth);
                timer.Stop();
                //writer.WriteLine("{0,0}", timer.ElapsedMilliseconds);
                if (((PlayerMove)move).Direction== (DIRECTION)(-1))
                {
                    // game over
                    Console.WriteLine("GAME OVER, final score = " + scoreController.getScore());
                    //writer.Close();
                    running = false;
                    if (debug)
                    {
                       logger.WriteLog(true);
                    }
                    return currentState;
                }

                gameEngine.SendUserAction((PlayerMove)move);

                if (debug)
                {
                    logger.WriteLog(false);
                }
                if (print)
                {
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine(GridHelper.ToString(currentState.Grid));
                    Thread.Sleep(500);
                }
            }
           
        }

        Move ThreadingAlphaBeta(State state, int depth, double alpha, double beta)
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
