using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SlimeFighter
{
    public class IndicationTile
    {
        private readonly static int _tileSize = 32;
        private readonly float scale = 0.25f;
        private double animationTimer = 0;
        private int animationFrame = 0;
        private Texture2D texture;

        private int xPos = 0;
        private int yPos = 0;
        private Vector2 position;
        public int XPos => xPos;
        public int YPos => yPos;
        public bool AnimationComplete = true;
        public CellType IndicatorType { get; set; }

        public void Spawn(int x, int y)
        {
            xPos = x;
            yPos = y;
            position = new Vector2((float)(XPos * _tileSize) + 30, (float)(YPos * _tileSize) + 125);
            animationTimer = 0;
            animationFrame = 0;
            IndicatorType = CellType.Open;
        }

        public void Activate(CellType type)
        {
            IndicatorType = type;
            animationFrame = 0;
            animationTimer = 0;
            AnimationComplete = false;
        }

        public void LoadContent(ContentManager content)
        {
            texture = content.Load<Texture2D>("PNGs/TileIndicator");
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            Color color = Color.White;

            animationTimer += gameTime.ElapsedGameTime.TotalSeconds;

            if (animationTimer > 0.08f)
            {
                if (animationFrame < 9) animationFrame++;
                else
                {                    
                    animationFrame = 0;
                    IndicatorType = CellType.Open;
                    AnimationComplete = true;
                }

                animationTimer -= 0.08f;
            }

            if (!AnimationComplete)
            {
                switch (IndicatorType)
                {
                    case CellType.EnemyIndicator:
                        color = Color.Yellow;
                        break;
                    case CellType.HitIndicator:
                        color = Color.Crimson;
                        break;
                }
                spriteBatch.Draw(texture, position, new Rectangle(animationFrame * 128, 0, 128, 128),
                        color, 0, new Vector2(0, 0), scale, SpriteEffects.None, 0);
            }
        }
    }
}
