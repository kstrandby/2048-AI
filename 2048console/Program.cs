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
        static void Main(string[] args)
        {
           

            Console.ReadLine();
            Console.WriteLine("Starting game...");
            Thread.Sleep(500);
            Console.SetCursorPosition(0, 0);
            ShowMenu();        
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
                Console.WriteLine(GridHelper.ToString(game.grid));
                DIRECTION direction = GetUserInput();
                Move move = new PlayerMove(direction);
                gameOver = game.SendUserAction((PlayerMove)move);
            }

            Console.WriteLine("Game over! Final score: " + game.scoreController.getScore());
            Thread.Sleep(500);
        }

        private static void ShowMenu()
        {
            Console.WriteLine("Please choose what you want to do:");
            Console.WriteLine("0: Play 2048\n1: Let AI Minimax play 2048\n2: Let Naive AI play 2048\n3: Let AI Expectimax play 2048\n4: Let AI Monte Carlo play 2048");
            int choice = Convert.ToInt32(Console.ReadLine());
            if (choice == 0) StartGame();
            else if (choice == 1) RunMinimax();
            else if (choice == 2) RunNaiveAI();
            else if (choice == 3) RunExpectimax();
            else if (choice == 4) RunMonteCarlo();
            else
            {
                Console.WriteLine("Sorry, please choose between the options shown:");
                ShowMenu();
            }
        }

        private static void RunMonteCarlo()
        {
            int choice = GetGraphicVsTestRunsChoice();
            if (choice == 1)
            {
                Console.WriteLine("How many simulations?");
                int simulations = Convert.ToInt32(Console.ReadLine());
                CleanConsole();
                GameEngine game = new GameEngine();
                MonteCarlo monteCarlo = new MonteCarlo(game, simulations, 5000);
                monteCarlo.Run(true);
                Console.ReadLine();
            }
            else if (choice == 2)
            {
                Console.WriteLine("Choose number of runs: ");
                int runs = Convert.ToInt32(Console.ReadLine());
                Console.WriteLine("Choose number of simulations: ");
                int simulations = Convert.ToInt32(Console.ReadLine());

                StreamWriter writer = new StreamWriter(@"C:\Users\Kristine\Documents\Visual Studio 2013\Projects\2048console\MCTS_c=parent.points+2000.txt", true);
                int num1024 = 0;
                int num2048 = 0;
                int num4096 = 0;
                int num8192 = 0;

                MonteCarlo MCTS = null;
                for (int i = 0; i < runs; i++)
                {
                    Console.Write(i + ": ");
                    GameEngine game = new GameEngine();


                    MCTS = new MonteCarlo(game, simulations, 1);

                    // timing run
                    var watch = Stopwatch.StartNew();
                    State endState = MCTS.Run(false);
                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;
                    Console.WriteLine("Execution time: " + elapsedMs + " ms");

                    int highestTile = GridHelper.HighestTile(endState.Grid);
                    int points = endState.Points;
                    writer.WriteLine("{0,0}{1,10}{2,15}{3,12}{4,15}", i, simulations, highestTile, points, elapsedMs);
                    if (highestTile >= 1024) num1024++;
                    if (highestTile >= 2048) num2048++;
                    if (highestTile >= 4096) num4096++;
                    if (highestTile >= 8192) num8192++;
                }
                writer.Close();
                Console.WriteLine("1024: " + (double)num1024 / runs * 100 + "%, 2048: " + (double)num2048 / runs * 100 + "%, 4096: " + (double)num4096 / runs * 100 + "%, 8192: " + (double)num8192 / runs * 100 + "%");
                Console.ReadLine();
            }
        }

        private static void RunExpectimax()
        {
            int choice = GetGraphicVsTestRunsChoice();
            if (choice == 1)
            {
                Console.SetCursorPosition(0, 0);
                CleanConsole();
                GameEngine game = new GameEngine();
                Expectimax expectimax = new Expectimax(game, 1);
                expectimax.Run(true);
                Console.ReadLine();
            }
            else if (choice == 2)
            {
                Console.WriteLine("Choose number of runs: ");
                int runs = Convert.ToInt32(Console.ReadLine());
                Console.WriteLine("Choose depth: ");
                int depth = Convert.ToInt32(Console.ReadLine());

                StreamWriter writer = new StreamWriter(@"C:\Users\Kristine\Documents\Visual Studio 2013\Projects\2048console\Expectimax_NoPruning.txt", true);
                int num512 = 0;
                int num1024 = 0;
                int num2048 = 0;
                int num4096 = 0;
                int num8192 = 0;
                for (int i = 0; i < runs; i++)
                {
                    Console.Write(i + ": ");
                    GameEngine game = new GameEngine();
                    

                    Expectimax expectimax = new Expectimax(game, depth);

                    // timing run
                    var watch = Stopwatch.StartNew();
                    State endState = expectimax.Run(false);
                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;
                    Console.WriteLine("Execution time: " + elapsedMs + " ms");

                    int highestTile = GridHelper.HighestTile(endState.Grid);
                    int points = endState.Points;
                    writer.WriteLine("{0,0}{1,10}{2,15}{3,12}{4,15}", i, depth, highestTile, points, elapsedMs);
                    if (highestTile >= 512) num512++;
                    if (highestTile >= 1024) num1024++;
                    if (highestTile >= 2048) num2048++;
                    if (highestTile >= 4096) num4096++;
                    if (highestTile >= 8192) num8192++;
                }
                writer.Close();
                Console.WriteLine("512: " + (double)num512 / runs * 100 + "%, 1024: " + (double)num1024 / runs * 100 + "%, 2048: " + (double)num2048 / runs * 100 + "%, 4096: " + (double)num4096 / runs * 100 + "%, 8192: " + (double)num8192 / runs * 100 + "%");
                Console.ReadLine();
            }
            else
            {
                RunExpectimax();
            }
            
        }



        private static void RunMinimax()
        {
            int choice = GetGraphicVsTestRunsChoice();
            if (choice == 1)
            {
                Console.WriteLine("Depth?");
                int depth = Convert.ToInt32(Console.ReadLine());
                Console.SetCursorPosition(0, 0);
                CleanConsole();
                GameEngine game = new GameEngine();
                Minimax minimax = new Minimax(game, depth);
                State endState = minimax.Run(true);

            }
            else if (choice == 2)
            {
                Console.WriteLine("Choose number of runs: ");
                int runs = Convert.ToInt32(Console.ReadLine());
                Console.WriteLine("Choose depth: ");
                int depth = Convert.ToInt32(Console.ReadLine());

                StreamWriter writer = new StreamWriter(@"C:\Users\Kristine\Documents\Visual Studio 2013\Projects\2048console\TestStats.txt", true);
                int num1024 = 0;
                int num2048 = 0;
                int num4096 = 0;
                for (int i = 0; i < runs; i++)
                {
                    Console.Write(i + ": ");
                    GameEngine game = new GameEngine();
                    Minimax minimax = new Minimax(game, depth);
                    State endState = minimax.Run(false);
                    int highestTile = GridHelper.HighestTile(endState.Grid);
                    int points = endState.Points;
                    writer.WriteLine("{0,0}{1,10}{2,15}{3,12}", i, depth, highestTile, points);
                    if (highestTile >= 1024) num1024++;
                    if (highestTile >= 2048) num2048++;
                    if (highestTile >= 4096) num4096++;
                }
                writer.Close();
                Console.WriteLine("1024: " + num1024 + "%, 2048: " + num2048 + "%, 4096: " + num4096 + "%");
                Console.ReadLine();

            }
        }

        private static void RunNaiveAI()
        {
            int choice = GetGraphicVsTestRunsChoice();
            if (choice == 1)
            {
                GameEngine gameEngine = new GameEngine();
                NaiveAI ai = new NaiveAI(gameEngine);
                bool gameOver = false;
                Console.SetCursorPosition(0, 0);
                CleanConsole();

                while (!gameOver)
                {
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine("Score: " + gameEngine.scoreController.getScore() + "              ");
                    Console.WriteLine(GridHelper.ToString(gameEngine.grid));
                    DIRECTION direction = ai.chooseAction();
                    Move move = new PlayerMove(direction);
                    gameOver = gameEngine.SendUserAction((PlayerMove)move);
                    Thread.Sleep(500);
                }
                Thread.Sleep(1000);
            }
            else if (choice == 2)
            {
                Console.WriteLine("Choose number of runs: ");
                int runs = Convert.ToInt32(Console.ReadLine());

                StreamWriter writer = new StreamWriter(@"C:\Users\Kristine\Documents\Visual Studio 2013\Projects\2048console\TestStats.txt", true);
                writer.WriteLine("{0,0}{1,10}{2,10}", "Run:", "Highest tile:", "Points:");
                CleanConsole();
                for (int i = 0; i < runs; i++)
                {
                    Console.SetCursorPosition(0, 0);
                    Console.Write("Progress: " + ((double)i / (double)runs * 100) + "%");
                    GameEngine gameEngine = new GameEngine();
                    NaiveAI ai = new NaiveAI(gameEngine);
                    bool gameOver = false;

                    while (!gameOver)
                    {
                        DIRECTION direction = ai.chooseAction();
                        Move move = new PlayerMove(direction); 
                        gameOver = gameEngine.SendUserAction((PlayerMove)move);
                    }

                    int highestTile = GridHelper.HighestTile(gameEngine.grid);
                    int points = gameEngine.scoreController.getScore();
                    writer.WriteLine("{0,0}{1,10}{2,10}", i, highestTile, points);
                }
            }
            else
            {
                Console.WriteLine("Please chose between the options described before");
            }
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

        public static void CleanConsole()
        {
            Console.SetCursorPosition(0, 0);
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine("                                                                                                                                                                                                                   ");
            }
                
        }

        private static int GetGraphicVsTestRunsChoice()
        {
            Console.WriteLine("1: Graphic run\n2: Test runs");
            int choice = Convert.ToInt32(Console.ReadLine());
            return choice;
        }
    }
}
