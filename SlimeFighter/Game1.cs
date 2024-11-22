﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static System.Formats.Asn1.AsnWriter;
using System.Transactions;
using SlimeFighter.Characters;
using System;
using Microsoft.Xna.Framework.Media;
using System.IO;
using Microsoft.VisualBasic;
using SlimeFighter._3DAssets;

namespace SlimeFighter
{
    public enum state
    {
        titleScreen,
        tutorial,
        gameLive,
        pause,
        lootChest,
        transition,
        gameOver
    }

    public class Game1 : Game
    {
        /// <summary>
        /// Objects and booleans used to get the game running
        /// </summary>
        private Random _random = new Random();
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteBatch _spriteBatch2;
        private Tilemap _tilemap;
        private state gameState;

        /// <summary>
        /// The game field representation
        /// </summary>
        private int[,] _gridSpaces = new int[28, 13];
        private static float _tileSize = 32f;

        /// <summary>
        /// Need to make classes for these objects just had to patch over to finish
        /// </summary>
        private bool _crateHit = false;
        private bool _potionCollected = false;
        private Texture2D crate2D;
        private Texture2D potion;
        private Vector2 cratePos = new Vector2((float)(13 * _tileSize) + 30, (float)(6 * _tileSize) + 125);
        private Vector2 cratePos2 = new Vector2((float)(14 * _tileSize) + 30, (float)(7 * _tileSize) + 125);

        /// <summary>
        /// All the necessary in-game objects probably could export some of these outside the Game class
        /// </summary>
        private Slime hero;
        private EvilSlime enemySlime;
        private Crate crate;
        private CirclingCamera camera;
        private Texture2D title;
        private Texture2D baseHP;
        private Texture2D HP;
        private Song intro;
        private Song gameplay;
        private SpriteFont spriteFont;
        
        /// <summary>
        /// Input reading variables
        /// </summary>
        private KeyboardState keyboardState;
        private KeyboardState previousKeyboardState;
        private GamePadState gamePadState;
        private GamePadState previousGamePadSate;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            int x = _random.Next(27);
            int y = _random.Next(12);
            gameState = state.titleScreen;

            // TODO: Add your initialization logic here
            _tilemap = new Tilemap("map.txt");
            hero = new Slime()
            {
                Active = true
            };

            while (x == (hero.Position.X - 30) / 32)
            {
                x = _random.Next(27);
            }
            while (y == (hero.Position.Y - 125) / 32)
            {
                y = _random.Next(12);
            }
            enemySlime = new EvilSlime(x, y);

            Array.Clear(_gridSpaces, 0, _gridSpaces.Length);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _spriteBatch2 = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            _tilemap.LoadContent(Content);
            hero.LoadContent(Content);
            enemySlime.LoadContent(Content);

            crate = new Crate(this, CrateType.Slats, Matrix.Identity);
            camera = new CirclingCamera(this, new Vector3(0, 3, 5), 2f);

            crate2D = Content.Load<Texture2D>("PNGs/Crate");
            potion = Content.Load<Texture2D>("PNGs/Potion");
            title = Content.Load<Texture2D>("PNGs/SlimeLogo");
            baseHP = Content.Load<Texture2D>("EmptyHPBar");
            HP = Content.Load<Texture2D>("FullHPBar");
            intro = Content.Load<Song>("MP3s/IntroSong");
            gameplay = Content.Load<Song>("MP3s/BeepBox-Song");
            spriteFont = Content.Load<SpriteFont>("Arial");

            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(intro);
        }

