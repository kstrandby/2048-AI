using _2048console.GeneticAlgorithm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace _2048console
{
    class Program
    {
        // log files
        private const string MINIMAX_LOG_FILE = @"MINIMAX_LOG.txt";
        private const string EXPECTIMAX_LOG_FILE = @"EXPECTIMAX_LOG.txt";
        private const string MCTS_LOG_FILE = @"MCTS_LOG.txt";
        private const string HEURISTICLEARNING_LOG_FILE = @"HL_LOG.txt";
        private const string RANDOM_LOG_FILE = @"RANDOM_LOG.txt";
        
        private const int NUM_THREADS = 8; // adjust based how many cores you have
        
        // weights
        private static WeightVectorAll GAweights = new WeightVectorAll { Corner = 0, Empty_cells = 0, Highest_tile = 0, Monotonicity = 0, Points = 0, Smoothness = 0.71618, Snake = 16.68341, Trapped_penalty = 2.2706 };
        private static WeightVectorAll MANweights = new WeightVectorAll { Corner = 0, Empty_cells = 0.5, Highest_tile = 1.0, Monotonicity = 1.0, Points = 0, Smoothness = 0.1, Snake = 0, Trapped_penalty = 2 };
        private static WeightVectorAll weights = new WeightVectorAll { Corner = 0, Empty_cells = 0, Highest_tile = 0, Monotonicity = 0, Points = 0, Smoothness = 0, Snake = 1, Trapped_penalty = 0 };

        // enum to hold all types of searches
        enum AI_TYPE
        {
            CLASSIC_MINIMAX,
            ALPHA_BETA,
            ITERATIVE_DEEPENING_ALPHA_BETA,
            PARALLEL_ALPHA_BETA,
            PARALLEL_ITERATIVE_DEEPENING_ALPHA_BETA,
            CLASSIC_EXPECTIMAX,
            EXPECTIMAX_STAR1,
            EXPECTIMAX_STAR1_FW_PRUNING,
            ITERATIVE_DEEPENING_EXPECTIMAX,
            PARALLEL_EXPECTIMAX,
            PARALLEL_ITERATIVE_DEEPENING_EXPECTIMAX,
            EXPECTIMAX_WITH_ALL_IMPROVEMENTS,
            EXPECTIMAX_WITH_ALL_IMPROVEMENTS_NO_FORWARDPRUNING,
            TT_ITERATIVE_DEEPENING_EXPECTIMAX,
            TT_ITERATIVE_DEEPENING_STAR1,
            ITERATION_LIMITED_MCTS,
            TIME_LIMITED_MCTS,
            ROOT_PARALLEL_ITERATION_LIMITED_MCTS,
            ROOT_PARALLEL_TIME_LIMITED_MCTS,
            EXPECTIMAX_MCTS_TIME_LIMITED,
            EXPECTIMAX_MCTS_WITH_SIMULATIONS_TIME_LIMITED,
            FINAL_COMBI
        }

        static void Main(string[] args)
        {
            ShowMenu(); 
            Console.ReadLine();
        }

        // Shows a menu in console letting user choose AI (or play the game)
        private static void ShowMenu()
        {
            int choice = GetChoice("0: Play 2048\n1: Minimax\n2: Expectimax\n3: Monte Carlo Tree Search\n4: Heuristic Learning\n5: Random game");
            if (choice == 0) StartGame();
            else if (choice == 1) RunMinimax();
            else if (choice == 2) RunExpectimax();
            else if (choice == 3) RunMonteCarlo();
            else if (choice == 4) RunHeuristicLearning();
            else if (choice == 5) RunRandomGame();
            else
            {
                Console.WriteLine("Sorry, please choose between the options shown:");
                ShowMenu();
            }
        }

        // Runs random games
        private static void RunRandomGame()
        {
            int choice = GetChoice("1: Graphic run\n2: Test runs");
            
            if (choice == 1) // graphic game
            {
                GameEngine game = new GameEngine();
                Naive naive = new Naive(game);
                naive.RunRandomPlay(true);
            }
            else // test runs
            {
                int runs = GetChoice("Choose number of runs: ");
                StreamWriter writer = new StreamWriter(RANDOM_LOG_FILE, true);
                Dictionary<int, int> highTileCount = new Dictionary<int, int>() { { 64, 0 }, { 128, 0 }, { 256, 0 }, { 512, 0 }, { 1024, 0 }, { 2048, 0 }, { 4096, 0 }, { 8192, 0 }, { 16384, 0 }, { 32768, 0 } };
                int totalScore = 0;

                for (int i = 0; i < runs; i++)
                {
                    GameEngine game = new GameEngine();
                    Naive naive = new Naive(game);
                    State endState = naive.RunRandomPlay(false);

                    // note highest tile and points
                    int highestTile = BoardHelper.HighestTile(endState.Board);
                    int points = endState.Points;
                    totalScore += points;

                    // write stats
                    String stats = i + ":\t" + highestTile + "\t" + points + "\t";
                    Console.WriteLine(stats);
                    writer.WriteLine(stats);

                    // keep track of high cards
                    List<int> keys = new List<int>(highTileCount.Keys);
                    for (int j = 0; j < keys.Count; j++)
                    {
                        if (highestTile >= keys[j]) highTileCount[keys[j]]++;
                    }
                    Thread.Sleep(1000);
                }
                writer.Close();
                Console.WriteLine(GetStatistics(highTileCount, runs, totalScore));
            }
            Console.ReadLine();
        }

        // Run games using MCTS comined with Expectimax AIs
        private static void RunHeuristicLearning()
        {
            AI_TYPE type = GetHeuristicLearningType();
            int choice = GetChoice("1: Graphic run\n2: Test runs");
            
            if (choice == 1) // graphic game
            {
                int depth = GetChoice("Depth?");
                int timeLimit = GetChoice("Time limit? (in ms)");
                CleanConsole();
                RunAIGame(type, true, depth, timeLimit);
            }
            else if (choice == 2) // test runs
            {
                int runs = GetChoice("Choose number of runs: ");
                StreamWriter writer = new StreamWriter(HEURISTICLEARNING_LOG_FILE, true);
                Dictionary<int, int> highTileCount = new Dictionary<int, int>() { { 512, 0 }, { 1024, 0 }, { 2048, 0 }, { 4096, 0 }, { 8192, 0 }, { 16384, 0 }, { 32768, 0 } };
                int totalScore = 0;

                if (type == AI_TYPE.EXPECTIMAX_MCTS_TIME_LIMITED || type == AI_TYPE.EXPECTIMAX_MCTS_WITH_SIMULATIONS_TIME_LIMITED || type == AI_TYPE.FINAL_COMBI)
                {
                    int depth = GetChoice("Depth?");
                    int timeLimit = GetChoice("Time limit? (in ms)");
                    for (int i = 0; i < runs; i++)
                    {
                        // time run
                        Stopwatch timer = new Stopwatch();
                        timer.Start();
                        State endState = RunAIGame(type, false, depth, timeLimit);
                        timer.Stop();
                        long elapsedMs = timer.ElapsedMilliseconds;

                        // note highest tile and points
                        int highestTile = BoardHelper.HighestTile(endState.Board);
                        int points = endState.Points;
                        totalScore += points;

                        // write stats
                        String stats = i + ":\t" + depth + "\t" + highestTile + "\t" + points + "\t" + elapsedMs;
                        Console.WriteLine(stats);
                        writer.WriteLine(stats);

                        // keep track of high cards
                        List<int> keys = new List<int>(highTileCount.Keys);
                        for (int j = 0; j < keys.Count; j++)
                        {
                            if (highestTile >= keys[j]) highTileCount[keys[j]]++;
                        }
                    }
                    writer.Close();
                    Console.WriteLine(GetStatistics(highTileCount, runs, totalScore));
                }
            }
            else RunHeuristicLearning();
        }

        // Runs Minimax games
        private static void RunMinimax()
        {
            AI_TYPE minimaxType = GetMinimaxType();
            int choice = GetChoice("1: Graphic run\n2: Test runs");
            
            // Graphic run
            if (choice == 1)
            {
                if (minimaxType == AI_TYPE.CLASSIC_MINIMAX || minimaxType == AI_TYPE.ALPHA_BETA || minimaxType == AI_TYPE.PARALLEL_ALPHA_BETA)
                {
                    int depth = GetChoice("Depth?");
                    CleanConsole();
                    RunAIGame(minimaxType, true, depth);
                }
                else // minimaxType == AI_TYPE.ITERATIVE_DEEPENING_ALPHA_BETA
                {
                    int timeLimit = GetChoice("Time limit? (in ms)");
                    CleanConsole();
                    RunAIGame(minimaxType, true, 0, timeLimit);
                }
            }

            // Test runs
            else if (choice == 2)
            {
                int runs = GetChoice("Choose number of runs: ");
                StreamWriter writer = new StreamWriter(MINIMAX_LOG_FILE, true);
                Dictionary<int, int> highTileCount = new Dictionary<int, int>() { {512, 0}, { 1024, 0 }, { 2048, 0 }, { 4096, 0 }, { 8192, 0 }, { 16384, 0 }, { 32768, 0 } };
                int totalScore = 0;

                if (minimaxType == AI_TYPE.CLASSIC_MINIMAX || minimaxType == AI_TYPE.ALPHA_BETA || minimaxType == AI_TYPE.PARALLEL_ALPHA_BETA)
                {
                    int depth = GetChoice("Depth?");
                    for (int i = 0; i < runs; i++)
                    {
                        // time run
                        Stopwatch timer = new Stopwatch();
                        timer.Start();
                        State endState = RunAIGame(minimaxType, false, depth);
                        timer.Stop();
                        long elapsedMs = timer.ElapsedMilliseconds;

                        // note highest tile and points
                        int highestTile = BoardHelper.HighestTile(endState.Board);
                        int points = endState.Points;
                        totalScore += points;

                        // write stats
                        String stats = i + ":\t" + depth + "\t" + highestTile + "\t" + points + "\t" + elapsedMs;
                        Console.WriteLine(stats);
                        writer.WriteLine(stats);

                        List<int> keys = new List<int>(highTileCount.Keys);
                        for (int j = 0; j < keys.Count; j++)
                        {
                            if (highestTile >= keys[j]) highTileCount[keys[j]]++;
                        }
                    }
                    writer.Close();
                    Console.WriteLine(GetStatistics(highTileCount, runs, totalScore));
                        
                }
                else if(minimaxType == AI_TYPE.ITERATIVE_DEEPENING_ALPHA_BETA || minimaxType == AI_TYPE.PARALLEL_ITERATIVE_DEEPENING_ALPHA_BETA)
                {
                    int timeLimit = GetChoice("Time limit? (in ms)");

                    for (int i = 0; i < runs; i++)
                    {
                        // time run
                        Stopwatch timer = new Stopwatch();
                        timer.Start();
                        State endState = RunAIGame(minimaxType, false, 0, timeLimit);
                        timer.Stop();
                        long elapsedMs = timer.ElapsedMilliseconds;

                        // note highest tile and points
                        int highestTile = BoardHelper.HighestTile(endState.Board);
                        int points = endState.Points;
                        totalScore += points;

                        // write stats
                        String stats = i + ":\t" + timeLimit + "\t" + highestTile + "\t" + points;
                        Console.WriteLine(stats);
                        writer.WriteLine(stats);

                        List<int> keys = new List<int>(highTileCount.Keys);
                        for (int j = 0; j < keys.Count; j++)
                        {
                            if (highestTile >= keys[j]) highTileCount[keys[j]]++;
                        }
                    }
                    writer.Close();
                    Console.WriteLine(GetStatistics(highTileCount, runs, totalScore));
                }
            }
            Console.ReadLine();
        }

        // Runs Expectimax algorithm games
        private static void RunExpectimax()
        {
            AI_TYPE expectimaxType = GetExpectimaxType();
            int choice = GetChoice("1: Graphic run\n2: Test runs");

            // Graphic run
            if (choice == 1)
            {
                if (expectimaxType == AI_TYPE.CLASSIC_EXPECTIMAX || expectimaxType == AI_TYPE.EXPECTIMAX_STAR1 || expectimaxType == AI_TYPE.PARALLEL_EXPECTIMAX || expectimaxType == AI_TYPE.EXPECTIMAX_STAR1_FW_PRUNING)
                {
                    int depth = GetChoice("Depth?");
                    CleanConsole();
                    RunAIGame(expectimaxType, true, depth);
                }
                else if(expectimaxType == AI_TYPE.ITERATIVE_DEEPENING_EXPECTIMAX || expectimaxType == AI_TYPE.PARALLEL_ITERATIVE_DEEPENING_EXPECTIMAX
                    || expectimaxType == AI_TYPE.TT_ITERATIVE_DEEPENING_EXPECTIMAX || expectimaxType == AI_TYPE.TT_ITERATIVE_DEEPENING_STAR1
                    || expectimaxType == AI_TYPE.EXPECTIMAX_WITH_ALL_IMPROVEMENTS || expectimaxType == AI_TYPE.EXPECTIMAX_WITH_ALL_IMPROVEMENTS_NO_FORWARDPRUNING)
                {
                    int timeLimit = GetChoice("Time limit? (in ms)");
                    CleanConsole();
                    RunAIGame(expectimaxType, true, 0, timeLimit);
                }
            }

            else if (choice == 2)
            {
                int runs = GetChoice("Choose number of runs: ");
                StreamWriter writer = new StreamWriter(EXPECTIMAX_LOG_FILE, true);
                Dictionary<int, int> highTileCount = new Dictionary<int, int>() { { 512, 0 }, { 1024, 0 }, { 2048, 0 }, { 4096, 0 }, { 8192, 0 }, { 16384, 0 }, { 32768, 0 } };
                int totalScore = 0;

                if (expectimaxType == AI_TYPE.CLASSIC_EXPECTIMAX || expectimaxType == AI_TYPE.EXPECTIMAX_STAR1 || expectimaxType == AI_TYPE.PARALLEL_EXPECTIMAX || expectimaxType == AI_TYPE.EXPECTIMAX_STAR1_FW_PRUNING)
                {
                    int depth = GetChoice("Depth?");
                    for (int i = 0; i < runs; i++)
                    {
                        // time run
                        Stopwatch timer = new Stopwatch();
                        timer.Start();
                        State endState = RunAIGame(expectimaxType, false, depth);
                        timer.Stop();
                        long elapsedMs = timer.ElapsedMilliseconds;

                        // note highest tile and points
                        int highestTile = BoardHelper.HighestTile(endState.Board);
                        int points = endState.Points;
                        totalScore += points;

                        // write stats
                        String stats = i + ":\t" + depth + "\t" + highestTile + "\t" + points + "\t" + elapsedMs;
                        Console.WriteLine(stats);
                        writer.WriteLine(stats);

                        List<int> keys = new List<int>(highTileCount.Keys);
                        for (int j = 0; j < keys.Count; j++)
                        {
                            if (highestTile >= keys[j]) highTileCount[keys[j]]++;
                        }
                    }
                    writer.Close();
                    Console.WriteLine(GetStatistics(highTileCount, runs, totalScore));
                }
                else if (expectimaxType == AI_TYPE.ITERATIVE_DEEPENING_EXPECTIMAX || expectimaxType == AI_TYPE.PARALLEL_ITERATIVE_DEEPENING_EXPECTIMAX
                    || expectimaxType == AI_TYPE.TT_ITERATIVE_DEEPENING_EXPECTIMAX || expectimaxType == AI_TYPE.TT_ITERATIVE_DEEPENING_STAR1
                    || expectimaxType == AI_TYPE.EXPECTIMAX_WITH_ALL_IMPROVEMENTS || expectimaxType == AI_TYPE.EXPECTIMAX_WITH_ALL_IMPROVEMENTS_NO_FORWARDPRUNING)
                {
                    int timeLimit = GetChoice("Time limit? (in ms)");

                    for (int i = 0; i < runs; i++)
                    {
                        // time run
                        Stopwatch timer = new Stopwatch();
                        timer.Start();
                        State endState = RunAIGame(expectimaxType, false, 0, timeLimit);
                        timer.Stop();
                        long elapsedMs = timer.ElapsedMilliseconds;

                        // note highest tile and points
                        int highestTile = BoardHelper.HighestTile(endState.Board);
                        int points = endState.Points;
                        totalScore += points;

                        // write stats
                        String stats = i + ":\t" + timeLimit + "\t" + highestTile + "\t" + points;
                        Console.WriteLine(stats);
                        writer.WriteLine(stats);

                        List<int> keys = new List<int>(highTileCount.Keys);
                        for (int j = 0; j < keys.Count; j++)
                        {
                            if (highestTile >= keys[j]) highTileCount[keys[j]]++;
                        }
                    }
                    writer.Close();
                    Console.WriteLine(GetStatistics(highTileCount, runs, totalScore));
                }
            }
            Console.ReadLine();
        }

        // Runs MCTS algorithm games
        private static void RunMonteCarlo()
        {
            AI_TYPE mctsType = GetMonteCarloTreeSearchType();
            int choice = GetChoice("1: Graphic run\n2: Test runs");
            
            // Graphic run
            if (choice == 1)
            {
                if (mctsType == AI_TYPE.TIME_LIMITED_MCTS || mctsType == AI_TYPE.ROOT_PARALLEL_TIME_LIMITED_MCTS)
                {
                    int timeLimit = GetChoice("Time limit?");
                    CleanConsole();
                    RunAIGame(mctsType, true, 0, timeLimit);
                }
                else if (mctsType == AI_TYPE.ITERATION_LIMITED_MCTS || mctsType == AI_TYPE.ROOT_PARALLEL_ITERATION_LIMITED_MCTS)
                {
                    int iterationLimit = GetChoice("Iteration limit?");
                    CleanConsole();
                    RunAIGame(mctsType, true, 0, 0, iterationLimit);
                }
            }

            // Test runs
            else if (choice == 2)
            {
                int runs = GetChoice("Choose number of runs: ");
                StreamWriter writer = new StreamWriter(MCTS_LOG_FILE, true);
                Dictionary<int, int> highTileCount = new Dictionary<int, int>() { { 512, 0 }, { 1024, 0 }, { 2048, 0 }, { 4096, 0 }, { 8192, 0 }, { 16384, 0 }, { 32768, 0 } };
                int totalScore = 0;

                if (mctsType == AI_TYPE.TIME_LIMITED_MCTS || mctsType == AI_TYPE.ROOT_PARALLEL_TIME_LIMITED_MCTS)
                {
                    int timeLimit = GetChoice("Time limit?");
                    for (int i = 0; i < runs; i++)
                    {
                        // time run
                        Stopwatch timer = new Stopwatch();
                        timer.Start();
                        State endState = RunAIGame(mctsType, false, 0, timeLimit);
                        timer.Stop();
                        long elapsedMs = timer.ElapsedMilliseconds;

                        // note highest tile and points
                        int highestTile = BoardHelper.HighestTile(endState.Board);
                        int points = endState.Points;
                        totalScore += points;
                        // write stats
                        String stats = i + ":\t" + timeLimit + "\t" + highestTile + "\t" + points;
                        Console.WriteLine(stats);
                        writer.WriteLine(stats);

                        List<int> keys = new List<int>(highTileCount.Keys);
                        for (int j = 0; j < keys.Count; j++)
                        {
                            if (highestTile >= keys[j]) highTileCount[keys[j]]++;
                        }
                    }
                    writer.Close();
                    Console.WriteLine(GetStatistics(highTileCount, runs, totalScore));
                }
                else if (mctsType == AI_TYPE.ITERATION_LIMITED_MCTS || mctsType == AI_TYPE.ROOT_PARALLEL_ITERATION_LIMITED_MCTS)
                {
                    int iterationLimit = GetChoice("Iteration limit?");

                    for (int i = 0; i < runs; i++)
                    {
                        // time run
                        Stopwatch timer = new Stopwatch();
                        timer.Start();
                        State endState = RunAIGame(mctsType, false, 0, 0, iterationLimit);
                        timer.Stop();
                        long elapsedMs = timer.ElapsedMilliseconds;

                        // note highest tile and points
                        int highestTile = BoardHelper.HighestTile(endState.Board);
                        int points = endState.Points;
                        totalScore += points;

                        // write stats
                        String stats = i + ":\t" + iterationLimit + "\t" + highestTile + "\t" + points;
                        Console.WriteLine(stats);
                        writer.WriteLine(stats);

                        List<int> keys = new List<int>(highTileCount.Keys);
                        for (int j = 0; j < keys.Count; j++)
                        {
                            if (highestTile >= keys[j]) highTileCount[keys[j]]++;
                        }
                    }
                    writer.Close();
                    Console.WriteLine(GetStatistics(highTileCount, runs, totalScore));
                }
            }
            Console.ReadLine();
        }

        // Constructs a string showing statistics
        private static string GetStatistics(Dictionary<int, int> highTileCount, int runs, int totalScore)
        {
            return "512: " + (double)highTileCount[512] / runs * 100
                + "%, 1024: " + (double)highTileCount[1024] / runs * 100
                + "%, 2048: " + (double)highTileCount[2048] / runs * 100
                + "%, 4096: " + (double)highTileCount[4096] / runs * 100
                + "%, 8192: " + (double)highTileCount[8192] / runs * 100
                + "%, 16384: " + (double)highTileCount[16384] / runs * 100
                + "%, 32768: " + (double)highTileCount[32768] / runs * 100
                + "%" + "\nAverage Score: " + (double)totalScore / runs;
        }

        // Runs an entire game using the given AI type to decide on moves
        private static State RunAIGame(AI_TYPE AItype, bool print, int depth = 0, int timeLimit = 0, int iterationLimit = 0)
        {
            GameEngine game = new GameEngine();
            State end = null;
            if (AItype == AI_TYPE.CLASSIC_MINIMAX) 
            {
                Minimax minimax = new Minimax(game, depth);
                end = minimax.RunClassicMinimax(print);
                            }
            else if (AItype == AI_TYPE.ALPHA_BETA) 
            {
                Minimax minimax = new Minimax(game, depth);
                end = minimax.RunAlphaBeta(print);
            }
            else if (AItype == AI_TYPE.ITERATIVE_DEEPENING_ALPHA_BETA)
            {
                Minimax minimax = new Minimax(game, depth);
                end = minimax.RunIterativeDeepeningAlphaBeta(print, timeLimit);
            }
            else if (AItype == AI_TYPE.PARALLEL_ALPHA_BETA)
            {
                Minimax minimax = new Minimax(game, depth);
                end = minimax.RunParallelAlphaBeta(print);
            }
            else if (AItype == AI_TYPE.PARALLEL_ITERATIVE_DEEPENING_ALPHA_BETA)
            {
                Minimax minimax = new Minimax(game, depth);
                end = minimax.RunParallelIterativeDeepeningAlphaBeta(print, timeLimit);
            }
            else if (AItype == AI_TYPE.CLASSIC_EXPECTIMAX)
            {
                Expectimax expectimax = new Expectimax(game, depth);
                end = expectimax.RunClassicExpectimax(print, weights);
            }
            else if (AItype == AI_TYPE.EXPECTIMAX_STAR1)
            {
                Expectimax expectimax = new Expectimax(game, depth);
                end = expectimax.RunStar1Expectimax(print, weights);
            }
            else if (AItype == AI_TYPE.EXPECTIMAX_STAR1_FW_PRUNING)
            {
                Expectimax expectimax = new Expectimax(game, depth);
                end = expectimax.RunStar1WithUnlikelyPruning(print, weights);
            }
            else if (AItype == AI_TYPE.ITERATIVE_DEEPENING_EXPECTIMAX)
            {
                Expectimax expectimax = new Expectimax(game, depth);
                end = expectimax.RunIterativeDeepeningExpectimax(print, timeLimit, weights);
            }
            else if (AItype == AI_TYPE.PARALLEL_EXPECTIMAX)
            {
                Expectimax expectimax = new Expectimax(game, depth);
                end = expectimax.RunParallelClassicExpectimax(print, weights);
            }
            else if (AItype == AI_TYPE.PARALLEL_ITERATIVE_DEEPENING_EXPECTIMAX)
            {
                Expectimax expectimax = new Expectimax(game, depth);
                end = expectimax.RunParallelIterativeDeepeningExpectimax(print, timeLimit, weights);
            }
            else if (AItype == AI_TYPE.TT_ITERATIVE_DEEPENING_EXPECTIMAX)
            {
                Expectimax exptectimax = new Expectimax(game, depth);
                end = exptectimax.RunTTExpectimax(print, timeLimit, weights);
            }
            else if (AItype == AI_TYPE.TT_ITERATIVE_DEEPENING_STAR1)
            {
                Expectimax expectimax = new Expectimax(game, depth);
                end = expectimax.RunTTStar1(print, timeLimit, weights);
            }
            else if (AItype == AI_TYPE.EXPECTIMAX_WITH_ALL_IMPROVEMENTS)
            {
                Expectimax expectimax = new Expectimax(game, depth);
                end = expectimax.RunTTIterativeDeepeningExpectimaxWithStar1andForwardPruning(print, timeLimit, weights);
            }
            else if (AItype == AI_TYPE.EXPECTIMAX_WITH_ALL_IMPROVEMENTS_NO_FORWARDPRUNING)
            {
                Expectimax expectimax = new Expectimax(game, depth);
                end = expectimax.RunTTIterativeDeepeningExpectimaxWithStar1(print, timeLimit, weights);
            }
            else if (AItype == AI_TYPE.ITERATION_LIMITED_MCTS)
            {
                MonteCarlo MCTS = new MonteCarlo(game);
                end = MCTS.RunIterationLimitedMCTS(print, iterationLimit);
            }
            else if (AItype == AI_TYPE.TIME_LIMITED_MCTS)
            {
                MonteCarlo MCTS = new MonteCarlo(game);
                end = MCTS.RunTimeLimitedMCTS(print, timeLimit);
            }
            else if (AItype == AI_TYPE.ROOT_PARALLEL_ITERATION_LIMITED_MCTS)
            {
                MonteCarlo MCTS = new MonteCarlo(game);
                end = MCTS.RunRootParallelizationIterationLimitedMCTS(print, iterationLimit, NUM_THREADS);
            }
            else if (AItype == AI_TYPE.ROOT_PARALLEL_TIME_LIMITED_MCTS)
            {
                MonteCarlo MCTS = new MonteCarlo(game);
                end = MCTS.RunRootParallelizationTimeLimitedMCTS(print, timeLimit, NUM_THREADS);
            }
            else if (AItype == AI_TYPE.EXPECTIMAX_MCTS_TIME_LIMITED)
            {
                HeuristicLearning HL = new HeuristicLearning(game);
                end = HL.RunExpectimaxMCTStimeLimited(print, depth, timeLimit);
            }
            else if (AItype == AI_TYPE.EXPECTIMAX_MCTS_WITH_SIMULATIONS_TIME_LIMITED)
            {
                HeuristicLearning HL = new HeuristicLearning(game);
                end = HL.RunExpectimaxMCTSwithSimulations(print, depth, timeLimit);
            }
            else if (AItype == AI_TYPE.FINAL_COMBI)
            {
                HeuristicLearning HL = new HeuristicLearning(game);
                end = HL.RunParallelizationMCTSExpectimaxCombi(print, depth, timeLimit);
            }
            else
            {
                throw new Exception();
            }
            if (print)
            {
                Console.WriteLine("GAME OVER!\nFinal score: " + game.scoreController.getScore());
            }
            return end;
        }

        // Asks user for type of Minimax algorithm
        private static AI_TYPE GetMinimaxType()
        {
            int choice = GetChoice("1: Classic Minimax\n2: Alpha-Beta\n3: Iterative Deepening Alpha Beta\n4: Parallel Alpha Beta\n5: Parallel Iterative Deepening Alpha Beta");
            if (choice == 1) return AI_TYPE.CLASSIC_MINIMAX;
            else if (choice == 2) return AI_TYPE.ALPHA_BETA;
            else if (choice == 3) return AI_TYPE.ITERATIVE_DEEPENING_ALPHA_BETA;
            else if (choice == 4) return AI_TYPE.PARALLEL_ALPHA_BETA;
            else if (choice == 5) return AI_TYPE.PARALLEL_ITERATIVE_DEEPENING_ALPHA_BETA;
            else return GetMinimaxType(); // invalid option, ask again
        }

        // Asks user for type of Expectimax algorithm
        private static AI_TYPE GetExpectimaxType()
        {
            int choice = GetChoice("1: Classic Expectimax\n2: Star1 Expectimax\n3: Iterative Deepening Expectimax\n4: Parallel Expectimax\n5: Parallel Iterative Deepening Expectimax\n"
                + "6: Transposition Table Iterative Deepening Expectimax\n7: Transposition Table Iterative Deepening Star1\n8: Expectimax with Star1 + forward pruning\n"
                + "9: Expectimax with all improvements\n10: Expectimax with all improvements but no forward pruning");
            if (choice == 1) return AI_TYPE.CLASSIC_EXPECTIMAX;
            else if (choice == 2) return AI_TYPE.EXPECTIMAX_STAR1;
            else if (choice == 3) return AI_TYPE.ITERATIVE_DEEPENING_EXPECTIMAX;
            else if (choice == 4) return AI_TYPE.PARALLEL_EXPECTIMAX;
            else if (choice == 5) return AI_TYPE.PARALLEL_ITERATIVE_DEEPENING_EXPECTIMAX;
            else if (choice == 6) return AI_TYPE.TT_ITERATIVE_DEEPENING_EXPECTIMAX;
            else if (choice == 7) return AI_TYPE.TT_ITERATIVE_DEEPENING_STAR1;
            else if (choice == 8) return AI_TYPE.EXPECTIMAX_STAR1_FW_PRUNING;
            else if (choice == 9) return AI_TYPE.EXPECTIMAX_WITH_ALL_IMPROVEMENTS;
            else if (choice == 10) return AI_TYPE.EXPECTIMAX_WITH_ALL_IMPROVEMENTS_NO_FORWARDPRUNING;
            else return GetExpectimaxType(); // invalid option, ask again
        }

        //Asks user for type of MCTS algorithm
        private static AI_TYPE GetMonteCarloTreeSearchType()
        {
            int choice = GetChoice("1: Iteration Limited MCTS\n2: Time Limited MCTS\n3: Root Parallel Iteration Limited MCTS\n4: Root Parallel Time Limited MCTS");
            if (choice == 1) return AI_TYPE.ITERATION_LIMITED_MCTS;
            else if (choice == 2) return AI_TYPE.TIME_LIMITED_MCTS;
            else if (choice == 3) return AI_TYPE.ROOT_PARALLEL_ITERATION_LIMITED_MCTS;
            else if (choice == 4) return AI_TYPE.ROOT_PARALLEL_TIME_LIMITED_MCTS;
            else return GetMonteCarloTreeSearchType(); // invalid option, ask again
        }

        // Asks user for type of heuristic learning algorithm
        private static AI_TYPE GetHeuristicLearningType()
        {
            int choice = GetChoice("1: MCTS with Expectimax instead of simulation - time limited\n2: MCTS with Expectimax and simulations - time limited\n3: Final combi!");
            if (choice == 1) return AI_TYPE.EXPECTIMAX_MCTS_TIME_LIMITED;
            else if (choice == 2) return AI_TYPE.EXPECTIMAX_MCTS_WITH_SIMULATIONS_TIME_LIMITED;
            else if (choice == 3) return AI_TYPE.FINAL_COMBI;
            else return GetHeuristicLearningType();
        }


        // Runs a game with the user playing
        private static void StartGame()
        {
            GameEngine game = new GameEngine();
            bool gameOver = false;
            CleanConsole();

            // main game loop
            while (!gameOver)
            {
                Console.SetCursorPosition(0, 0);
                Console.WriteLine("Score: " + game.scoreController.getScore() + "              ");
                Console.WriteLine(BoardHelper.ToString(game.board));
                DIRECTION direction = GetUserInput();
                Move move = new PlayerMove(direction);
                game.SendUserAction((PlayerMove)move);
                if (new State(game.board, game.scoreController.getScore(), GameEngine.PLAYER).IsGameOver())
                {
                    gameOver = true;
                }
            }
            Console.WriteLine("Game over! Final score: " + game.scoreController.getScore());
            Thread.Sleep(500);
        }

        // Reads user key input 
        private static DIRECTION GetUserInput()
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey();
            if (keyInfo.Key == ConsoleKey.UpArrow)
            {
                return DIRECTION.UP;
            }
            else if (keyInfo.Key == ConsoleKey.DownArrow)
            {
                return DIRECTION.DOWN;
            }
            else if (keyInfo.Key == ConsoleKey.LeftArrow)
            {
                return DIRECTION.LEFT;
            }
            else if (keyInfo.Key == ConsoleKey.RightArrow)
            {
                return DIRECTION.RIGHT;
            }
            else return (DIRECTION)(-1);
        }

        // Prints a game state to the console
        public static void PrintState(State state)
        {
            Program.CleanConsole();
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(BoardHelper.ToString(state.Board));
        }

        // Resets console output
        public static void CleanConsole()
        {
            Console.SetCursorPosition(0, 0);
            for (int i = 0; i < 20; i++)
            {
                Console.WriteLine("                                                                                                                                                                                                                   ");
            }    
        }

        // Just a helper method to ask for user input
        private static int GetChoice(String options)
        {
            Console.WriteLine("Please choose an option:");
            Console.WriteLine(options);
            int choice = Convert.ToInt32(Console.ReadLine());
            return choice;
        }
    }
}
