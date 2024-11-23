using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;
using System.Runtime.CompilerServices;
using System.ComponentModel.Design;
using System;

namespace SlimeFighter.Characters
{
    public class Slime
    {
        /// <summary>
        /// Upkeep animation timers and booleans for states
        /// </summary>
        private double animationTimer = 0;
        private double moveWaitTime;
        private double attackWaitTime;
        private int animationFrame = 0;
        private int animationIndex = 0;
        private bool attacking = false;
        private bool hasAttacked = false;
        private bool damaged = false;
        private bool death = false;
        private float scale = 0.13f;
        private static readonly float tileSize = 32f;
        private static readonly int MAX_SPEED = 25;
        private static readonly int MAX_ATTACK = 25;
        private static readonly int MAX_HP = 250;
        private static readonly int MAX_RANGE = 5;

        /// <summary>
        /// Attributes of the character
        /// </summary>
        private int health;
        private int maxHealth;
        private int attack;
        private int attackDistance;
        private int attackSpeed;
        private int speed;

        /// <summary>
        /// Input vars, textures, and position variables
        /// </summary>
        private KeyboardState keyboardState;
        private GamePadState gamePadState;
        private Texture2D texture;
        private int xPos = 2;
        private int yPos = 10;
        private SoundEffect deathSound;
        private char direction;
        private SpriteEffects spriteEffect = SpriteEffects.None;

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
        public int Speed => speed;
        public int AttackDistance => attackDistance;
        public bool HasAttacked => hasAttacked;
        public bool Attacking => attacking;
        public bool Damaged => damaged;
        public char Direction => direction;
        public double MoveWaitTime => moveWaitTime;
        public double CooldownTimer = 0;
        public AttackType AttackClass = AttackType.StraightAhead;
        public bool Death = false;
        public bool Active = false;
        public bool HasMoved = false;

        public Slime()
        {
            this.health = 10;
            this.maxHealth = 10;
            this.attack = 2;
            this.attackDistance = 1;
            this.speed = 1;
            this.attackSpeed = 1;
            this.moveWaitTime = 0.52f - (float)(speed * 0.02f);
            this.attackWaitTime = 0.52f - (float)(speed * 0.02f);
            this.direction = 'E';
        }

        public void LoadContent(ContentManager content)
        {
            texture = content.Load<Texture2D>("PNGs/SlimeCharacter(WithAlpha)");
            deathSound = content.Load<SoundEffect>("Death");
        }

