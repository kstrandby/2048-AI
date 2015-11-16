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

        private int evaluatedStates;

        public State Run(bool print)
        {
            //StreamWriter writer = new StreamWriter(@"C:\Users\Kristine\Documents\Visual Studio 2013\Projects\2048console\ExpectimaxTimeSpentPerMove.txt", true);
            // main game loop
            while (true)
            {

                // update state
                currentState = new State(GridHelper.CloneGrid(gameEngine.grid), scoreController.getScore(), GameEngine.PLAYER);

                if (print)
                {
                    Program.CleanConsole();
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine(GridHelper.ToString(currentState.Grid));
                }
                evaluatedStates = 0;
                Stopwatch timer = new Stopwatch();
                Stopwatch timer2 = new Stopwatch();
                timer.Start();
                // run algorithm and send action choice to game engine
                //Move move = Star2Expectimax(currentState, AI.GetLowerBound(), AI.GetUpperBound(), chosenDepth);
                //Move move = Star1Expectimax(currentState, Double.MinValue, Double.MaxValue, chosenDepth);
                //Move move = ParallelExpectimax(currentState, chosenDepth);
                //writer.WriteLine("{0,0}{1,10}", chosenDepth, evaluatedStates);
                Move move1 = IterativeDeepening(currentState, 100);
                //Move move = ThreadingStar1(currentState, chosenDepth);

                timer.Stop();

                timer2.Start();
                Move move = ParallelIterativeDeepeningExpectimax(currentState, 100);

                timer2.Stop();

                //Console.WriteLine("Move chosen by IDE: " + ((PlayerMove)move).Direction);
                //Console.WriteLine("Move chosen by PIDE: " + ((PlayerMove)move1).Direction);
                if (((PlayerMove)move).Direction != ((PlayerMove)move1).Direction)
                {
                    //Console.Write("");
                }

                long time = timer.ElapsedMilliseconds;
                //writer.WriteLine(time);
                
                if (((PlayerMove)move).Direction == (DIRECTION)(-1))
                {
                    // game over
                    Console.WriteLine("GAME OVER, final score = " + scoreController.getScore());
                    //writer.Close();
                    return currentState;
                }
                gameEngine.SendUserAction((PlayerMove)move);

                if (print)
                {
                    Program.CleanConsole();
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine(GridHelper.ToString(currentState.Grid));
                }
            }
        }

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

        // called only at the root, where depth will always be > 0 and player will always be player
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

        // called only at the root, where depth will always be > 0 and player will always be player
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

        private Move ParallelIterativeDeepeningExpectimax(State state, int timeLimit)
        {

            int depth = 1;
            Stopwatch timer = new Stopwatch();

            Move bestMove = new PlayerMove();

            List<Move> moves = state.GetMoves();
            ConcurrentBag<Tuple<double, Move>> scores = new ConcurrentBag<Tuple<double, Move>>();

            if (moves.Count == 0) // game over
            {
                return bestMove;
            }

            

            timer.Start();
            while (timer.ElapsedMilliseconds < timeLimit)
            {
                // create the resulting states before starting the threads
                List<State> resultingStates = new List<State>();
                foreach (Move move in moves)
                {
                    State resultingState = state.ApplyMove(move);
                    resultingStates.Add(resultingState);
                }

                Parallel.ForEach(resultingStates, resultingState =>
                {
                    Tuple<Move, Boolean> result = IterativeDeepeningExpectimax(resultingState, depth - 1, timeLimit, timer);
                    if(result.Item2) scores.Add(new Tuple<double, Move>(result.Item1.Score, resultingState.GeneratingMove));
                });

                // find the best score
                double highestScore = Double.MinValue;
                foreach (Tuple<double, Move> score in scores)
                {
                    
                    if (score.Item1 > highestScore)
                    {
                        highestScore = score.Item1;
                        bestMove = score.Item2;
                    }
                }
                scores = new ConcurrentBag<Tuple<double, Move>>();
                depth++;
            }
            //Console.WriteLine("Depth reached in PIDE: " + depth);

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
                Tuple<Move, Boolean> result = IterativeDeepeningExpectimax(state, depth, timeLimit, timer);
                if (result.Item2) bestMove = result.Item1; // only update bestMove if full recursion
                depth++;
                

            }
            //Console.WriteLine("Depth reaced in IDE: " + depth);
            return bestMove;
        }

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

                    average += StateProbability(resultingState, ((ComputerMove)move).Tile, availableCells.Count) * IterativeDeepeningExpectimax(resultingState, depth - 1, timeLimit, timer).Item1.Score;
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
                    //if(depth == chosenDepth) Console.WriteLine("Expectimax: Score for " + ((PlayerMove)move).Direction + " : " + currentScore);
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

                    double score = StateProbability(resultingState, ((ComputerMove)move).Tile, availableCells.Count) * Star1Expectimax(resultingState, sucAlpha, sucBeta, depth - 1).Score;
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
                    double score = StateProbability(resultingState, ((ComputerMove)move).Tile, availableCells.Count) * Star2Expectimax(resultingState, sucAlpha, sucBeta, depth - 1).Score;
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
