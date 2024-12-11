using SlimeFighter.Characters;
using System;
using System.Collections.Generic;

namespace SlimeFighter.Generation
{
    public class ObjectGeneration
    {
        private Random _randomSeed = new Random();
        private Random _random = new Random();
        private EnemyWeights enemyWeights = new EnemyWeights();

        /// <summary>
        /// This function is used to create a weighted sum to be used in the spawning of enemies
        /// </summary>
        /// <param name="roundNum">the current round number</param>
        /// <param name="player">the player object to grab the stats from</param>
        /// <param name="enemyWeights"></param>
        /// <returns></returns>
        public double CalculateDifficulty(int roundNum, Slime player)
        {
            // Normalized factors
            double roundFactor = roundNum / 10.0; // Assume max rounds are 10+ for normalization
            double playerFactor = (player.Attack + player.Health + player.Speed) / 100.0; // Normalize to a max stat sum
            double attackTypeFactor = player.AttackDistance; // A value between 0.5 and 1.5, for example

            // Weights
            double roundWeight = enemyWeights.RoundWeight;
            double playerWeight = enemyWeights.PlayerStatsWeight;
            double attackTypeWeight = enemyWeights.AttackTypeWeight;

            // Weighted sum
            return (roundFactor * roundWeight) +
                   (playerFactor * playerWeight) +
                   (attackTypeFactor * attackTypeWeight);
        }

        /// <summary>
        /// A function that dynamically spawns in enemy slimes based on how strong the player is and how far they've gotten
        /// </summary>
        /// <param name="difficultyRating">a double that is to be calculated by the CalculateDifficulty function above</param>
        /// <param name="slimes">an array containing initialized EvilSlimes that are to be assigned in this function and returned</param>
        /// <param name="numberOfSlimes">a ref int that is to be changed to how many slimes were added to the array</param>
        /// <returns>an array containing the generated enemy slimes to spawned into the game world</returns>
        public EvilSlime[] SpawnEnemies(double difficultyRating, EvilSlime[] slimes, ref int totalEnemies, ref int[,] gameGrid)
        {
            _random = new Random(_randomSeed.Next());
            EvilSlime[] newSlimes = slimes;
            totalEnemies = 0;
            double difficultyThreshold = enemyWeights.DifficultyThreshold * difficultyRating; // Scaled threshold
            double remainingDifficulty = difficultyThreshold; // Track remaining difficulty budget

            // Minimum enemies to spawn
            int baseEnemies = enemyWeights.BaseEnemyCount;
            int maxAdditionalEnemies = (int)(difficultyRating * enemyWeights.EnemyCountMultiplier);

            // Determine total number of enemies (but won't exceed threshold)
            int enemyNumber = baseEnemies + _random.Next(0, maxAdditionalEnemies + 1);

            for (int i = 0; i < enemyNumber; i++)
            {
                if (remainingDifficulty <= 0)
                    break; // Stop if we hit the difficulty budget

                // Randomly generate enemy stats within bounds
                double enemyContribution = _random.NextDouble() * remainingDifficulty / (enemyNumber - i);
                int enemyHP = (int)(enemyWeights.BaseHP + enemyContribution * enemyWeights.HPMultiplier);
                int enemyAttack = (int)(enemyWeights.BaseAttack + enemyContribution * enemyWeights.AttackMultiplier);

                // Cap the stats so the total difficulty stays under the threshold
                double contribution = (enemyHP * enemyWeights.HPMultiplier) + (enemyAttack * enemyWeights.AttackMultiplier);
                if (contribution > remainingDifficulty)
                {
                    double scaleFactor = remainingDifficulty / contribution;
                    enemyHP = (int)(enemyHP * scaleFactor);
                    enemyAttack = (int)(enemyAttack * scaleFactor);

                    enemyHP = enemyHP < 4 ? 4 : enemyHP;
                    enemyAttack = enemyAttack < 1 ? 1 : enemyAttack;
                }

                remainingDifficulty -= (enemyHP * enemyWeights.HPMultiplier) + (enemyAttack * enemyWeights.AttackMultiplier);

                // Run the accessory function to find a valid spawn location for the new enemy slime
                (int, int) posValues = FindEnemySpawnLocation(gameGrid);
                if (posValues.Item1 > 27 || posValues.Item2 > 12) // Checking to make sure there was no error in the search function before assigning to array
                {
                    throw new Exception("Invalid Enemy Slime Spawn Location!: Likely error in FindEnemySpawnLocation function");
                }
                // Assigning the spawn location to the new enemy slime
                gameGrid[posValues.Item1, posValues.Item2] = (int)CellType.EvilSlime;

                // Add the enemy
                newSlimes[i].NewValues(posValues.Item1, posValues.Item2, enemyHP, enemyAttack);
                totalEnemies++;
            }

            return newSlimes;
        }

