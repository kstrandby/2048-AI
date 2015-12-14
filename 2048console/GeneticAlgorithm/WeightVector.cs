using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2048console.GeneticAlgorithm
{
    public class WeightVector
    {
        internal double Empty_cells { get; set; }
        internal double Highest_tile { get; set; }
        internal double Monotonicity { get; set; }
        internal double Points { get; set; }
        internal double Smoothness { get; set; }
        internal double Trapped_penalty { get; set; }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2}, {3}, {4}, {5})", Empty_cells, Highest_tile, Monotonicity, Points, Smoothness, Trapped_penalty);
        }
    }
}
