using _2048console.GeneticAlgorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2048console
{
    // test comparing expectimax, minimax and mcts based on pre-defined states
    public class Test
    {
        public Test() { }

        public void TestMCTStree()
        {
             WeightVectorAll weights = new WeightVectorAll { Corner = 0, Empty_cells = 0, Highest_tile = 0, Monotonicity = 0, Points =0, Smoothness = 0, Snake = 1, Trapped_penalty = 0 };
             int timeLimit = 100;

            int[][] state1 = new int[][] {
                new int[]{1024,16,0,0},
                new int[]{4,32,2,0},
                new int[]{64,16,0,0},
                new int[]{16,16,2,2}
            };
            int[][] state2 = new int[][] {
                new int[]{2,0,2,0},
                new int[]{8,2,0,0},
                new int[]{16,8,4,0},
                new int[]{64,4,4,0}
            };
            int[][] state3 = new int[][] {
                new int[]{16,16,16,4},
                new int[]{64,4,0,0},
                new int[]{8,0,2,0},
                new int[]{16,0,0,0}
            };
            int[][] state4 = new int[][] {
                new int[]{0,0,0,8},
                new int[]{0,0,16,16},
                new int[]{2,0,32,32},
                new int[]{2,4,16,8}
            };
            Console.WriteLine("Testing state1:");
            GameEngine gameEngine = new GameEngine();
            Minimax minimax = new Minimax(gameEngine, 0);
            Expectimax expectimax = new Expectimax(gameEngine, 0);
            MonteCarlo mcts = new MonteCarlo(gameEngine);

            Move minimaxMove = minimax.IterativeDeepening(new State(state1, CalculateScore(state1), GameEngine.PLAYER), timeLimit);
            Move expectimaxMove = expectimax.IterativeDeepening(new State(state1, CalculateScore(state1), GameEngine.PLAYER), timeLimit, weights);
            Move mctsMove = (mcts.TimeLimitedMCTS(new State(state1, CalculateScore(state1), GameEngine.PLAYER), timeLimit)).GeneratingMove;

            Console.WriteLine("Minimax move chosen: " + ((PlayerMove)minimaxMove).Direction);
            Console.WriteLine("Expectimax move chosen: " + ((PlayerMove)expectimaxMove).Direction);
            Console.WriteLine("MCTS move chosen: " + ((PlayerMove)mctsMove).Direction);

            Console.WriteLine("Testing state2:");
            minimaxMove = minimax.IterativeDeepening(new State(state2, CalculateScore(state2), GameEngine.PLAYER), timeLimit);
            expectimaxMove = expectimax.IterativeDeepening(new State(state2, CalculateScore(state2), GameEngine.PLAYER), timeLimit, weights);
            mctsMove = (mcts.TimeLimitedMCTS(new State(state2, CalculateScore(state2), GameEngine.PLAYER), timeLimit)).GeneratingMove;

            Console.WriteLine("Minimax move chosen: " + ((PlayerMove)minimaxMove).Direction);
            Console.WriteLine("Expectimax move chosen: " + ((PlayerMove)expectimaxMove).Direction);
            Console.WriteLine("MCTS move chosen: " + ((PlayerMove)mctsMove).Direction);

            Console.WriteLine("Testing state3:");
            minimaxMove = minimax.IterativeDeepening(new State(state3, CalculateScore(state3), GameEngine.PLAYER), timeLimit);
            expectimaxMove = expectimax.IterativeDeepening(new State(state3, CalculateScore(state3), GameEngine.PLAYER), timeLimit, weights);
            mctsMove = (mcts.TimeLimitedMCTS(new State(state3, CalculateScore(state3), GameEngine.PLAYER), timeLimit)).GeneratingMove;

            Console.WriteLine("Minimax move chosen: " + ((PlayerMove)minimaxMove).Direction);
            Console.WriteLine("Expectimax move chosen: " + ((PlayerMove)expectimaxMove).Direction);
            Console.WriteLine("MCTS move chosen: " + ((PlayerMove)mctsMove).Direction);

            Console.WriteLine("Testing state4:");
            minimaxMove = minimax.IterativeDeepening(new State(state4, CalculateScore(state4), GameEngine.PLAYER), timeLimit);
            expectimaxMove = expectimax.IterativeDeepening(new State(state4, CalculateScore(state4), GameEngine.PLAYER), timeLimit, weights);
            mctsMove = (mcts.TimeLimitedMCTS(new State(state4, CalculateScore(state4), GameEngine.PLAYER), timeLimit)).GeneratingMove;

            Console.WriteLine("Minimax move chosen: " + ((PlayerMove)minimaxMove).Direction);
            Console.WriteLine("Expectimax move chosen: " + ((PlayerMove)expectimaxMove).Direction);
            Console.WriteLine("MCTS move chosen: " + ((PlayerMove)mctsMove).Direction);
        }
        private Node FindBestChild(List<Node> children)
        {

            double bestResults = 0;
            Node best = null;
            foreach (Node child in children)
            {
                if (child.Results / child.Visits > bestResults)
                {
                    best = child;
                    bestResults = child.Results / child.Visits;
                }
            }
            return best;
        }

        private int CalculateScore(int[][] board)
        {
            int score = 0;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (board[i][j] != 0)
                    {
                        double value = Math.Log(board[i][j]) / Math.Log(2);

                        double points = Math.Pow(2, value) * (value - 1);
                        score += (int)points;
                    }
                    
                }
            }
            return score;
        }

    }
}
