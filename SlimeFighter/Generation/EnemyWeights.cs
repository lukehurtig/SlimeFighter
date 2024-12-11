using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimeFighter.Generation
{
    public class EnemyWeights
    {
        /// <summary>
        /// The multipliers to influence the stats of the spawned slime enemies
        /// </summary>
        public double DifficultyThreshold { get; set; } = 30.0; // Total allowable difficulty contribution
        public int BaseEnemyCount { get; set; } = 2; // Minimum enemies per round
        public double RoundWeight { get; set; } = 0.4; // Gives a weight for the round number
        public double PlayerStatsWeight { get; set; } = 0.5; // Gives a weight for cumulative players stats
        public double AttackTypeWeight { get; set; } = 0.1; // Gives a weight for attack type
        public double EnemyCountMultiplier { get; set; } = 1.0; // Extra enemies per difficulty rating

        /// <summary>
        /// Flooring stats for the enemies
        /// </summary>
        public double BaseHP { get; set; } = 5; // Minimum HP
        public double HPMultiplier { get; set; } = 1.2; // HP scaling factor
        public double BaseAttack { get; set; } = 2; // Minimum Attack
        public double AttackMultiplier { get; set; } = 1.0; // Attack scaling factor
    }
}