        protected override void Update(GameTime gameTime)
        {
            previousGamePadSate = gamePadState;
            previousKeyboardState = keyboardState;

            gamePadState = GamePad.GetState(PlayerIndex.One);
            keyboardState = Keyboard.GetState();

            switch (gameState)
            {
                case state.titleScreen:
                    if ((gamePadState.Buttons.Start == ButtonState.Pressed && previousGamePadSate.Buttons.Start != ButtonState.Pressed) ||
                        (keyboardState.IsKeyDown(Keys.Enter)) && !previousKeyboardState.IsKeyDown(Keys.Enter))
                    {
                        _gridSpaces = new int[28, 13];
                        MediaPlayer.Play(gameplay);
                        gameState = state.gameLive;
                    }
                    if (gamePadState.Buttons.Y == ButtonState.Pressed || keyboardState.IsKeyDown(Keys.Space))
                    {
                        gameState = state.tutorial;
                    }
                    if ((gamePadState.Buttons.Back == ButtonState.Pressed && previousGamePadSate.Buttons.Back != ButtonState.Pressed) ||
                        (keyboardState.IsKeyDown(Keys.Escape)) && !previousKeyboardState.IsKeyDown(Keys.Escape))
                    {
                        Exit();
                    }
                    break;

                case state.tutorial:
                    if ((gamePadState.Buttons.Start == ButtonState.Pressed && previousGamePadSate.Buttons.Start != ButtonState.Pressed) ||
                    (keyboardState.IsKeyDown(Keys.Enter)) && !previousKeyboardState.IsKeyDown(Keys.Enter))
                        gameState = state.titleScreen;
                    break;

                case state.gameLive:

                    /*
                     * Commented out becuase I am testing my new hit detection function DamageTaken()
                     * 
                    if (Vector2.Distance(hero.Position, enemySlime.Position) < 50f && hero.HasMoved && !enemySlime.Death)
                    {
                        // Apply damage if both slimes are attacking
                        if (hero.Attacking && !enemySlime.Attacking)
                            damageToEnemy = hero.Attack * 2;
                        else if (!hero.Attacking && enemySlime.Attacking)
                            damageToHero = enemySlime.Attack * 2;
                        if (hero.Attacking && enemySlime.Attacking)
                        {
                            damageToEnemy = hero.Attack;
                            damageToHero = enemySlime.Attack;
                        }
                    }
                    */                    

                    if (enemySlime.Death && !_potionCollected && Vector2.Distance(hero.Position, enemySlime.Position) < 10f)
                    {
                        hero.Heal(5);
                        _potionCollected = true;
                    }

                    if (enemySlime.Death && !_crateHit && (Vector2.Distance(hero.Position, cratePos) < 50f ||
                        Vector2.Distance(hero.Position, cratePos2) < 50f) && hero.Attacking)
                    {
                        _crateHit = true;
                        gameState = state.lootChest;
                    }

                    if (hero.HasMoved)
                    {
                        if (enemySlime.Attacking)
                        {
                            DamageCheck(enemySlime.XPos, enemySlime.YPos, enemySlime.AttackDistance, enemySlime.Attack, true, enemySlime.AttackClass, enemySlime.Direction);
                        }
                        enemySlime.Update(gameTime, ref _gridSpaces, hero.Position);
                    }
                    if (hero.Attacking)
                    {
                        DamageCheck(hero.XPos, hero.YPos, hero.AttackDistance, hero.Attack, false, hero.AttackClass, hero.Direction);
                    }
                    hero.Update(gameTime, ref _gridSpaces);

                    if (enemySlime.Death)
                    {
                        _gridSpaces[enemySlime.XPos, enemySlime.YPos] = (int)CellType.Potion;

                        _gridSpaces[13, 6] = (int)CellType.Crate;
                        _gridSpaces[13, 7] = (int)CellType.Crate;
                        _gridSpaces[14, 6] = (int)CellType.Crate;
                        _gridSpaces[14, 7] = (int)CellType.Crate;
                    }

                    if (hero.Death)
                    {
                        gameState = state.gameOver;
                    }

                    if ((gamePadState.Buttons.Start == ButtonState.Pressed && previousGamePadSate.Buttons.Start != ButtonState.Pressed) ||
                        (keyboardState.IsKeyDown(Keys.Enter)) && !previousKeyboardState.IsKeyDown(Keys.Enter))
                        gameState = state.pause;
                    break;

                case state.pause:
                    if ((gamePadState.Buttons.Start == ButtonState.Pressed && previousGamePadSate.Buttons.Start != ButtonState.Pressed) ||
                    (keyboardState.IsKeyDown(Keys.Enter)) && !previousKeyboardState.IsKeyDown(Keys.Enter))
                        gameState = state.gameLive;

                    if ((gamePadState.Buttons.Back == ButtonState.Pressed && previousGamePadSate.Buttons.Back != ButtonState.Pressed) ||
                        (keyboardState.IsKeyDown(Keys.Escape)) && !previousKeyboardState.IsKeyDown(Keys.Escape))
                    {
                        gameState = state.titleScreen;
                        hero.ResetValues();
                        enemySlime.NewValues(_random.Next(27), _random.Next(12), _random.Next(20) + 1, _random.Next(3) + 1);
                        MediaPlayer.Play(intro);
                    }
                    break;

                case state.lootChest:
                    camera.Update(gameTime);

                    if ((gamePadState.Buttons.Start == ButtonState.Pressed && previousGamePadSate.Buttons.Start != ButtonState.Pressed) ||
                        (keyboardState.IsKeyDown(Keys.Enter)) && !previousKeyboardState.IsKeyDown(Keys.Enter))
                    {
                        // Temp variables to get game running
                        _crateHit = false;
                        _potionCollected = false;
                        _gridSpaces[13, 6] = (int)CellType.Open;
                        _gridSpaces[13, 7] = (int)CellType.Open;
                        _gridSpaces[14, 6] = (int)CellType.Open;
                        _gridSpaces[14, 7] = (int)CellType.Open;

                        gameState = state.titleScreen;
                        hero.ResetValues();
                        enemySlime.NewValues(_random.Next(27), _random.Next(12), _random.Next(20) + 1, _random.Next(3) + 1);
                        MediaPlayer.Play(intro);
                    }

                    if ((gamePadState.Buttons.Back == ButtonState.Pressed && previousGamePadSate.Buttons.Back != ButtonState.Pressed) ||
                        (keyboardState.IsKeyDown(Keys.Escape)) && !previousKeyboardState.IsKeyDown(Keys.Escape))
                        Exit();
                    break;

                case state.transition:
                    // TODO: Still need to implement screen between stages and decisions for player to improve slime
                    break;

                case state.gameOver:
                    gameState = state.titleScreen;
                    hero.ResetValues();
                    enemySlime.NewValues(_random.Next(27), _random.Next(12), _random.Next(20) + 1, _random.Next(3) + 1);
                    MediaPlayer.Play(intro);
                    break;

                default:
                    // TODO make a screen that says you should never be here.
                    break;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            Matrix transform = Matrix.CreateScale(0.85f) *
                       Matrix.CreateTranslation(-10, -15, 0);

            _spriteBatch.Begin(transformMatrix: transform);
            // TODO: Add your drawing code here            
            _tilemap.Draw(gameTime, _spriteBatch);
            if (gameState > state.tutorial)
            {
                if (enemySlime.Death)
                {
                    if (!_potionCollected) _spriteBatch.Draw(potion, enemySlime.Position, Color.White);
                    if (!_crateHit) _spriteBatch.Draw(crate2D, cratePos, Color.White);
                }

                enemySlime.Draw(gameTime, _spriteBatch);
                hero.Draw(gameTime, _spriteBatch);

                int avg = (int)(180 * (float)hero.Health / (float)hero.MaxHealth);

                _spriteBatch.Draw(baseHP, new Vector2(30, 30), new Rectangle(0, 0, 180, 20), Color.White);
                _spriteBatch.Draw(HP, new Vector2(30, 30), new Rectangle(0, 0, avg, 20), Color.White);
            }
            _spriteBatch.End();
            
            _spriteBatch2.Begin();
            if (gameState == state.pause)
            {
                _spriteBatch2.DrawString(spriteFont, "    - Press Start or Enter to Resume -\n- Press Back or Esc to Return to Menu -",
                    new Vector2(_graphics.GraphicsDevice.Viewport.Width * 0.5f - 280,
                    (_graphics.GraphicsDevice.Viewport.Height * 0.5f) - 20), Color.LightGoldenrodYellow);
            }
            if (gameState == state.titleScreen)
            {
                _spriteBatch2.DrawString(spriteFont, "     - Press Start or Enter to Play -\nPress Y or Space Bar for Instructions\n  - Press Back or Esc to Exit Game -",
                    new Vector2((_graphics.GraphicsDevice.Viewport.Width / 2) - 250, 270), Microsoft.Xna.Framework.Color.LightGoldenrodYellow);
                _spriteBatch2.Draw(title, new Vector2((_graphics.GraphicsDevice.Viewport.Width / 2) - 200, 30), new Rectangle(0, 0, 2000, 1000),
                    Color.White, 0, new Vector2(0, 0), .2f, SpriteEffects.None, 0);
            }
            if (gameState == state.tutorial)
            {
                _spriteBatch2.DrawString(spriteFont, "- Move with WASD, Arrow Keys, or DPad\n- Press Space Bar or Right Trigger to Attack\n- Press Enter or Start to Pause the game\n-The goal of the game is to survive as many\n rounds as possible, every round you survive\n the stronger you get, but watch your health (HP)\n\n   - Press Enter or Start to Return to Menu -",
                    new Vector2(75, 100), Color.LightGoldenrodYellow);
            }
            if (gameState == state.lootChest)
            {
                crate.Draw(camera);
                _spriteBatch2.DrawString(spriteFont, "- This game is still a Work in Progress\n I intend to make it so each crate destroyed\n upgrades the player through stats,\n abilities or attack types.\n- Also more enemies to come with\n varied spawns and ammounts\n- Also AI could use a little work, but solid start\n\n   - Press Enter or Start to Return to Menu -\n   - Or Press ESC or Back to Exit the Game -",
                    new Vector2(75, 60), Color.LightGoldenrodYellow);
            }
            _spriteBatch2.End();            

            base.Draw(gameTime);
        }

        private bool DamageCheck(int xCord, int yCord, int range, int damage, bool enemy, AttackType attackType, char dir = 'W')
        {
            int xLowerBounds, xUpperBounds, yLowerBounds, yUpperBounds;
            if (xCord < range) xLowerBounds = 0;
            else xLowerBounds = xCord - range;

            if (yCord < range) yLowerBounds = range;
            else yLowerBounds = yCord - range;

            if ((xCord + range) > _gridSpaces.GetLength(0) - 1) xUpperBounds = _gridSpaces.GetLength(0) - 1;
            else xUpperBounds = xCord + range;

            if ((yCord + range) > _gridSpaces.GetLength(1) - 1) yUpperBounds = _gridSpaces.GetLength(1) - 1;
            else yUpperBounds = yCord + range;

            switch (attackType)
            {
                case AttackType.StraightAhead:
                    switch (dir)
                    {
                        case 'N':
                            xLowerBounds = xUpperBounds = xCord;
                            yUpperBounds = yCord;
                            break;
                        case 'W':
                            xUpperBounds = xCord;
                            yLowerBounds = yUpperBounds = yCord;
                            break;
                        case 'S':
                            xLowerBounds = xUpperBounds = xCord;
                            yLowerBounds = yCord;
                            break;
                        default:
                            xLowerBounds = xCord;
                            yLowerBounds = yUpperBounds = yCord;
                            break;
                    }

                    for (int x = xLowerBounds; x <= xUpperBounds; x++)
                    {
                        for (int y = yLowerBounds; y <= yUpperBounds; y++)
                        {
                            if (enemy)
                            {
                                if (_gridSpaces[x, y] == (int)CellType.Slime) hero.TakeDamage(damage);
                            }
                            /*
                            * TODO: Need to change once I add more than one enemy
                            */
                            else if (_gridSpaces[x, y] >= (int)CellType.EvilSlime)
                            {
                                enemySlime.TakeDamage(damage);
                            }
                        }
                    }
                    break;

                case AttackType.Plus:
                    for (int x = xLowerBounds; x <= xUpperBounds; x++)
                    {
                        if (enemy)
                        {
                            if (_gridSpaces[x, yCord] == (int)CellType.Slime) hero.TakeDamage(damage);
                        }
                        /*
                        * TODO: Need to change once I add more than one enemy
                        */
                        else if (_gridSpaces[x, yCord] >= (int)CellType.EvilSlime)
                        {                            
                            enemySlime.TakeDamage(damage);
                        }
                    }
                    for (int y = yLowerBounds; y <= yUpperBounds; y++)
                    {
                        if (enemy)
                        {
                            if (_gridSpaces[xCord, y] == (int)CellType.Slime) hero.TakeDamage(damage);
                        }
                        /*
                        * TODO: Need to change once I add more than one enemy
                        */
                        else if (_gridSpaces[xCord, y] >= (int)CellType.EvilSlime)
                        {                            
                            enemySlime.TakeDamage(damage);
                        }
                    }
                    break;

                case AttackType.Ranged:
                    if (enemy)
                    {
                        switch (dir)
                        {
                            case 'N':
                                if (_gridSpaces[xCord, yCord + range] == (int)CellType.Slime) hero.TakeDamage(damage);
                                break;
                            case 'W':
                                if (_gridSpaces[xCord - range, yCord] == (int)CellType.Slime) hero.TakeDamage(damage);
                                break;
                            case 'S':
                                if (_gridSpaces[xCord, yCord - range] == (int)CellType.Slime) hero.TakeDamage(damage);
                                break;
                            default:
                                if (_gridSpaces[xCord + range, yCord] == (int)CellType.Slime) hero.TakeDamage(damage);
                                break;
                        }
                    }
                    /*
                     * This will be implemented as more of a powerup where the use selects a tile to shoot at and 
                     * the coordinates of the selected tile will be fed into this function to check occupancy of enemy
                     */
                    else if (_gridSpaces[xCord, yCord] >= (int)CellType.EvilSlime)
                    {
                        enemySlime.TakeDamage(damage);
                    }
                    break;

                case AttackType.Shock:
                    for (int x = xLowerBounds; x <= xUpperBounds; x++)
                    {
                        for (int y = yLowerBounds; y <= yUpperBounds; y++)
                        {
                            if (enemy)
                            {
                                if (_gridSpaces[x, y] == (int)CellType.Slime && !hero.Damaged)
                                {
                                    hero.TakeDamage(damage);
                                    DamageCheck(x, y, 1, damage / 2, true, AttackType.Shock);
                                }
                            }
                            /*
                            * TODO: Need to change once I add more than one enemy and the if(!enemySlime.Damaged) to account
                            * for more enemies that have already been damaged... maybe check inside conditional with for loop?
                            */
                            else if (_gridSpaces[x, y] >= (int)CellType.EvilSlime && !enemySlime.Damaged)
                            {
                                enemySlime.TakeDamage(damage);
                                DamageCheck(x, y, 1, damage / 2, false, AttackType.Shock);
                            }
                        }
                    }
                    break;

                case AttackType.Radius:
                    for (int x = xLowerBounds; x <= xUpperBounds; x++)
                    {
                        for (int y = yLowerBounds; y <= yUpperBounds; y++)
                        {
                            if (enemy)
                            {
                                if (_gridSpaces[x, y] == (int)CellType.Slime) hero.TakeDamage(damage);
                            }
                            /*
                            * TODO: Need to change once I add more than one enemy
                            */
                            else if (_gridSpaces[x, y] >= (int)CellType.EvilSlime)
                            {
                                enemySlime.TakeDamage(damage);
                            }
                        }
                    }                    
                    break;

                case AttackType.Viral:
                    /*
                     * Not sure I know how I want to implement, but I want to work something like it is a weak attack, but once
                     * you kill one weak peon, all other enemies within a certain range of them will get killed instantly...
                     */
                    break;
            }
            return false;
        }
    }
}
