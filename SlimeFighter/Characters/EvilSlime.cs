using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;
using System.Runtime.CompilerServices;
using System.ComponentModel.Design;
using System;

namespace SlimeFighter.Characters
{
    public class EvilSlime : HittableObject
    {
        /// <summary>
        /// Upkeep animation timers and booleans for states
        /// </summary>
        private double animationTimer = 0;
        private double waitTime = 0.8f;
        private int animationFrame = 0;
        private int animationIndex = 0;
        private bool attacking = false;
        private bool damaged = false;
        private bool death = false;
        private float scale = 0.115f;
        private float tileSize = 32f;

        /// <summary>
        /// Attributes of the character
        /// </summary>
        private int health;
        private int maxHealth;
        private int attack;
        private int attackDistance;
        private int speed;

        /// <summary>
        /// Input vars, textures, and position variables
        /// </summary>
        private Texture2D texture;
        private int xPos;
        private int yPos;
        private SoundEffect deathSound;
        private char direction;
        private SpriteEffects spriteEffect = SpriteEffects.None;
        public double CooldownTimer = 0;

        /// <summary>
        /// Public facing attributes
        /// </summary>
        public Vector2 Position => 
            new Vector2((float)(xPos * tileSize) + 30, (float)(yPos * tileSize) + 125);
        public int XPos => xPos;
        public int YPos => yPos;
        public int Health => health;
        public int MaxHealth => maxHealth;
        public int Attack => attack;
        public int AttackDistance => attackDistance;
        public bool Attacking => attacking;
        public bool Damaged => damaged;
        public char Direction => direction;
        public bool Inactive => death;
        public AttackType AttackClass = AttackType.Plus;

        public EvilSlime(int iniPosX, int iniPosY)
        {
            this.xPos = iniPosX;
            this.yPos = iniPosY;
            this.health = 10;
            this.attack = 1;
            this.attackDistance = 1;
            this.speed = 1;
            this.direction = 'E';
        }

        public void LoadContent(ContentManager content)
        {
            texture = content.Load<Texture2D>("PNGs/SlimeCharacter(WithAlpha)");
            deathSound = content.Load<SoundEffect>("Death");
        }

        public void Update(GameTime gameTime, ref int[,] gameGrid, int playerX, int playerY)
        {
            attacking = false;
            if (death) return;
            if (health <= 0)
            {
                animationIndex = 0;
                death = true;
                deathSound.Play();
                gameGrid[xPos, yPos] = (int)CellType.Open;
                return;
            }

            CooldownTimer += gameTime.ElapsedGameTime.TotalSeconds;
            // Animation timer for visual effects
            animationTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (animationTimer > 0.2f)
            {
                animationFrame = (animationFrame + 1) % 4;
                animationTimer -= 0.2f;
            }

            if (CooldownTimer >= waitTime)
            {
                // Calculate distance to player
                int distanceX = playerX - xPos;
                int distanceY = playerY - yPos;

                // Basic movement logic towards the player
                gameGrid[xPos, yPos] = (int)CellType.Open;

                if (Math.Abs(distanceX) > Math.Abs(distanceY))
                {
                    if (distanceX > 1 && xPos < gameGrid.GetLength(0) - 1 && gameGrid[xPos + 1, yPos] != (int)CellType.Slime)
                    {
                        xPos += 1;  // Move right
                        CooldownTimer = 0;
                    }
                    else if (distanceX < 1 && xPos > 0 && gameGrid[xPos - 1, yPos] != (int)CellType.Slime)
                    {
                        xPos -= 1;  // Move left
                        CooldownTimer = 0;
                    }
                }
                else
                {
                    if (distanceY > 1 && yPos < gameGrid.GetLength(1) - 1 && gameGrid[xPos, yPos + 1] != (int)CellType.Slime)
                    {
                        yPos += 1;  // Move down
                        CooldownTimer = 0;
                    }
                    else if (distanceY < 1 && yPos > 0 && gameGrid[xPos, yPos - 1] != (int)CellType.Slime)
                    {
                        yPos -= 1;  // Move up
                        CooldownTimer = 0;
                    }
                }

                gameGrid[xPos, yPos] = (int)CellType.EvilSlime;
                // Attack if close enough
                if ((Math.Abs(distanceX) <= attackDistance && Math.Abs(distanceY) <= attackDistance) && !death)
                {
                    attacking = true;
                    CooldownTimer = 0;
                }

                gameGrid[xPos, yPos] = (int)CellType.EvilSlime;
            }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            Rectangle sourceRectangle = attacking
                ? new Rectangle(animationFrame * 320, (animationIndex + 1) * 320, 320, 320)
                : new Rectangle(animationFrame * 320, animationIndex * 320, 320, 320);

            if (death && scale > 0)
            {
                scale -= 0.001f;
                spriteBatch.Draw(texture, Position, new Rectangle(animationFrame * 320, animationIndex * 320, 320, 320),
                Color.Crimson, 0, new Vector2(0, 0), scale, spriteEffect, 0);
            }
            if (!death) spriteBatch.Draw(texture, Position, sourceRectangle,
                Color.Crimson, 0, Vector2.Zero, scale, spriteEffect, 0);
        }

        public void NewValues(int iniPosX, int iniPosY, int newHealth, int newAttack)
        {
            this.xPos = iniPosX;
            this.yPos = iniPosY;
            this.health = newHealth;
            this.attack = newAttack;
            this.attackDistance = 1;
            this.speed = 1;
            this.scale = 0.115f;
            if (xPos <= 13) this.direction = 'E';
            else this.direction = 'W';
            this.death = false;
        }

        public void Heal(int amount)
        {
            if (amount + health < maxHealth) health += amount;
            else health = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            damaged = true;
            animationFrame = 0;

            if (health - amount < 0)
            {
                health = 0;
            }
            else
            {
                health -= amount;
            }
        }
    }
}
