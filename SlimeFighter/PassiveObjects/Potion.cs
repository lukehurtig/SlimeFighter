using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SlimeFighter.PassiveObjects
{
    /// <summary>
    /// This class is essentially a static reference to an instantiated potion object
    /// that maintains its position and healing value until respawned in game loop
    /// </summary>
    public class Potion
    {
        private bool _potionCollected;
        private static int _tileSize = 32;
        // The amount the potion heals the character
        private int value;

        // Variables to hold the position, texture, and whether the potion is Available for spawn
        private int xPos;
        private int yPos;
        private Texture2D texture;
        private Vector2 position;
        public int XPos => xPos;
        public int YPos => yPos;
        public bool Available => _potionCollected;

        /// <summary>
        /// A constructor for the potion object
        /// </summary>
        /// <param name="x">the desired x position for the potion on the game grid</param>
        /// <param name="y">the desired y position for the potion on the game grid</param>
        /// <param name="healAmount">the amount the potion will heal the character</param>
        public Potion(int x, int y, int healAmount)
        {
            xPos = x;
            yPos = y;
            position = new Vector2((float)(XPos * _tileSize) + 30, (float)(YPos * _tileSize) + 125);
            value = healAmount;
            _potionCollected = false;
        }

        /// <summary>
        /// A function to call when the player collects the potion and make it inactive
        /// </summary>
        /// <returns>how much the potion heals the player</returns>
        public int Collect()
        {
            _potionCollected = true;
            return value;
        }

        /// <summary>
        /// A function to spawn the potion after the player has constucted it and it is available
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="healAmount"></param>
        /// <returns>whether the potion was spawned or not</returns>
        public bool Spawn(int x, int y, int healAmount)
        {
            if (!_potionCollected)
            {
                return false;
            }
            else
            {
                xPos = x;
                yPos = y;
                position = new Vector2((float)(XPos * _tileSize) + 30, (float)(YPos * _tileSize) + 125);
                value = healAmount;
                _potionCollected = false;
                return true;
            }
        }

        public void LoadContent(ContentManager content)
        {
            texture = content.Load<Texture2D>("PNGs/Potion");
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!_potionCollected) spriteBatch.Draw(texture, position, Color.White);
        }
    }
}
