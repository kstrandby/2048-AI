using System;
using System.Collections.Generic;
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

            // run main game loop
            while (true)
            {
                // update state
                currentState = new State(GridHelper.CloneGrid(gameEngine.grid), scoreController.getScore(), GameEngine.PLAYER);

                // run algorithm and send choice action to game engine
                Move move = AlphaBeta(currentState, chosenDepth, Double.MinValue, Double.MaxValue);
                if (((PlayerMove)move).Direction== (DIRECTION)(-1))
                {
                    // game over
                    Console.WriteLine("GAME OVER, final score = " + scoreController.getScore());
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
    }
}