        /// <summary>
        /// A helper function that finds an available space for an enemy slime that is at least 1 space away from the player
        /// </summary>
        /// <param name="gameGrid">the game world to search for available spaces</param>
        /// <returns>the coordinates of an available space</returns>
        public (int, int) FindEnemySpawnLocation(int[,] gameGrid)
        {
            _random = new Random(_randomSeed.Next());
            int attempts = 0;
            bool flag = false;
            int UpperBoundx = gameGrid.GetLength(0) - 1;
            int UpperBoundy = gameGrid.GetLength(1) - 1;
            int x = _random.Next(UpperBoundx);
            int y = _random.Next(UpperBoundy);

            while (attempts < 100)
            {
                flag = false;
                if (gameGrid[x, y] != (int)CellType.Open)
                {
                    x = _random.Next(gameGrid.GetLength(0) - 1);
                    y = _random.Next(gameGrid.GetLength(1) - 1);
                    attempts++;
                    continue;
                }

                int LowerX = x == 0 ? 0 : x - 1;
                int LowerY = y == 0 ? 0 : y - 1;
                int UpperX = x == UpperBoundx ? UpperBoundx : x + 1;
                int UpperY = y == UpperBoundy ? UpperBoundy : y + 1;

                for (int j = LowerX; j <= UpperX; j++)
                {
                    for (int k = LowerY; k <= UpperY; k++)
                    {
                        if (gameGrid[j, k] == (int)CellType.Slime)
                        {
                            x = _random.Next(gameGrid.GetLength(0) - 1);
                            y = _random.Next(gameGrid.GetLength(1) - 1);
                            flag = true;
                            continue;
                        }
                    }
                    if (flag) continue;
                }

                if (!flag) return (x, y);
                attempts++;
            }

            return (0xFF,0xFF);
        }

        /// <summary>
        /// This function is purely temporary to fulfill gameplay loop by giving random stat increase, I will replace with a better loot table styled version in the future
        /// </summary>
        public (string attribute, int amount) RandomizeAttributes()
        {
            _random = new Random(_randomSeed.Next());
            // Attributes to randomize
            string[] attributes = { "Attack", "HP", "Speed", "Range" };

            // Choose a random attribute
            string selectedAttribute = attributes[_random.Next(attributes.Length)];

            // Generate a random amount between 1 and 2
            int amount = _random.Next(1, 3);

            if (selectedAttribute == "HP")
            {
                amount *= 2;
            }

            // Return the selected attribute and the random amount
            return (selectedAttribute, amount);
        }

        public (CellType item, int value) RandomizeDrop(int round, int value = 0)
        {
            double spawn = _random.NextDouble();
            float missRate;

            if (round < 40)
            {
                missRate = (1f + round) / 100f;
            }
            else
            {
                missRate = 0.4f;
            }

            if (spawn >= missRate)
            {
                return (CellType.Potion, value * 2);
            }

            return (CellType.Open, 0);
        }
    }
}
