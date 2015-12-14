using _2048console.GeneticAlgorithm;
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
        //private StreamWriter writer = new StreamWriter(@"expectimaxTiming_d5.txt", true);
        //private int stateExpansions;


        struct TableRow {

            public TableRow(short depth, DIRECTION direction, double value) 
            {
                this.depth = depth;
                this.direction = direction;
                this.value = value;
            }
            public short depth;
            public DIRECTION direction;
            public double value;
        }

        // constants used for transposition table and Zobrist hashing
        private const int NUM_VALUES = 17;
        private int[][][] zobrist_table;
        private ConcurrentDictionary<int,TableRow> transposition_table;

        private GameEngine gameEngine;
        private ScoreController scoreController;
        private int chosenDepth;
        private State currentState;


        public Expectimax(GameEngine game, int depth)
        {
            this.gameEngine = game;
            this.scoreController = gameEngine.scoreController;
            this.chosenDepth = depth;
            transposition_table = new ConcurrentDictionary<int, TableRow>();
            InitializeZobristTable();
            
        }

        private void InitializeZobristTable()
        {
            Random random = new Random();
            zobrist_table = new int[GameEngine.ROWS][][];
            for (int i = 0; i < GameEngine.ROWS; i++)
            {
                for (int j = 0; j < GameEngine.COLUMNS; j++)
                {
                    if (j == 0) zobrist_table[i] = new int[GameEngine.COLUMNS][];
                    for (int k = 0; k < NUM_VALUES; k++)
                    {
                        if (k == 0) zobrist_table[i][j] = new int[NUM_VALUES];
                        zobrist_table[i][j][k] = random.Next(); // random 32-bit integer
                    }
                        
                }
            }
        }

        private int GetHash(State state)
        {
            int hash = 0;
            // loop over board positions
            for (int i = 0; i < GameEngine.COLUMNS; i++)
            {
                for (int j = 0; j < GameEngine.ROWS; j++)
                {
                    if (state.Board[i][j] != 0)
                    {
                        int value = (int)(Math.Log(state.Board[i][j]) / Math.Log(2));
                        hash = hash ^ zobrist_table[i][j][value];
                    }
                }
            }
            return hash;
        }


        // --------------------------------------------
        // Iterative Deepening with Transposition Table and Move Ordering (Star1)
        public State RunTTStar1(bool print, int timeLimit, WeightVector weights)
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
                Move move = TTStar1(currentState, timeLimit, weights);
                if (((PlayerMove)move).Direction == (DIRECTION)(-1))
                {
                    // game over
                    return currentState;
                }
                gameEngine.SendUserAction((PlayerMove)move);
            }
        }

        public Move TTStar1(State state, double timeLimit, WeightVector weights)
        {
            int zob_hash = GetHash(state);
            if (transposition_table.ContainsKey(zob_hash) && transposition_table[zob_hash].depth > 3) return new PlayerMove(transposition_table[zob_hash].direction);

            int depth = 1;
            Stopwatch timer = new Stopwatch();
            Move bestMove = null;
            // start the search
            timer.Start();
            while (true)
            {
                if (timer.ElapsedMilliseconds > timeLimit)
                {
                    if (bestMove == null) // workaround to overcome problem with timer running out too fast with low limits
                    {
                        timeLimit += 10;
                        timer.Restart();
                    }
                    else break;
                }
                Tuple<Move, Boolean> result = RecursiveTTStar1(state, Double.MinValue, Double.MaxValue, depth, timeLimit, timer, weights);
                if (result.Item2)
                {
                    bestMove = result.Item1; // only update bestMove if full recursion
                }
                depth++;
            }

            timer.Stop();

            TableRow row = new TableRow((short)depth, ((PlayerMove)bestMove).Direction, bestMove.Score);
            transposition_table.AddOrUpdate(zob_hash, row, (key, oldValue) => row);

            return bestMove;


        }

        // Recursive part of iterative deepening Expectimax
        private Tuple<Move, Boolean> RecursiveTTStar1(State state, double alpha, double beta, int depth, double timeLimit, Stopwatch timer, WeightVector weights)
        {
            Move bestMove;

            if (depth == 0 || state.IsGameOver())
            {
                if (state.Player == GameEngine.PLAYER)
                {
                    bestMove = new PlayerMove(); // dummy action, as there will be no valid move
                    bestMove.Score = AI.EvaluateWithWeights(state, weights);
                    return new Tuple<Move, Boolean>(bestMove, true);
                }
                else if (state.Player == GameEngine.COMPUTER)
                {
                    bestMove = new ComputerMove(); // dummy action, as there will be no valid move
                    bestMove.Score = AI.EvaluateWithWeights(state, weights);
                    return new Tuple<Move, Boolean>(bestMove, true);
                }
                else throw new Exception();
            }
            if (state.Player == GameEngine.PLAYER) // AI's turn 
            {
                DIRECTION bestDirection = (DIRECTION)(-1);
                bestMove = new PlayerMove();
                double highestScore = Double.MinValue, currentScore = Double.MinValue;

                // transposition table look-up
                int zob_hash = GetHash(state);
                if (transposition_table.ContainsKey(zob_hash) && transposition_table[zob_hash].depth > depth)
                {
                    Move move = new PlayerMove(transposition_table[zob_hash].direction);
                    move.Score = transposition_table[zob_hash].value;
                    return new Tuple<Move, Boolean>(move, true);
                }
                    // move ordering - make sure we first check the move we believe to be best based on earlier searches
                else if (transposition_table.ContainsKey(zob_hash))
                {
                    bestDirection = transposition_table[zob_hash].direction;
                    State resultingState = state.ApplyMove(new PlayerMove(bestDirection));
                    currentScore = RecursiveTTStar1(resultingState, alpha, beta, depth - 1, timeLimit, timer, weights).Item1.Score;

                    if (currentScore > highestScore)
                    {
                        highestScore = currentScore;
                        bestMove = new PlayerMove(bestDirection);
                    }
                    if (timer.ElapsedMilliseconds > timeLimit)
                    {
                        bestMove.Score = highestScore;
                        return new Tuple<Move, Boolean>(bestMove, false); // recursion not completed, return false
                    }
                }

                // now check the rest of moves
                List<Move> moves = state.GetMoves();
                foreach (Move move in moves)
                {
                    if (((PlayerMove)move).Direction != bestDirection)
                    {
                        State resultingState = state.ApplyMove(move);
                        currentScore = RecursiveTTStar1(resultingState, alpha, beta, depth - 1, timeLimit, timer, weights).Item1.Score;

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
                    
                }
                bestMove.Score = highestScore;

                // add result to transposition table
                TableRow row = new TableRow((short)depth, ((PlayerMove)bestMove).Direction, bestMove.Score);
                transposition_table.AddOrUpdate(zob_hash, row, (key, oldValue) => row);
                return new Tuple<Move, Boolean>(bestMove, true);
            }
            else if (state.Player == GameEngine.COMPUTER) // computer's turn  (the random event node)
            {
                bestMove = new ComputerMove();
                int moveCheckedSoFar = 0;

                List<Cell> availableCells = state.GetAvailableCells();
                List<Move> moves = state.GetAllComputerMoves(availableCells);

                int numSuccessors = moves.Count;
                double upperBound = AI.GetUpperBound(weights);
                double lowerBound = AI.GetLowerBound(weights);
                double curAlpha = numSuccessors * (alpha - upperBound) + upperBound;
                double curBeta = numSuccessors * (beta - lowerBound) + lowerBound;

                double scoreSum = 0;
                int i = 1;
                foreach (Move move in moves)
                {
                    double sucAlpha = Math.Max(curAlpha, lowerBound);
                    double sucBeta = Math.Min(curBeta, upperBound);

                    State resultingState = state.ApplyMove(move);

                    double score = StateProbability(((ComputerMove)move).Tile) * RecursiveTTStar1(resultingState, sucAlpha, sucBeta, depth - 1, timeLimit, timer, weights).Item1.Score;
                    scoreSum += score;
                    moveCheckedSoFar++;
                    if (score <= curAlpha)
                    {
                        scoreSum += upperBound * (numSuccessors - i);
                        bestMove.Score = scoreSum / numSuccessors;
                        return new Tuple<Move,bool>(bestMove, true); // pruning
                    }
                    if (score >= curBeta)
                    {
                        scoreSum += lowerBound * (numSuccessors - i);
                        bestMove.Score = scoreSum / numSuccessors;
                        return new Tuple<Move, bool>(bestMove, true); // pruning
                    }
                    if (timer.ElapsedMilliseconds > timeLimit)
                    {
                        bestMove.Score = scoreSum / moveCheckedSoFar;
                        return new Tuple<Move, Boolean>(bestMove, false); // recursion not completed, return false
                    }
                    curAlpha += upperBound - score;
                    curBeta += lowerBound - score;

                    i++;

                }
                bestMove.Score = scoreSum / numSuccessors;
                return new Tuple<Move, bool>(bestMove, true);


            }
            else throw new Exception();
        }



        // --------------------------------------------
        // Iterative Deepening with Transposition Table
        public State RunTTExpectimax(bool print, int timeLimit, WeightVector weights)
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
                Move move = TTExpectimax(currentState, timeLimit, weights);
                if (((PlayerMove)move).Direction == (DIRECTION)(-1))
                {
                    // game over
                    return currentState;
                }
                gameEngine.SendUserAction((PlayerMove)move);
            }
        }

        public Move TTExpectimax(State state, double timeLimit, WeightVector weights)
        {
            int zob_hash = GetHash(state);

            int depth = 1;
            Stopwatch timer = new Stopwatch();
            Move bestMove = null;
            // start the search
            timer.Start();
            while (true)
            {
                if (timer.ElapsedMilliseconds > timeLimit)
                {
                    if (bestMove == null) // workaround to overcome problem with timer running out too fast with low limits
                    {
                        timeLimit += 10;
                        timer.Restart();
                    }
                    else break;
                } 
                Tuple<Move, Boolean> result = RecursiveTTExpectimax(state, depth, timeLimit, timer, weights);
                if (result.Item2)
                {
                    bestMove = result.Item1; // only update bestMove if full recursion
                }
                depth++;
            }

            timer.Stop();
            
            TableRow row = new TableRow((short)depth, ((PlayerMove)bestMove).Direction, bestMove.Score);
            transposition_table.AddOrUpdate(zob_hash, row, (key, oldValue) => row);

            return bestMove;
            
            
        }

        // Recursive part of iterative deepening Expectimax
        private Tuple<Move, Boolean> RecursiveTTExpectimax(State state, int depth, double timeLimit, Stopwatch timer, WeightVector weights)
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
                // transposition table look-up
                int zob_hash = GetHash(state);
                if (transposition_table.ContainsKey(zob_hash) && transposition_table[zob_hash].depth > depth)
                {
                    Move move = new PlayerMove(transposition_table[zob_hash].direction);
                    move.Score = transposition_table[zob_hash].value;
                    return new Tuple<Move, Boolean>(move, true);
                }
                    

                bestMove = new PlayerMove();
                double highestScore = Double.MinValue, currentScore = Double.MinValue;

                List<Move> moves = state.GetMoves();
                foreach (Move move in moves)
                {
                    State resultingState = state.ApplyMove(move);
                    currentScore = RecursiveTTExpectimax(resultingState, depth - 1, timeLimit, timer, weights).Item1.Score;

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

                // add result to transposition table
                TableRow row = new TableRow((short)depth, ((PlayerMove)bestMove).Direction, bestMove.Score);
                transposition_table.AddOrUpdate(zob_hash, row, (key, oldValue) => row);
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

                    average += StateProbability(((ComputerMove)move).Tile) * RecursiveTTExpectimax(resultingState, depth - 1, timeLimit, timer, weights).Item1.Score;
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

        // ------------------------------------------

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
                Stopwatch timer = new Stopwatch();
                timer.Start();
                Move move = ExpectimaxAlgorithm(currentState, chosenDepth);
                timer.Stop();
                //writer.WriteLine(timer.ElapsedMilliseconds);
                if (((PlayerMove)move).Direction == (DIRECTION)(-1))
                {
                  //  writer.Close();

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

        public State RunIterativeDeepeningExpectimax(bool print, int timeLimit, WeightVector weights)
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
                Move move = IterativeDeepening(currentState, timeLimit, weights);
                if (((PlayerMove)move).Direction == (DIRECTION)(-1))
                {
                    // game over
                    return currentState;
                }
                gameEngine.SendUserAction((PlayerMove)move);
            }
        }

        public State RunParallelIterativeDeepeningExpectimax(bool print, int timeLimit, WeightVector weights)
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
                Move move = ParallelIterativeDeepeningExpectimax(currentState, timeLimit, weights);
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
        private Move ParallelIterativeDeepeningExpectimax(State state, int timeLimit, WeightVector weights)
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
                double score = IterativeDeepening(resultingState, timeLimit, weights).Score;
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
        private Move IterativeDeepening(State state, double timeLimit, WeightVector weights)
        {
            int depth = 0;
            Stopwatch timer = new Stopwatch();
            Move bestMove = null;
            // start the search
            timer.Start();
            while (timer.ElapsedMilliseconds < timeLimit)
            {
                Tuple<Move, Boolean> result = IterativeDeepeningExpectimax(state, depth, timeLimit, timer, weights);
                if (result.Item2) bestMove = result.Item1; // only update bestMove if full recursion
                depth++;
                

            }
            return bestMove;
        }

        // Recursive part of iterative deepening Expectimax
        private Tuple<Move, Boolean> IterativeDeepeningExpectimax(State state, int depth, double timeLimit, Stopwatch timer, WeightVector weights)
        {
            Move bestMove;

            if (depth == 0 || state.IsGameOver())
            {
                if (state.Player == GameEngine.PLAYER)
                {
                    bestMove = new PlayerMove(); // dummy action, as there will be no valid move
                    bestMove.Score = AI.EvaluateWithWeights(state, weights);
                    return new Tuple<Move, Boolean>(bestMove, true);
                }
                else if (state.Player == GameEngine.COMPUTER)
                {
                    bestMove = new ComputerMove(); // dummy action, as there will be no valid move
                    bestMove.Score = AI.EvaluateWithWeights(state, weights);
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
                    currentScore = IterativeDeepeningExpectimax(resultingState, depth - 1, timeLimit, timer, weights).Item1.Score;
                    
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

                    average += StateProbability(((ComputerMove)move).Tile) * IterativeDeepeningExpectimax(resultingState, depth - 1, timeLimit, timer, weights).Item1.Score;
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
