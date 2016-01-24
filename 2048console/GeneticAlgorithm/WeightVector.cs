using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2048console.GeneticAlgorithm
{
    public abstract class WeightVector { }

    // Weight vector class to represent a weight vector
    public class WeightVectorAll : WeightVector
    {
        internal double Corner { get; set; }
        internal double Empty_cells { get; set; }
        internal double Highest_tile { get; set; }
        internal double Monotonicity { get; set; }
        internal double Points { get; set; }
        internal double Smoothness { get; set; }
        internal double Snake { get; set; }
        internal double Trapped_penalty { get; set; }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})", Corner, Empty_cells, Highest_tile, Monotonicity, Points, Smoothness, Snake, Trapped_penalty);
        }
    }
}
