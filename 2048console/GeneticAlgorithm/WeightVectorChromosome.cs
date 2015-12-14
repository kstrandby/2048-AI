using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2048console.GeneticAlgorithm
{
    class WeightVectorChromosome
    {
        private const int NUM_THREADS = 5;
        private const int NUM_TESTS = 10; // how many games to run to test weights
        private const int TIME_LIMIT = 10; // time limit for it.deep. expectimax runs
        private const int CHROMOSOME_LENGTH = 6; // length of chromosome (number of features)

        private WeightVector chromosome;
        private double fitness = -1;
        private int num_tests;

        private Random random = new Random();

        public WeightVectorChromosome(double empty_cells, double highest_value, double monotonicity, double points, double smoothness, double trapped_penalty)
        {
            this.chromosome = new WeightVector { Empty_cells = empty_cells, Highest_tile = highest_value, Monotonicity = monotonicity, Points = points, Smoothness = smoothness, Trapped_penalty = trapped_penalty };
        }

        public WeightVector ChromosomeValue
        {
            get { return chromosome; }
        }

        public double GetUpdatedFitness()
        {
            // first time this chromosome is evaluated
            if (fitness < 0)
            {
                fitness = TestWeights();
            }
            else
            {
                // update fitness by running new test
                double newFitness = TestWeights();
                fitness += newFitness;
            }
            
            return fitness / num_tests;
        }


        public double Fitness
        {
            get {
                if (fitness < 0) // shouldn't happen at this point
                {
                    fitness = TestWeights();
                }
                return fitness / num_tests;
            }
        }


        // Crossover two chromosomes
        public Tuple<WeightVectorChromosome, WeightVectorChromosome> Crossover(WeightVectorChromosome otherChromosome)
        {
            int crossoverPoint = random.Next(1, CHROMOSOME_LENGTH);
            if (crossoverPoint == 1)
            {
                WeightVectorChromosome child1 = new WeightVectorChromosome(this.chromosome.Empty_cells, otherChromosome.chromosome.Highest_tile, otherChromosome.chromosome.Monotonicity,
                    otherChromosome.chromosome.Points, otherChromosome.chromosome.Smoothness, otherChromosome.chromosome.Trapped_penalty);
                WeightVectorChromosome child2 = new WeightVectorChromosome(otherChromosome.chromosome.Empty_cells, this.chromosome.Highest_tile, this.chromosome.Monotonicity,
                    this.chromosome.Points, this.chromosome.Smoothness, this.chromosome.Trapped_penalty);
                return new Tuple<WeightVectorChromosome, WeightVectorChromosome>(child1, child2);
            }
            else if (crossoverPoint == 2)
            {
                WeightVectorChromosome child1 = new WeightVectorChromosome(this.chromosome.Empty_cells, this.chromosome.Highest_tile, otherChromosome.chromosome.Monotonicity,
                    otherChromosome.chromosome.Points, otherChromosome.chromosome.Smoothness, otherChromosome.chromosome.Trapped_penalty);
                WeightVectorChromosome child2 = new WeightVectorChromosome(otherChromosome.chromosome.Empty_cells, otherChromosome.chromosome.Highest_tile, this.chromosome.Monotonicity,
                    this.chromosome.Points, this.chromosome.Smoothness, this.chromosome.Trapped_penalty);
                return new Tuple<WeightVectorChromosome, WeightVectorChromosome>(child1, child2);
            }
            else if (crossoverPoint == 3)
            {
                WeightVectorChromosome child1 = new WeightVectorChromosome(this.chromosome.Empty_cells, this.chromosome.Highest_tile, this.chromosome.Monotonicity,
                    otherChromosome.chromosome.Points, otherChromosome.chromosome.Smoothness, otherChromosome.chromosome.Trapped_penalty);
                WeightVectorChromosome child2 = new WeightVectorChromosome(otherChromosome.chromosome.Empty_cells, otherChromosome.chromosome.Highest_tile, otherChromosome.chromosome.Monotonicity,
                    this.chromosome.Points, this.chromosome.Smoothness, this.chromosome.Trapped_penalty);
                return new Tuple<WeightVectorChromosome, WeightVectorChromosome>(child1, child2);
            }
            else if (crossoverPoint == 4)
            {
                WeightVectorChromosome child1 = new WeightVectorChromosome(this.chromosome.Empty_cells, this.chromosome.Highest_tile, this.chromosome.Monotonicity,
                    this.chromosome.Points, otherChromosome.chromosome.Smoothness, otherChromosome.chromosome.Trapped_penalty);
                WeightVectorChromosome child2 = new WeightVectorChromosome(otherChromosome.chromosome.Empty_cells, otherChromosome.chromosome.Highest_tile, otherChromosome.chromosome.Monotonicity,
                    otherChromosome.chromosome.Points, this.chromosome.Smoothness, this.chromosome.Trapped_penalty);
                return new Tuple<WeightVectorChromosome, WeightVectorChromosome>(child1, child2);
            }
            else if (crossoverPoint == 5)
            {
                WeightVectorChromosome child1 = new WeightVectorChromosome(this.chromosome.Empty_cells, this.chromosome.Highest_tile, this.chromosome.Monotonicity,
                    this.chromosome.Points, this.chromosome.Smoothness, otherChromosome.chromosome.Trapped_penalty);
                WeightVectorChromosome child2 = new WeightVectorChromosome(otherChromosome.chromosome.Empty_cells, otherChromosome.chromosome.Highest_tile, otherChromosome.chromosome.Monotonicity,
                    otherChromosome.chromosome.Points, otherChromosome.chromosome.Smoothness, this.chromosome.Trapped_penalty);
                return new Tuple<WeightVectorChromosome, WeightVectorChromosome>(child1, child2);
            }
            else throw new Exception();
        }

        public WeightVectorChromosome Mutate()
        {

            int randFeatureIndex = random.Next(0, CHROMOSOME_LENGTH);
            
            if(randFeatureIndex == 0) {
                double min = -this.chromosome.Empty_cells;
                double max = this.chromosome.Empty_cells;
                double mutation = random.NextDouble() * (max - min) + min;
                return new WeightVectorChromosome(this.chromosome.Empty_cells + mutation, this.chromosome.Highest_tile, 
                    this.chromosome.Monotonicity, this.chromosome.Points, this.chromosome.Smoothness, this.chromosome.Trapped_penalty);
            }
            else if (randFeatureIndex == 1)
            {
                double min = -this.chromosome.Highest_tile;
                double max = this.chromosome.Highest_tile;
                double mutation = random.NextDouble() * (max - min) + min;
                return new WeightVectorChromosome(this.chromosome.Empty_cells, this.chromosome.Highest_tile + mutation,
                    this.chromosome.Monotonicity, this.chromosome.Points, this.chromosome.Smoothness, this.chromosome.Trapped_penalty);
            }
            else if (randFeatureIndex == 2)
            {
                double min = -this.chromosome.Monotonicity;
                double max = this.chromosome.Monotonicity;
                double mutation = random.NextDouble() * (max - min) + min;
                return new WeightVectorChromosome(this.chromosome.Empty_cells, this.chromosome.Highest_tile, this.chromosome.Monotonicity + mutation, 
                    this.chromosome.Points, this.chromosome.Smoothness, this.chromosome.Trapped_penalty);
            }
            else if (randFeatureIndex == 3)
            {
                double min = -this.chromosome.Points;
                double max = this.chromosome.Points;
                double mutation = random.NextDouble() * (max - min) + min;
                return new WeightVectorChromosome(this.chromosome.Empty_cells, this.chromosome.Highest_tile, this.chromosome.Monotonicity,
                    this.chromosome.Points + mutation, this.chromosome.Smoothness, this.chromosome.Trapped_penalty);
            }
            else if (randFeatureIndex == 4)
            {
                double min = -this.chromosome.Smoothness;
                double max = this.chromosome.Smoothness;
                double mutation = random.NextDouble() * (max - min) + min;
                return new WeightVectorChromosome(this.chromosome.Empty_cells, this.chromosome.Highest_tile, this.chromosome.Monotonicity,
                    this.chromosome.Points, this.chromosome.Smoothness + mutation, this.chromosome.Trapped_penalty);
            }
            else if (randFeatureIndex == 5)
            {
                double min = -this.chromosome.Trapped_penalty;
                double max = this.chromosome.Trapped_penalty;
                double mutation = random.NextDouble() * (max - min) + min;
                return new WeightVectorChromosome(this.chromosome.Empty_cells, this.chromosome.Highest_tile, this.chromosome.Monotonicity,
                    this.chromosome.Points, this.chromosome.Smoothness, this.chromosome.Trapped_penalty + mutation);
            }
            else
            {
                throw new Exception();
            }
        }


        private double TestWeights()
        {
            double total = 0;
            ConcurrentBag<double> subTotals = new ConcurrentBag<double>();
            Parallel.For(0, NUM_THREADS, j =>
            {
                double subtotal = 0;
                for (int i = 0; i < NUM_TESTS / NUM_THREADS; i++)
                {
                    GameEngine gameEngine = new GameEngine();
                    Expectimax expectimax = new Expectimax(gameEngine, 0);
                    State end = expectimax.RunTTExpectimax(false, TIME_LIMIT, chromosome);
                    double points = end.Points;
                    subtotal += points;

                }
                subTotals.Add(subtotal);
            });
            foreach (double subtotal in subTotals)
            {
                total += subtotal;
            }
            num_tests += NUM_TESTS;
            return total;

        }
    }
}
