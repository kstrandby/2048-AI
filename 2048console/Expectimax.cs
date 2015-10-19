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
                //Move action = Star1Expectimax(currentState, Double.MinValue, Double.MaxValue, depth, GameEngine.PLAYER);
                Move move = ExpectimaxAlgorithm(currentState, chosenDepth);
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


        /*

        private Move Star1Expectimax(State state, double alpha, double beta, int depth)
        {
            Move bestMove;

            if (depth == 0 || state.IsGameOver())
            {
                if (state.Player == GameEngine.PLAYER)
                {
                    bestMove = new PlayerMove(); // dummy action, as there will be no valid move
                }
                else if (state.Player == GameEngine.COMPUTER)
                {
                    bestMove = new ComputerMove(); // dummy action, as there will be no valid move
                }
                else throw new Exception();
            }
            if (state.Player == GameEngine.PLAYER)
            {
                double highestScore = Double.MinValue, currentScore = Double.MinValue;

                List<Move> moves = state.GetMoves();
                foreach (ACTION move in moves)
                {
                    State resultingState = AI.PlayerResult(state, move);
                    currentScore = Star1Expectimax(resultingState, alpha, beta, depth - 1, GameEngine.COMPUTER).score;
                    if (currentScore > highestScore)
                    {
                        highestScore = currentScore;
                        bestAction = move;
                    }
                    alpha = Math.Max(alpha, highestScore);
                    if (beta <= alpha)
                    { // beta cut-off
                        break;
                    }
                }

                bestMove.playerAction = bestAction;
                bestMove.score = highestScore;
                return bestMove;
            }
            else if (player == AI.COMPUTER) // computer's turn  (the random event node)
            {
                // return the weighted average of all the child nodes's scores
                List<Cell> availableCells = AI.getAvailableCells(state);
                List<int[]> actions = AI.getAllComputerMoves(state, availableCells);
                int numSuccessors = actions.Count;
                int highestTile = AI.findHighestTile(state);
                double upperBound = AI.GetUpperBound(highestTile);
                double lowerBound = AI.GetLowerBound(highestTile);
                double curAlpha = numSuccessors * (alpha - upperBound) + upperBound;
                double curBeta = numSuccessors * (beta - lowerBound) + lowerBound;

                double scoreSum = 0;
                int i = 1;
                foreach (int[] action in actions)
                {
                    double sucAlpha = Math.Max(curAlpha, lowerBound);
                    double sucBeta = Math.Min(curBeta, upperBound);

                    State resultingState = AI.ComputerResult(state, action);

                    double score = Star1Expectimax(resultingState, sucAlpha, sucBeta, _depth - 1, AI.AI_PLAYER).score;
                    scoreSum += score;
                    if (score <= curAlpha)
                    {
                        scoreSum += upperBound * (numSuccessors - i);
                        bestMove.score = scoreSum / numSuccessors;
                        return bestMove; // pruning
                    }
                    if (score >= curBeta)
                    {
                        scoreSum += lowerBound * (numSuccessors - i);
                        bestMove.score = scoreSum / numSuccessors;
                        return bestMove; // pruning
                    }
                    curAlpha += upperBound - score;
                    curBeta += lowerBound - score;

                    i++;
                    
                }
                bestMove.score = scoreSum / numSuccessors;
            }
            return bestMove;
        }
         */

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
