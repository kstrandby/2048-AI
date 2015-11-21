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
        // constants
        private const string MINIMAX_LOG_FILE = @"Minimax.txt";
        private const string EXPECTIMAX_LOG_FILE = @"Expectimax.txt";
        private const string MCTS_LOG_FILE = @"MCTS.txt";

        private const int NUM_THREADS = 8; // adjust based how many cores you have

        enum AI_TYPE
        {
            CLASSIC_MINIMAX,
            ALPHA_BETA,
            ITERATIVE_DEEPENING_ALPHA_BETA,
            PARALLEL_ALPHA_BETA,
            //PARALLEL_ITERATIVE_DEEPENING_ALPHA_BETA,
            CLASSIC_EXPECTIMAX,
            EXPECTIMAX_STAR1,
            ITERATIVE_DEEPENING_EXPECTIMAX,
            PARALLEL_EXPECTIMAX,
            PARALLEL_ITERATIVE_DEEPENING_EXPECTIMAX,
            ITERATION_LIMITED_MCTS,
            TIME_LIMITED_MCTS,
            ROOT_PARALLEL_ITERATION_LIMITED_MCTS,
            ROOT_PARALLEL_TIME_LIMITED_MCTS
        }

        static void Main(string[] args)
        {
            ShowMenu();        
        }

        private static void ShowMenu()
        {
            int choice = GetChoice("0: Play 2048\n1: Minimax\n2: Expectimax\n3: Monte Carlo Tree Search");
            if (choice == 0) StartGame();
            else if (choice == 1) RunMinimax();
            else if (choice == 2) RunExpectimax();
            else if (choice == 3) RunMonteCarlo();
            else
            {
                Console.WriteLine("Sorry, please choose between the options shown:");
                ShowMenu();
            }
        }

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
                    Console.WriteLine(GetStatistics(highTileCount, runs));
                        
                }
                else // minimaxType == AI_TYPE.ITERATIVE_DEEPENING_ALPHA_BETA
                {
                    int timeLimit = GetChoice("Time limit? (in ms)");
                    for (int i = 0; i < runs; i++)
                    {
                        // time run
                        Stopwatch timer = new Stopwatch();
                        timer.Start();
                        State endState = RunAIGame(minimaxType, true, 0, timeLimit);
                        timer.Stop();
                        long elapsedMs = timer.ElapsedMilliseconds;

                        // note highest tile and points
                        int highestTile = BoardHelper.HighestTile(endState.Board);
                        int points = endState.Points;

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
                    Console.WriteLine(GetStatistics(highTileCount, runs));
                }
            }
            Console.ReadLine();
        }

        private static void RunExpectimax()
        {
            AI_TYPE expectimaxType = GetExpectimaxType();
            int choice = GetChoice("1: Graphic run\n2: Test runs");

            // Graphic run
            if (choice == 1)
            {
                if (expectimaxType == AI_TYPE.CLASSIC_EXPECTIMAX || expectimaxType == AI_TYPE.EXPECTIMAX_STAR1 || expectimaxType == AI_TYPE.PARALLEL_EXPECTIMAX)
                {
                    int depth = GetChoice("Depth?");
                    CleanConsole();
                    RunAIGame(expectimaxType, true, depth);
                }
                else if(expectimaxType == AI_TYPE.ITERATIVE_DEEPENING_EXPECTIMAX || expectimaxType == AI_TYPE.PARALLEL_ITERATIVE_DEEPENING_EXPECTIMAX)
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

                if (expectimaxType == AI_TYPE.CLASSIC_EXPECTIMAX || expectimaxType == AI_TYPE.EXPECTIMAX_STAR1 || expectimaxType == AI_TYPE.PARALLEL_EXPECTIMAX)
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
                    Console.WriteLine(GetStatistics(highTileCount, runs));
                }
                else if (expectimaxType == AI_TYPE.ITERATIVE_DEEPENING_EXPECTIMAX || expectimaxType == AI_TYPE.PARALLEL_ITERATIVE_DEEPENING_EXPECTIMAX)
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
                    Console.WriteLine(GetStatistics(highTileCount, runs));
                }
            }
            Console.ReadLine();
        }

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
                    Console.WriteLine(GetStatistics(highTileCount, runs));
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
                    Console.WriteLine(GetStatistics(highTileCount, runs));
                }
            }
            Console.ReadLine();
        }

        private static string GetStatistics(Dictionary<int, int> highTileCount, int runs)
        {
            return "512: " + (double)highTileCount[512] / runs * 100
                + "%, 1024: " + (double)highTileCount[1024] / runs * 100
                + "%, 2048: " + (double)highTileCount[2048] / runs * 100
                + "%, 4096: " + (double)highTileCount[4096] / runs * 100
                + "%, 8192: " + (double)highTileCount[8192] / runs * 100
                + "%, 16384: " + (double)highTileCount[16384] / runs * 100
                + "%, 32768: " + (double)highTileCount[32768] / runs * 100
                + "%";
        }

        private static State RunAIGame(AI_TYPE AItype, bool print, int depth = 0, int timeLimit = 0, int iterationLimit = 0)
        {
            GameEngine game = new GameEngine();
            State end = null;
            // MINIMAX TYPES
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
            else if (AItype == AI_TYPE.CLASSIC_EXPECTIMAX)
            {
                Expectimax expectimax = new Expectimax(game, depth);
                end = expectimax.RunClassicExpectimax(print);
            }
            else if (AItype == AI_TYPE.EXPECTIMAX_STAR1)
            {
                Expectimax expectimax = new Expectimax(game, depth);
                end = expectimax.RunStar1Expectimax(print);
            }
            else if (AItype == AI_TYPE.ITERATIVE_DEEPENING_EXPECTIMAX)
            {
                Expectimax expectimax = new Expectimax(game, depth);
                end = expectimax.RunIterativeDeepeningExpectimax(print, timeLimit);
            }
            else if (AItype == AI_TYPE.PARALLEL_EXPECTIMAX)
            {
                Expectimax expectimax = new Expectimax(game, depth);
                end = expectimax.RunParallelClassicExpectimax(print);
            }
            else if (AItype == AI_TYPE.PARALLEL_ITERATIVE_DEEPENING_EXPECTIMAX)
            {
                Expectimax expectimax = new Expectimax(game, depth);
                end = expectimax.RunParallelIterativeDeepeningExpectimax(print, timeLimit);
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

        private static AI_TYPE GetMinimaxType()
        {
            int choice = GetChoice("1: Classic Minimax\n2: Alpha-Beta\n3: Iterative Deepening Alpha Beta\n4: Parallel Alpha Beta");
            if (choice == 1) return AI_TYPE.CLASSIC_MINIMAX;
            else if (choice == 2) return AI_TYPE.ALPHA_BETA;
            else if (choice == 3) return AI_TYPE.ITERATIVE_DEEPENING_ALPHA_BETA;
            else if (choice == 4) return AI_TYPE.PARALLEL_ALPHA_BETA;
            else return GetMinimaxType(); // invalid option, ask again
        }

        private static AI_TYPE GetExpectimaxType()
        {
            int choice = GetChoice("1: Classic Expectimax\n2: Star1 Expectimax\n3: Iterative Deepening Expectimax\n4: Parallel Expectimax\n5: Parallel Iterative Deepening Expectimax");
            if (choice == 1) return AI_TYPE.CLASSIC_EXPECTIMAX;
            else if (choice == 2) return AI_TYPE.EXPECTIMAX_STAR1;
            else if (choice == 3) return AI_TYPE.ITERATIVE_DEEPENING_EXPECTIMAX;
            else if (choice == 4) return AI_TYPE.PARALLEL_EXPECTIMAX;
            else if (choice == 5) return AI_TYPE.PARALLEL_ITERATIVE_DEEPENING_EXPECTIMAX;
            else return GetExpectimaxType(); // invalid option, ask again
        }

        private static AI_TYPE GetMonteCarloTreeSearchType()
        {
            int choice = GetChoice("1: Iteration Limited MCTS\n2: Time Limited MCTS\n3: Root Parallel Iteration Limited MCTS\n4: Root Parallel Time Limites MCTS");
            if (choice == 1) return AI_TYPE.ITERATION_LIMITED_MCTS;
            else if (choice == 2) return AI_TYPE.TIME_LIMITED_MCTS;
            else if (choice == 3) return AI_TYPE.ROOT_PARALLEL_ITERATION_LIMITED_MCTS;
            else if (choice == 4) return AI_TYPE.ROOT_PARALLEL_TIME_LIMITED_MCTS;
            else return GetMonteCarloTreeSearchType(); // invalid option, ask again
        }

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
                gameOver = game.SendUserAction((PlayerMove)move);
            }

            Console.WriteLine("Game over! Final score: " + game.scoreController.getScore());
            Thread.Sleep(500);
        }

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

        public static void PrintState(State state)
        {
            Program.CleanConsole();
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(BoardHelper.ToString(state.Board));
        }

        public static void CleanConsole()
        {
            Console.SetCursorPosition(0, 0);
            for (int i = 0; i < 20; i++)
            {
                Console.WriteLine("                                                                                                                                                                                                                   ");
            }    
        }

        private static int GetChoice(String options)
        {
            Console.WriteLine("Please choose an option:");
            Console.WriteLine(options);
            int choice = Convert.ToInt32(Console.ReadLine());
            return choice;
        }
    }
}
