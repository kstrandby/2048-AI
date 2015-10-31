using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace _2048console
{
    class Expectimax
    {

        private GameEngine gameEngine;
        public ScoreController scoreController;

        private int chosenDepth;
        private State currentState;

        public Expectimax(GameEngine game, int depth)
        {
            this.gameEngine = game;
            this.scoreController = gameEngine.scoreController;
            this.chosenDepth = depth;
        }

        public State Run(bool print)
        {

            // main game loop
            while (true)
            {
                // update state
                currentState = new State(GridHelper.CloneGrid(gameEngine.grid), scoreController.getScore(), GameEngine.PLAYER);

                // run algorithm and send action choice to game engine
                Move move = Star1Expectimax(currentState, Double.MinValue, Double.MaxValue, chosenDepth);
                //Move move = ExpectimaxAlgorithm(currentState, chosenDepth);
                if (((PlayerMove)move).Direction == (DIRECTION)(-1))
                {
                    // game over
                    Console.WriteLine("GAME OVER, final score = " + scoreController.getScore());
                    return currentState;
                }
                gameEngine.SendUserAction((PlayerMove)move);

                if (print)
                {
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine(GridHelper.ToString(currentState.Grid));
                }
            }

        }


        private Move Star1Expectimax(State state, double alpha, double beta, int depth)
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
                //int highestTile = GridHelper.HighestTile(state.Grid);
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

                    double score = Star1Expectimax(resultingState, sucAlpha, sucBeta, depth - 1).Score;
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


        private Move ExpectimaxAlgorithm(State state, int depth)
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

                    average += StateProbability(resultingState, ((ComputerMove)move).Tile, availableCells.Count) * ExpectimaxAlgorithm(resultingState, depth - 1).Score;
                }
                bestMove.Score = average / moves.Count;
                return bestMove;
            }
            else throw new Exception();
        }

        private double StateProbability(State resultingState, int tileValue, int numAvailableCells)
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
    }   
}
