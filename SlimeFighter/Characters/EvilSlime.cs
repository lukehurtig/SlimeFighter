using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;
using System.Runtime.CompilerServices;
using System.ComponentModel.Design;
using System;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace SlimeFighter.Characters
{
    public class EvilSlime : HittableObject
    {
        /// <summary>
        /// Upkeep animation timers and booleans for states
        /// </summary>
        private double animationTimer = 0;
        private double waitTime = 0.6f;
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
        private SoundEffect hitSound;
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
        public float Scale => scale;
        public AttackType AttackClass = AttackType.Plus;

        public EvilSlime()
        {
            xPos = 1;
            yPos = 1;
            maxHealth = 10;
            health = maxHealth;
            attack = 1;
            attackDistance = 1;
            speed = 1;
            direction = 'E';
        }

        public void LoadContent(ContentManager content)
        {
            texture = content.Load<Texture2D>("PNGs/SlimeCharacter(WithAlpha)");
            hitSound = content.Load<SoundEffect>("Hit");
            deathSound = content.Load<SoundEffect>("Death");
        }

        /// <summary>
        /// A version of update for the enemy slime AI that uses the game grid and player x and y positions
        /// </summary>
        /// <param name="gameTime">the game timer to read how much time has passed since the lasst update</param>
        /// <param name="gameGrid">the game grid listing other objects locations</param>
        /// <param name="playerX">the player's x coordinate</param>
        /// <param name="playerY">the player's y coordinate</param>
        public void Update(GameTime gameTime, ref int[,] gameGrid, int playerX, int playerY)
        {
            attacking = false;
            if (death) return;
            if (health <= 0)
            {
                animationIndex = 0;
                death = true;
                deathSound.Play(0.5f, 0, 0);
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

                if (AttackingRange(gameGrid))
                {
                    attacking = true;
                    CooldownTimer -= waitTime;
                }
                else if (Math.Abs(distanceX) >= Math.Abs(distanceY))
                {
                    if (distanceX >= 1 && xPos < gameGrid.GetLength(0) - 1 && gameGrid[xPos + 1, yPos] != (int)CellType.Slime)
                    {
                        if (gameGrid[xPos + 1, yPos] < (int)CellType.ImpassObj1)
                        {
                            xPos += 1;  // Move right
                        }
                    }
                    else if (distanceX < 1 && xPos > 0 && gameGrid[xPos - 1, yPos] != (int)CellType.Slime)
                    {
                        if (gameGrid[xPos - 1, yPos] < (int)CellType.ImpassObj1)
                        {
                            xPos -= 1;  // Move left
                        }
                    }
                }
                else
                {
                    if (distanceY > 1 && yPos < gameGrid.GetLength(1) - 1 && gameGrid[xPos, yPos + 1] != (int)CellType.Slime)
                    {
                        if (gameGrid[xPos, yPos + 1] < (int)CellType.ImpassObj1)
                        {
                            yPos += 1;  // Move down
                        }
                    }
                    else if (distanceY <= 1 && yPos > 0 && gameGrid[xPos, yPos - 1] != (int)CellType.Slime)
                    {
                        if (gameGrid[xPos, yPos - 1] < (int)CellType.ImpassObj1)
                        {
                            yPos -= 1;  // Move up
                        }
                    }
                }
                gameGrid[xPos, yPos] = (int)CellType.EvilSlime;
                CooldownTimer -= waitTime;
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

        /// <summary>
        /// A function to respawn the enemy slime with new attributes
        /// </summary>
        /// <param name="iniPosX">the starting x coord for the enenmy slime</param>
        /// <param name="iniPosY">the starting y coord for the enenmy slime</param>
        /// <param name="newHealth">the new health value for the enemy slime</param>
        /// <param name="newAttack">the new attack value for the enemy slime</param>
        public void NewValues(int iniPosX, int iniPosY, int newHealth, int newAttack)
        {
            xPos = iniPosX;
            yPos = iniPosY;
            maxHealth = newHealth;
            health = newHealth;
            attack = newAttack;
            attackDistance = 1;
            speed = 1;
            scale = 0.115f;
            if (xPos <= 13) direction = 'E';
            else direction = 'W';
            death = false;
        }

        public void ReplaceSlime(int posX, int posY, int newHealth, int newMaxHealth,  int newAttack, double currentCooldown)
        {
            xPos = posX;
            yPos = posY;
            maxHealth = newHealth;
            health = newHealth;
            maxHealth = newMaxHealth;
            attack = newAttack;
            CooldownTimer = currentCooldown;
            attackDistance = 1;
            speed = 1;
            scale = 0.115f;
            if (xPos <= 13) direction = 'E';
            else direction = 'W';
            death = false;
        }

        /// <summary>
        /// A public function to heal the enemy slime if I choose to add a game element that does so
        /// </summary>
        /// <param name="amount">the desired amount to heal the enemy slime</param>
        public void Heal(int amount)
        {
            if (amount + health < maxHealth) health += amount;
            else health = maxHealth;
        }

        /// <summary>
        /// A function to apply damage to the enemy slime and make it compatible witht the HittableObject Interface
        /// </summary>
        /// <param name="amount">the amount of damage to apply to the enemy slime</param>
        public void TakeDamage(int amount)
        {
            damaged = true;
            animationFrame = 0;
            hitSound.Play();

            if (health - amount < 0)
            {
                health = 0;
            }
            else
            {
                health -= amount;
            }
        }

        /// <summary>
        /// A helper function to check if the enemy slime should be in attacking mode, moved it out here
        /// to uncrowd the Update loop
        /// </summary>
        /// <param name="gameGrid">the game grid listing other objects locations</param>
        /// <returns></returns>
        public bool AttackingRange(int[,] gameGrid)
        {
            int xLowerBounds, xUpperBounds, yLowerBounds, yUpperBounds;

            if (xPos < attackDistance) xLowerBounds = 0;
            else xLowerBounds = xPos - attackDistance;

            if (yPos < attackDistance) yLowerBounds = attackDistance;
            else yLowerBounds = yPos - attackDistance;

            if ((xPos + attackDistance) > gameGrid.GetLength(0) - 1) xUpperBounds = gameGrid.GetLength(0) - 1;
            else xUpperBounds = xPos + attackDistance;

            if ((yPos + attackDistance) > gameGrid.GetLength(1) - 1) yUpperBounds = gameGrid.GetLength(1) - 1;
            else yUpperBounds = yPos + attackDistance;

            if (AttackClass == AttackType.Plus)
            {
                for (int x = xLowerBounds; x <= xUpperBounds; x++)
                {
                    if (gameGrid[x, yPos] == (int)CellType.Slime)
                    {
                        attacking = true;
                        CooldownTimer = 0;
                        return true;
                    }
                }
                for (int y = yLowerBounds; y <= yUpperBounds; y++)
                {
                    if (gameGrid[xPos, y] == (int)CellType.Slime)
                    {
                        attacking = true;
                        CooldownTimer = 0;
                        return true;
                    }
                }
            }
            else if (AttackClass == AttackType.Radius)
            {
                for (int x = xLowerBounds; x <= xUpperBounds; x++)
                {
                    for (int y = yLowerBounds; y <= yUpperBounds; y++)
                    {
                        if (gameGrid[x, y] == (int)CellType.Slime)
                        {
                            attacking = true;
                            CooldownTimer = 0;
                            return true;
                        }
                    }
                }
            }
            else
            {
                switch (direction)
                {
                    case 'N':
                        xLowerBounds = xUpperBounds = xPos;
                        yUpperBounds = yPos;
                        break;
                    case 'W':
                        xUpperBounds = xPos;
                        yLowerBounds = yUpperBounds = yPos;
                        break;
                    case 'S':
                        xLowerBounds = xUpperBounds = xPos;
                        yLowerBounds = yPos;
                        break;
                    default:
                        xLowerBounds = xPos;
                        yLowerBounds = yUpperBounds = yPos;
                        break;
                }

                for (int x = xLowerBounds; x <= xUpperBounds; x++)
                {
                    for (int y = yLowerBounds; y <= yUpperBounds; y++)
                    {
                        if (gameGrid[x, y] == (int)CellType.Slime)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
