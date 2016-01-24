using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2048console.GeneticAlgorithm
{
    // Class to represent a chromosome
    abstract class WeightVectorChromosome
    {
        public WeightVector chromosome;
        private double fitness = -1;
        private int num_tests;

        private const int NUM_THREADS = 5;
        private const int NUM_TESTS = 20; // how many games to run to test weights

        public WeightVector ChromosomeValue
        {
            get { return chromosome; }
        }

        public double Fitness
        {
            get
            {
                if (fitness < 0) // shouldn't happen at this point
                {
                    fitness = TestWeights();
                }
                return fitness / num_tests;
            }
        }

        // Runs expectimax searches to test the weights of the chromosome
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
                    Expectimax expectimax = new Expectimax(gameEngine, 2);
                    State end = expectimax.RunStar1WithUnlikelyPruning(false, chromosome);
                    double points = end.Points;
                    subtotal += points;

                }
                subTotals.Add(subtotal);
            });
            foreach (double sub in subTotals)
            {
                total += sub;
            }
            num_tests += NUM_TESTS;
            return total;

        }
        public int GetNumberOfTestRuns()
        {
            return num_tests;
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

        // to be implemented by each subclass
        public abstract Tuple<WeightVectorChromosome, WeightVectorChromosome> Crossover(WeightVectorChromosome parent2, Random random);
        public abstract WeightVectorChromosome Mutate(Random random);
    }
    
    
    class WeightVectorChromosomeAll : WeightVectorChromosome
    {

        private const int CHROMOSOME_LENGTH = 8; // length of chromosome (number of features)

        public WeightVectorChromosomeAll(double corner, double empty_cells, double highest_value, double monotonicity, double points, double smoothness, double snake, double trapped_penalty)
        {
            this.chromosome = new WeightVectorAll { Corner = corner, Empty_cells = empty_cells, Highest_tile = highest_value, Monotonicity = monotonicity, Points = points, Smoothness = smoothness, Snake = snake, Trapped_penalty = trapped_penalty };
        }

        // Crossover two chromosomes
        public override Tuple<WeightVectorChromosome, WeightVectorChromosome> Crossover(WeightVectorChromosome otherChromosome, Random random)
        {
            int crossoverPoint = random.Next(1, CHROMOSOME_LENGTH);
            if (crossoverPoint == 1)
            {
                WeightVectorChromosomeAll child1 = new WeightVectorChromosomeAll(((WeightVectorAll)this.chromosome).Corner, ((WeightVectorAll)otherChromosome.chromosome).Empty_cells, 
                    ((WeightVectorAll)otherChromosome.chromosome).Highest_tile, ((WeightVectorAll)otherChromosome.chromosome).Monotonicity, ((WeightVectorAll)otherChromosome.chromosome).Points,
                    ((WeightVectorAll)otherChromosome.chromosome).Smoothness, ((WeightVectorAll)otherChromosome.chromosome).Snake, ((WeightVectorAll)otherChromosome.chromosome).Trapped_penalty);
                WeightVectorChromosomeAll child2 = new WeightVectorChromosomeAll(((WeightVectorAll)otherChromosome.chromosome).Corner, ((WeightVectorAll)this.chromosome).Empty_cells, 
                    ((WeightVectorAll)this.chromosome).Highest_tile, ((WeightVectorAll)this.chromosome).Monotonicity, ((WeightVectorAll)this.chromosome).Points,
                    ((WeightVectorAll)this.chromosome).Smoothness, ((WeightVectorAll)this.chromosome).Snake, ((WeightVectorAll)this.chromosome).Trapped_penalty);
                return new Tuple<WeightVectorChromosome, WeightVectorChromosome>(child1, child2);
            }
            else if (crossoverPoint == 2)
            {
                WeightVectorChromosomeAll child1 = new WeightVectorChromosomeAll(((WeightVectorAll)this.chromosome).Corner, ((WeightVectorAll)this.chromosome).Empty_cells, 
                    ((WeightVectorAll)otherChromosome.chromosome).Highest_tile, ((WeightVectorAll)otherChromosome.chromosome).Monotonicity, ((WeightVectorAll)otherChromosome.chromosome).Points,
                    ((WeightVectorAll)otherChromosome.chromosome).Smoothness, ((WeightVectorAll)otherChromosome.chromosome).Snake, ((WeightVectorAll)otherChromosome.chromosome).Trapped_penalty);
                WeightVectorChromosomeAll child2 = new WeightVectorChromosomeAll(((WeightVectorAll)otherChromosome.chromosome).Corner, ((WeightVectorAll)otherChromosome.chromosome).Empty_cells, 
                    ((WeightVectorAll)this.chromosome).Highest_tile, ((WeightVectorAll)this.chromosome).Monotonicity, ((WeightVectorAll)this.chromosome).Points,
                    ((WeightVectorAll)this.chromosome).Smoothness, ((WeightVectorAll)this.chromosome).Snake, ((WeightVectorAll)this.chromosome).Trapped_penalty);
                return new Tuple<WeightVectorChromosome, WeightVectorChromosome>(child1, child2);
            }
            else if (crossoverPoint == 3)
            {
                WeightVectorChromosomeAll child1 = new WeightVectorChromosomeAll(((WeightVectorAll)this.chromosome).Corner, ((WeightVectorAll)this.chromosome).Empty_cells, 
                    ((WeightVectorAll)this.chromosome).Highest_tile, ((WeightVectorAll)otherChromosome.chromosome).Monotonicity, ((WeightVectorAll)otherChromosome.chromosome).Points,
                    ((WeightVectorAll)otherChromosome.chromosome).Smoothness, ((WeightVectorAll)otherChromosome.chromosome).Snake, ((WeightVectorAll)otherChromosome.chromosome).Trapped_penalty);
                WeightVectorChromosomeAll child2 = new WeightVectorChromosomeAll(((WeightVectorAll)otherChromosome.chromosome).Corner, ((WeightVectorAll)otherChromosome.chromosome).Empty_cells, 
                    ((WeightVectorAll)otherChromosome.chromosome).Highest_tile, ((WeightVectorAll)this.chromosome).Monotonicity, ((WeightVectorAll)this.chromosome).Points,
                    ((WeightVectorAll)this.chromosome).Smoothness, ((WeightVectorAll)this.chromosome).Snake, ((WeightVectorAll)this.chromosome).Trapped_penalty);
                return new Tuple<WeightVectorChromosome, WeightVectorChromosome>(child1, child2);
            }
            else if (crossoverPoint == 4)
            {
                WeightVectorChromosomeAll child1 = new WeightVectorChromosomeAll(((WeightVectorAll)this.chromosome).Corner, ((WeightVectorAll)this.chromosome).Empty_cells, 
                    ((WeightVectorAll)this.chromosome).Highest_tile, ((WeightVectorAll)this.chromosome).Monotonicity, ((WeightVectorAll)otherChromosome.chromosome).Points,
                    ((WeightVectorAll)otherChromosome.chromosome).Smoothness, ((WeightVectorAll)otherChromosome.chromosome).Snake, ((WeightVectorAll)otherChromosome.chromosome).Trapped_penalty);
                WeightVectorChromosomeAll child2 = new WeightVectorChromosomeAll(((WeightVectorAll)otherChromosome.chromosome).Corner, ((WeightVectorAll)otherChromosome.chromosome).Empty_cells, 
                    ((WeightVectorAll)otherChromosome.chromosome).Highest_tile, ((WeightVectorAll)otherChromosome.chromosome).Monotonicity, ((WeightVectorAll)this.chromosome).Points,
                    ((WeightVectorAll)this.chromosome).Smoothness, ((WeightVectorAll)this.chromosome).Snake, ((WeightVectorAll)this.chromosome).Trapped_penalty);
                return new Tuple<WeightVectorChromosome, WeightVectorChromosome>(child1, child2);
            }
            else if (crossoverPoint == 5)
            {
                WeightVectorChromosomeAll child1 = new WeightVectorChromosomeAll(((WeightVectorAll)this.chromosome).Corner, ((WeightVectorAll)this.chromosome).Empty_cells, 
                    ((WeightVectorAll)this.chromosome).Highest_tile, ((WeightVectorAll)this.chromosome).Monotonicity, ((WeightVectorAll)this.chromosome).Points,
                    ((WeightVectorAll)otherChromosome.chromosome).Smoothness, ((WeightVectorAll)otherChromosome.chromosome).Snake, ((WeightVectorAll)otherChromosome.chromosome).Trapped_penalty);
                WeightVectorChromosomeAll child2 = new WeightVectorChromosomeAll(((WeightVectorAll)otherChromosome.chromosome).Corner, ((WeightVectorAll)otherChromosome.chromosome).Empty_cells, 
                    ((WeightVectorAll)otherChromosome.chromosome).Highest_tile, ((WeightVectorAll)otherChromosome.chromosome).Monotonicity, ((WeightVectorAll)otherChromosome.chromosome).Points,
                    ((WeightVectorAll)this.chromosome).Smoothness, ((WeightVectorAll)this.chromosome).Snake, ((WeightVectorAll)this.chromosome).Trapped_penalty);
                return new Tuple<WeightVectorChromosome, WeightVectorChromosome>(child1, child2);
            }
            else if (crossoverPoint == 6)
            {
                WeightVectorChromosomeAll child1 = new WeightVectorChromosomeAll(((WeightVectorAll)this.chromosome).Corner, ((WeightVectorAll)this.chromosome).Empty_cells,
                    ((WeightVectorAll)this.chromosome).Highest_tile, ((WeightVectorAll)this.chromosome).Monotonicity, ((WeightVectorAll)this.chromosome).Points,
                    ((WeightVectorAll)this.chromosome).Smoothness, ((WeightVectorAll)otherChromosome.chromosome).Snake, ((WeightVectorAll)otherChromosome.chromosome).Trapped_penalty);
                WeightVectorChromosomeAll child2 = new WeightVectorChromosomeAll(((WeightVectorAll)otherChromosome.chromosome).Corner, ((WeightVectorAll)otherChromosome.chromosome).Empty_cells,
                    ((WeightVectorAll)otherChromosome.chromosome).Highest_tile, ((WeightVectorAll)otherChromosome.chromosome).Monotonicity, ((WeightVectorAll)otherChromosome.chromosome).Points,
                    ((WeightVectorAll)otherChromosome.chromosome).Smoothness, ((WeightVectorAll)this.chromosome).Snake, ((WeightVectorAll)this.chromosome).Trapped_penalty);
                return new Tuple<WeightVectorChromosome, WeightVectorChromosome>(child1, child2);
            }
            else if (crossoverPoint == 7)
            {
                WeightVectorChromosomeAll child1 = new WeightVectorChromosomeAll(((WeightVectorAll)this.chromosome).Corner, ((WeightVectorAll)this.chromosome).Empty_cells,
                    ((WeightVectorAll)this.chromosome).Highest_tile, ((WeightVectorAll)this.chromosome).Monotonicity, ((WeightVectorAll)this.chromosome).Points,
                    ((WeightVectorAll)this.chromosome).Smoothness, ((WeightVectorAll)this.chromosome).Snake, ((WeightVectorAll)otherChromosome.chromosome).Trapped_penalty);
                WeightVectorChromosomeAll child2 = new WeightVectorChromosomeAll(((WeightVectorAll)otherChromosome.chromosome).Corner, ((WeightVectorAll)otherChromosome.chromosome).Empty_cells,
                    ((WeightVectorAll)otherChromosome.chromosome).Highest_tile, ((WeightVectorAll)otherChromosome.chromosome).Monotonicity, ((WeightVectorAll)otherChromosome.chromosome).Points,
                    ((WeightVectorAll)otherChromosome.chromosome).Smoothness, ((WeightVectorAll)otherChromosome.chromosome).Snake, ((WeightVectorAll)this.chromosome).Trapped_penalty);
                return new Tuple<WeightVectorChromosome, WeightVectorChromosome>(child1, child2);
            }

            else throw new Exception();
        }


        // Mutation
        public override WeightVectorChromosome Mutate(Random random)
        {

            int randFeatureIndex = random.Next(0, CHROMOSOME_LENGTH);

            if (randFeatureIndex == 0)
            {
                double min = -((WeightVectorAll)this.chromosome).Corner;
                double max = ((WeightVectorAll)this.chromosome).Corner;
                double mutation = random.NextDouble() * (max - min) + min;
                return new WeightVectorChromosomeAll(((WeightVectorAll)this.chromosome).Corner + mutation, ((WeightVectorAll)this.chromosome).Empty_cells, 
                    ((WeightVectorAll)this.chromosome).Highest_tile, ((WeightVectorAll)this.chromosome).Monotonicity, ((WeightVectorAll)this.chromosome).Points, 
                    ((WeightVectorAll)this.chromosome).Smoothness, ((WeightVectorAll)this.chromosome).Snake, ((WeightVectorAll)this.chromosome).Trapped_penalty);
            }
            else if (randFeatureIndex == 1)
            {
                double min = -((WeightVectorAll)this.chromosome).Empty_cells;
                double max = ((WeightVectorAll)this.chromosome).Empty_cells;
                double mutation = random.NextDouble() * (max - min) + min;
                return new WeightVectorChromosomeAll(((WeightVectorAll)this.chromosome).Corner, ((WeightVectorAll)this.chromosome).Empty_cells + mutation,
                    ((WeightVectorAll)this.chromosome).Highest_tile, ((WeightVectorAll)this.chromosome).Monotonicity, ((WeightVectorAll)this.chromosome).Points,
                    ((WeightVectorAll)this.chromosome).Smoothness, ((WeightVectorAll)this.chromosome).Snake, ((WeightVectorAll)this.chromosome).Trapped_penalty);
            }
            else if (randFeatureIndex == 2)
            {
                double min = -((WeightVectorAll)this.chromosome).Highest_tile;
                double max = ((WeightVectorAll)this.chromosome).Highest_tile;
                double mutation = random.NextDouble() * (max - min) + min;
                return new WeightVectorChromosomeAll(((WeightVectorAll)this.chromosome).Corner, ((WeightVectorAll)this.chromosome).Empty_cells,
                    ((WeightVectorAll)this.chromosome).Highest_tile + mutation, ((WeightVectorAll)this.chromosome).Monotonicity, ((WeightVectorAll)this.chromosome).Points,
                    ((WeightVectorAll)this.chromosome).Smoothness, ((WeightVectorAll)this.chromosome).Snake, ((WeightVectorAll)this.chromosome).Trapped_penalty);
            }
            else if (randFeatureIndex == 3)
            {
                double min = -((WeightVectorAll)this.chromosome).Monotonicity;
                double max = ((WeightVectorAll)this.chromosome).Monotonicity;
                double mutation = random.NextDouble() * (max - min) + min;
                return new WeightVectorChromosomeAll(((WeightVectorAll)this.chromosome).Corner, ((WeightVectorAll)this.chromosome).Empty_cells,
                    ((WeightVectorAll)this.chromosome).Highest_tile, ((WeightVectorAll)this.chromosome).Monotonicity + mutation, ((WeightVectorAll)this.chromosome).Points,
                    ((WeightVectorAll)this.chromosome).Smoothness, ((WeightVectorAll)this.chromosome).Snake, ((WeightVectorAll)this.chromosome).Trapped_penalty);
            }
            else if (randFeatureIndex == 4)
            {
                double min = -((WeightVectorAll)this.chromosome).Points;
                double max = ((WeightVectorAll)this.chromosome).Points;
                double mutation = random.NextDouble() * (max - min) + min;
                return new WeightVectorChromosomeAll(((WeightVectorAll)this.chromosome).Corner, ((WeightVectorAll)this.chromosome).Empty_cells,
                    ((WeightVectorAll)this.chromosome).Highest_tile, ((WeightVectorAll)this.chromosome).Monotonicity, ((WeightVectorAll)this.chromosome).Points + mutation,
                    ((WeightVectorAll)this.chromosome).Smoothness, ((WeightVectorAll)this.chromosome).Snake, ((WeightVectorAll)this.chromosome).Trapped_penalty);
            }
            else if (randFeatureIndex == 5)
            {
                double min = -((WeightVectorAll)this.chromosome).Smoothness;
                double max = ((WeightVectorAll)this.chromosome).Smoothness;
                double mutation = random.NextDouble() * (max - min) + min;
                return new WeightVectorChromosomeAll(((WeightVectorAll)this.chromosome).Corner, ((WeightVectorAll)this.chromosome).Empty_cells,
                    ((WeightVectorAll)this.chromosome).Highest_tile, ((WeightVectorAll)this.chromosome).Monotonicity, ((WeightVectorAll)this.chromosome).Points,
                    ((WeightVectorAll)this.chromosome).Smoothness + mutation, ((WeightVectorAll)this.chromosome).Snake, ((WeightVectorAll)this.chromosome).Trapped_penalty);
            }
            else if (randFeatureIndex == 6)
            {
                double min = -((WeightVectorAll)this.chromosome).Snake;
                double max = ((WeightVectorAll)this.chromosome).Snake;
                double mutation = random.NextDouble() * (max - min) + min;
                return new WeightVectorChromosomeAll(((WeightVectorAll)this.chromosome).Corner, ((WeightVectorAll)this.chromosome).Empty_cells,
                    ((WeightVectorAll)this.chromosome).Highest_tile, ((WeightVectorAll)this.chromosome).Monotonicity, ((WeightVectorAll)this.chromosome).Points,
                    ((WeightVectorAll)this.chromosome).Smoothness, ((WeightVectorAll)this.chromosome).Snake + mutation, ((WeightVectorAll)this.chromosome).Trapped_penalty);
            }
            else if (randFeatureIndex == 7)
            {
                double min = -((WeightVectorAll)this.chromosome).Trapped_penalty;
                double max = ((WeightVectorAll)this.chromosome).Trapped_penalty;
                double mutation = random.NextDouble() * (max - min) + min;
                return new WeightVectorChromosomeAll(((WeightVectorAll)this.chromosome).Corner, ((WeightVectorAll)this.chromosome).Empty_cells,
                    ((WeightVectorAll)this.chromosome).Highest_tile, ((WeightVectorAll)this.chromosome).Monotonicity, ((WeightVectorAll)this.chromosome).Points,
                    ((WeightVectorAll)this.chromosome).Smoothness, ((WeightVectorAll)this.chromosome).Snake, ((WeightVectorAll)this.chromosome).Trapped_penalty + mutation);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}
