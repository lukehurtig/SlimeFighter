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
        private readonly int value;

        // Variables to hold the position, texture, and whether the potion is Available for spawn
        private readonly int xPos;
        private readonly int yPos;
        private Texture2D texture;
        private Vector2 position;
        public int XPos => xPos;
        public int YPos => yPos;
        public bool Available => _potionCollected;
        public Texture2D Texture => texture;

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
        /// A quick a dirty method to get around setting the texture for new instantiations
        /// </summary>
        /// <param name="text">the texture to set to the object</param>
        public void SetTexture(Texture2D text)
        {
            texture = text;
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

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!_potionCollected) spriteBatch.Draw(texture, position, Color.White);
        }
    }
}
