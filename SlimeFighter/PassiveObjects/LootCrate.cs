using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SlimeFighter.PassiveObjects
{
    /// <summary>
    /// Class to represent the 2D crates in the playing area with options to make large and small
    /// </summary>
    public class LootCrate : HittableObject
    {
        private static readonly int _tileSize = 32;
        private bool _inactive;

        // A bool to indicate true if 1 tile sized and false for 2x2 tile sized crate
        private readonly bool _crateSmall;

        // Variables for rendering the crate
        private Texture2D texture;
        private SoundEffect hitSound;
        private readonly float scale = 1.0f;

        // Variables to hold the coordinates and the position on screen of the crate
        private int xPos;
        private int yPos;
        private Vector2 position;
        public int XPos => xPos;
        public int YPos => yPos;
        public Vector2 Position => position;

        // A bool to indicate whether the crate is spawned or not
        public bool Inactive => _inactive;

        /// <summary>
        /// Constructor for the crate object
        /// </summary>
        /// <param name="small">true for 1x1 crate and false for 2x2 crate</param>
        public LootCrate(bool small)
        {
            if (small)
            {
                scale = 0.5f;
            }
            else
            {
                scale = 1.0f;
            }
            _inactive = true;
        }

        /// <summary>
        /// A function to spawn a crate object into the playing grid
        /// </summary>
        /// <param name="x">the desired x coordinate to attempt spawning into</param>
        /// <param name="y">the desired y coordinate to attempt spawning into</param>
        /// <param name="grid">the grid to spawn the crate into</param>
        /// <returns>true if successful and false if failed</returns>
        public bool Spawn(int x, int y, ref int[,] grid)
        {
            bool spawned = false;
            int xLower = x;
            int xUpper = x;
            int yLower = y;
            int yUpper = y;
            int necessarySpace = 1;

            if (!_crateSmall)
            {
                necessarySpace = 2;
            }

            // Setting the max bounds for x and y
            int xBounds = grid.GetLength(0) - necessarySpace;
            int yBounds = grid.GetLength(1) - necessarySpace;

            // Attempting to find coordinate to spawn the loot crate
            while (!spawned)
            {
                // Iterate through viable x coords
                for (int i = xLower; i <= xUpper; i++)
                {
                    // Iterate through viable y coords
                    for (int j = yLower; j <= yUpper; j++)
                    {
                        // If it is a 1x1 crate and cell is open, it is spawnable
                        if (_crateSmall && grid[i,j] == (int)CellType.Open)
                        {
                            spawned = true;
                            // Start drawing the crate
                            _inactive = false;
                            // Set the coords to type Crate
                            grid[i, j] = (int)CellType.Crate;
                            // Position the crate
                            xPos = i;
                            yPos = j;
                            position = new Vector2((float)(XPos * _tileSize) + 30, (float)(YPos * _tileSize) + 125);
                            return true;
                        }
                        // Checking to see if current coordinates in a square down to the right of i & j are spawnable for a 2x2 crate
                        else if (grid[i, j] == (int)CellType.Open && grid[i + 1, j] == (int)CellType.Open &&
                            grid[i, j + 1] == (int)CellType.Open && grid[i + 1, j + 1] == (int)CellType.Open)
                        {
                            spawned = true;
                            // Start drawing the crate
                            _inactive = false;
                            // Set the coords to type Crate
                            grid[i, j] = (int)CellType.Crate;
                            grid[i, j + 1] = (int)CellType.Crate;
                            grid[i + 1, j] = (int)CellType.Crate;
                            grid[i + 1, j + 1] = (int)CellType.Crate;
                            // Position the crate
                            xPos = i;
                            yPos = j;
                            position = new Vector2((float)(XPos * _tileSize) + 30, (float)(YPos * _tileSize) + 125);
                            return true;
                        }
                    }
                }

                // Adjusting the coordinates to expand search area
                if (xLower > 0) xLower--;
                if (xUpper < xBounds) xUpper++;
                if (yLower > 0) yLower--;
                if (yUpper < yBounds) yUpper++;

                // reached the end of the search area and nothing found
                if (xLower == 0 && yLower == 0 && xUpper == xBounds && yUpper == yBounds)
                {
                    return false;
                }
            }

            return false;
        }

        public void LoadContent(ContentManager content)
        {
            texture = content.Load<Texture2D>("PNGs/Crate");
            hitSound = content.Load<SoundEffect>("KnockDown");
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!_inactive)
            {
                spriteBatch.Draw(texture, Position, new Rectangle(0, 0, 64, 64),
                Color.White, 0, new Vector2(0, 0), scale, SpriteEffects.None, 0);
            }
        }

        /// <summary>
        /// A function to be used to make the crate inactive after a hit
        /// </summary>
        /// <param name="damage">placeholder just to confirm crate hit and compatiblity with collision checks</param>
        public void TakeDamage(int damage)
        {
            if (damage > 0)
            {
                _inactive = true;
                hitSound.Play();
            }
        }
    }
}