        public void Update(GameTime gameTime, ref int[,] gameGrid)
        {
            hasAttacked = false;
            HasMoved = false;
            animationTimer += gameTime.ElapsedGameTime.TotalSeconds;
            CooldownTimer += gameTime.ElapsedGameTime.TotalSeconds;

            if (animationTimer > 0.2f)
            {
                if (animationFrame < 4) animationFrame++;
                else
                {
                    if (attacking) attacking = false;
                    if (damaged) damaged = false;
                    animationFrame = 0;
                }

                animationTimer -= 0.2f;
            }            
            
            if (Active && !death)
            {
                #region Input Handling
                GamePadState previousGamePadState = gamePadState;
                KeyboardState previousKeyboardState = keyboardState;

                gamePadState = GamePad.GetState(PlayerIndex.One);
                keyboardState = Keyboard.GetState();

                if (!HasMoved)
                {
                    if (!attacking && CooldownTimer > moveWaitTime)
                    {
                        if (((gamePadState.DPad.Right == ButtonState.Pressed && previousGamePadState.DPad.Right != ButtonState.Pressed) ||
                        (keyboardState.IsKeyDown(Keys.Right) && !previousKeyboardState.IsKeyDown(Keys.Right)) ||
                        (keyboardState.IsKeyDown(Keys.D) && !previousKeyboardState.IsKeyDown(Keys.D))) && xPos < 27)
                        {
                            if (gameGrid[xPos + 1, yPos] < (int)CellType.WallLeft)
                            {
                                gameGrid[xPos, yPos] = (int)CellType.Open;
                                xPos++;
                                direction = 'E';
                                CooldownTimer = 0;
                            }
                            else direction = 'E';
                            HasMoved = true;
                        }
                        else if (((gamePadState.DPad.Left == ButtonState.Pressed && previousGamePadState.DPad.Left != ButtonState.Pressed) ||
                            (keyboardState.IsKeyDown(Keys.Left) && !previousKeyboardState.IsKeyDown(Keys.Left)) ||
                            (keyboardState.IsKeyDown(Keys.A) && !previousKeyboardState.IsKeyDown(Keys.A))) && xPos > 0)
                        {
                            if (gameGrid[xPos - 1, yPos] < (int)CellType.WallLeft)
                            {
                                gameGrid[xPos, yPos] = (int)CellType.Open;
                                xPos--;
                                direction = 'W';
                                CooldownTimer = 0;
                            }
                            else direction = 'W';
                            HasMoved = true;
                        }
                        else if (((gamePadState.DPad.Up == ButtonState.Pressed && previousGamePadState.DPad.Up != ButtonState.Pressed) ||
                            (keyboardState.IsKeyDown(Keys.Up) && !previousKeyboardState.IsKeyDown(Keys.Up)) ||
                            (keyboardState.IsKeyDown(Keys.W) && !previousKeyboardState.IsKeyDown(Keys.W))) && yPos > 0)
                        {
                            if (gameGrid[xPos, yPos - 1] < (int)CellType.WallLeft)
                            {
                                gameGrid[xPos, yPos] = (int)CellType.Open;
                                yPos--;
                                direction = 'N';
                                CooldownTimer = 0;
                            }
                            else direction = 'N';
                            HasMoved = true;
                        }
                        else if (((gamePadState.DPad.Down == ButtonState.Pressed && previousGamePadState.DPad.Down != ButtonState.Pressed) ||
                            (keyboardState.IsKeyDown(Keys.Down) && !previousKeyboardState.IsKeyDown(Keys.Down)) ||
                            (keyboardState.IsKeyDown(Keys.S) && !previousKeyboardState.IsKeyDown(Keys.S))) && yPos < 12)
                        {
                            if (gameGrid[xPos, yPos + 1] < (int)CellType.WallLeft)
                            {
                                gameGrid[xPos, yPos] = (int)CellType.Open;
                                yPos++;
                                direction = 'S';
                                CooldownTimer = 0;
                            }
                            else direction = 'S';
                            HasMoved = true;
                        }
                        gameGrid[xPos, yPos] = (int)CellType.Slime;
                    }

                    if (((gamePadState.IsButtonDown(Buttons.RightTrigger) && !previousGamePadState.IsButtonDown(Buttons.RightTrigger)) ||
                        (keyboardState.IsKeyDown(Keys.Space) && !previousKeyboardState.IsKeyDown(Keys.Space))) && CooldownTimer > attackWaitTime)
                    {
                        hasAttacked = true;
                        attacking = true;
                        animationFrame = 0;
                        HasMoved = true;
                        CooldownTimer = 0;
                    }
                    #endregion Input Handling
                }

                if (health <= 0 && !death)
                {
                    deathSound.Play();
                    animationIndex = 0;
                    death = true;
                }
            }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (death && scale > 0)
            {
                scale -= 0.001f;
                spriteBatch.Draw(texture, Position, new Rectangle(animationFrame * 320, animationIndex * 320, 320, 320),
                Color.White, 0, new Vector2(16, 16), scale, spriteEffect, 0);
            }

            else if (!death)
            {
                if (direction == 'S' || direction == 'E') animationIndex = 0;
                else if (direction == 'N') animationIndex = 3;
                if (direction == 'E') spriteEffect = SpriteEffects.None;
                else if (direction == 'W') spriteEffect = SpriteEffects.FlipHorizontally;

                if (damaged) spriteBatch.Draw(texture, Position,
                    new Rectangle(animationFrame * 320, (animationIndex + 2) * 320, 320, 320),
                    Color.White, 0, new Vector2(0, 0), .115f, spriteEffect, 0);
                else if (attacking) spriteBatch.Draw(texture, Position,
                    new Rectangle(animationFrame * 320, (animationIndex + 1) * 320, 320, 320),
                    Color.White, 0, new Vector2(0, 0), .115f, spriteEffect, 0);
                else spriteBatch.Draw(texture, Position, new Rectangle(animationFrame * 320, animationIndex * 320, 320, 320),
                    Color.White, 0, new Vector2(0, 0), .115f, spriteEffect, 0);
            }
            else Death = true;
        }

        public void ResetValues()
        {
            xPos = 2;
            yPos = 10;
            this.health = 10;
            this.maxHealth = 10;
            this.attack = 2;
            this.attackDistance = 1;
            this.speed = 1;
            this.scale = 0.13f;
            this.direction = 'E';
            this.death = false;
            this.Death = false;
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
            health -= amount;
        }

        public bool IncreaseStat(string stat, int amount, AttackType attackType)
        {
            switch (stat)
            {
                case "Attack":
                    if (attack < MAX_ATTACK)
                    {
                        attack += amount;
                        return true;
                    }
                    return false;

                case "HP":
                    if (health < MAX_HP)
                    {
                        if (health + amount > maxHealth) health = maxHealth;
                        else health += amount;
                        maxHealth += amount;
                        return true;
                    }
                    return false;

                case "Speed":
                    if (speed < MAX_SPEED)
                    {
                        speed += amount;
                        return true;
                    }
                    return false;

                case "Range":
                    if (attackDistance < MAX_RANGE)
                    {
                        attackDistance += amount;
                        return true;
                    }
                    return false;

                case "Class":
                    AttackClass = attackType;
                    
                    if (amount > 0)
                    {
                        attackDistance = amount;
                    }

                    return true;
            }

            return false;
        }
    }
}
