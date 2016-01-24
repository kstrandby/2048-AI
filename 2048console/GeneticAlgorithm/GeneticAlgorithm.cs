using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2048console.GeneticAlgorithm
{
    // Class containing genetic algorithm logic
    public class GA
    {
        // constants
        private const double MAX_WEIGHT = 5.0;
        private const double MIN_WEIGHT = 0.0;
        private const double CHANGE_THRESHOLD = 10000;
        private const int MAX_NUM_OF_NO_IMPROVEMENTS = 10;
        
        private Random random = new Random();
        private int populationSize;
        private int survivorSize;
        private int iterations;
        private List<WeightVectorChromosome> population;
        private double previousPopulationFitness;
        private int numOfNoImprovementIterations;

        public GA(int populationSize, int survivorSize, int iterations)
        {
            this.populationSize = populationSize;
            this.survivorSize = survivorSize;
            this.iterations = iterations;
            this.numOfNoImprovementIterations = 0;
            this.previousPopulationFitness = 0;
            population = new List<WeightVectorChromosome>(populationSize);
        }

        // runs the genetic algorithm until termination condition
        public void RunAlgorithm()
        {
            Console.WriteLine("Initializing population with " + populationSize + " random chromosomes...");
            InitializePopulation();

            for (int i = 0; i < iterations; i++)
            {
                // calculate total fitness score for current population
                double totalFitness = EvaluatePopulation();
                
                if (i > 0) // check termination condition
                {
                    double improvement = totalFitness - previousPopulationFitness;
                    if (improvement < CHANGE_THRESHOLD) // overall population improvement is less than threshold
                    {
                        // number of iterations with no population improvement have reached limit - termination
                        if (numOfNoImprovementIterations >= MAX_NUM_OF_NO_IMPROVEMENTS)
                        {
                            Console.WriteLine("Terminating condition reached - best chromosome is: " + GetBestChromosome().ToString() + " with fitness: " + GetBestChromosome().Fitness);
                            break;
                        }
                        else
                        {
                            numOfNoImprovementIterations++;
                            previousPopulationFitness = totalFitness;

                        }
                        Console.WriteLine("No significant improvement from last population - number of times of no improvement: " + numOfNoImprovementIterations);
                    }
                    else
                    {
                        previousPopulationFitness = totalFitness;
                        numOfNoImprovementIterations = 0;
                    }
                }
                else
                {
                    previousPopulationFitness = totalFitness;
                    numOfNoImprovementIterations = 0;
                }
                
                // Write out population info
                Console.WriteLine(i + ": Total fitness of population: " + totalFitness + ", fitness of best chromosome: " + GetBestChromosome().Fitness + "\nSurvivors: ");
                int c = 0;
                foreach (WeightVectorChromosome chromosome in GetSurvivors())
                {
                    Console.WriteLine("\t" + c + ":" + chromosome.ChromosomeValue.ToString());
                    Console.WriteLine("\tFitness: " + chromosome.Fitness + ", Number of testruns: " + chromosome.GetNumberOfTestRuns());
                    c++;
                }
                RunEvolution(totalFitness);
            }
            Console.WriteLine("DONE!");
            Console.ReadLine();
        }

        // Runs the evolution
        private void RunEvolution(double totalFitness)
        {
            // Create list to hold new population and add the survivors of current population to list
            List<WeightVectorChromosome> newPopulation = new List<WeightVectorChromosome>();
            List<WeightVectorChromosome> survivors = GetSurvivors();
            foreach (WeightVectorChromosome survivor in survivors)
            {
                newPopulation.Add(survivor);
            }

            Random rand = new Random(Guid.NewGuid().GetHashCode());

            // Keep adding mutations until new population is desired size
            while (newPopulation.Count < populationSize)
            {
                // Select two chromosomes using roulette wheel selection
                WeightVectorChromosome parent1 = RouletteWheelSelection(totalFitness);
                WeightVectorChromosome parent2 = RouletteWheelSelection(totalFitness);

                // Crossover
                Tuple<WeightVectorChromosome, WeightVectorChromosome> children = parent1.Crossover(parent2, rand);
                WeightVectorChromosome child1 = children.Item1;
                WeightVectorChromosome child2 = children.Item2;

                // Mutation
                child1 = child1.Mutate(rand);
                child2 = child2.Mutate(rand);

                // add to population
                newPopulation.Add(child1);
                newPopulation.Add(child2);
            }
            population = newPopulation;

        }

        // Selects a chromosome from current population based on roulette wheel selection
        private WeightVectorChromosome RouletteWheelSelection(double totalFitness)
        {
            double rand = random.NextDouble() * totalFitness;
            foreach (WeightVectorChromosome chromosome in population)
            {
                rand -= chromosome.Fitness;
                if (rand <= 0) return chromosome;
            }
            throw new Exception();
        }

        // Returns the best chromosome in the population, i.e. chromosome with highest fitness
        private WeightVectorChromosome GetBestChromosome()
        {
            return population.OrderByDescending(o => o.Fitness).First();
        }

        // Returns a list containing the best chromosomes of current population (based on their fitness)
        private List<WeightVectorChromosome> GetSurvivors()
        {
            // sort the population according to fitness score and take first N chromosomes (N is number of survivors used)
            List<WeightVectorChromosome> survivors = population.OrderByDescending(o => o.Fitness).Take(survivorSize).ToList();
            return survivors;
        }

        // Creates as many random chromosomes as specified by populationsize
        // and adds them to the population
        public void InitializePopulation()
        {
            for (int i = 0; i < populationSize - 1; i++)
            {
                WeightVectorChromosome chromosome = GenerateRandomChromosome();
                population.Add(chromosome);
                Console.WriteLine(chromosome.ChromosomeValue.ToString());
            }
        }

        // Evaluates the population, returns the total fitness score for entire population
        public double EvaluatePopulation()
        {
            int cursorTop = Console.CursorTop;
            Console.Write("Evalutating population: ");
            int cursorLeft = Console.CursorLeft;

            double total = 0;
            int count = 0;
            foreach (WeightVectorChromosome chromosome in population)
            {
                total += chromosome.GetUpdatedFitness();
                count++;
                Console.SetCursorPosition(cursorLeft, cursorTop);
                Console.Write((double)count/populationSize * 100 + "%       ");
            }
            Console.SetCursorPosition(0, cursorTop);
            return total;
        }

        // Generates a chromosome with random genes
        private WeightVectorChromosome GenerateRandomChromosome()
        { 
            return new WeightVectorChromosomeAll(GetRandomWeight(), GetRandomWeight(), GetRandomWeight(), GetRandomWeight(), GetRandomWeight(), GetRandomWeight(), GetRandomWeight(), GetRandomWeight());
        }

        // Returns random double between MAX_WEIGHT and MIN_WEIGHT
        private double GetRandomWeight()
        {
            return random.NextDouble() * (MAX_WEIGHT - MIN_WEIGHT) + MIN_WEIGHT;
        }
    }
}
