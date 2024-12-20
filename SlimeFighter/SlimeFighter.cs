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
using System.Collections.Generic;
using SlimeFighter.PassiveObjects;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;
using SlimeFighter.Generation;

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

    public class SlimeFighter : Game
    {
        /// <summary>
        /// Objects and booleans used to get the game running
        /// </summary>
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteBatch _spriteBatch2;
        private Tilemap _tilemap;
        private ObjectGeneration objectGeneration;
        private state gameState;

        /// <summary>
        /// The game field representation
        /// </summary>
        private int[,] _gridSpaces = new int[28, 13];
        private IndicationTile[,] indicationTiles = new IndicationTile[28, 13];
        private static float _tileSize = 32f;

        /// <summary>
        /// Upkeep variables such as reset values, flags, and counts
        /// </summary>
        private bool _crateHit = false;
        private bool _lootScreenTransition = false;
        private string lootScreenStatText = string.Empty;
        private Vector3 cameraStartPosition = new Vector3(0, 3, 5);
        private int round = 0;
        private int enemySlimesCount = 0;
        private int clockAnimationFrame = 0;
        private float completeTextPos = -50f;
        private float transitionTextPos = -300f;
        private float clockAnimationTimer = 0;

        /// <summary>
        /// All the necessary in-game objects probably could export some of these outside the Game class
        /// </summary>
        private Slime hero;
        private EvilSlime enemySlime;
        private Crate crate;
        private CirclingCamera camera;
        private LootCrate mainCrate;
        private Texture2D potion;
        private Texture2D title;
        private Texture2D baseHP;
        private Texture2D HP;
        private Texture2D clock;
        private SoundEffect heal;
        private Song intro;
        private Song gameplay;
        private Song lootbox;
        private Song selectionScreen;
        private Song transitionScreen;
        private Song gameOver;
        private SpriteFont spriteFont;
        private SpriteFont gameOverHeader;

        /// <summary>
        /// Collections of objects used in the game
        /// </summary>
        private List<LootCrate> lootCrates = new List<LootCrate>();
        private List<Potion> potions = new List<Potion>();
        private EvilSlime[] enemySlimes = new EvilSlime[20];
        private List<HittableObject> hittableObjects = new List<HittableObject>();

        /// <summary>
        /// Input reading variables
        /// </summary>
        private KeyboardState keyboardState;
        private KeyboardState previousKeyboardState;
        private GamePadState gamePadState;
        private GamePadState previousGamePadSate;

        public SlimeFighter()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            gameState = state.titleScreen;

            _tilemap = new Tilemap("map.txt");
            objectGeneration = new ObjectGeneration();

            for (int i = 0; i < 28; i++)
            {
                for (int j = 0; j < 13; j++)
                {
                    indicationTiles[i, j] = new IndicationTile();
                    indicationTiles[i, j].Spawn(i, j);
                }
            }

            for (int i = 0; i < enemySlimes.Length; i++)
            {
                enemySlimes[i] = new EvilSlime();
            }

            hero = new Slime()
            {
                Active = true
            };
            _gridSpaces[hero.XPos, hero.YPos] = (int)CellType.Slime;

            (int, int) posValues = objectGeneration.FindEnemySpawnLocation(_gridSpaces);
            _gridSpaces[posValues.Item1, posValues.Item2] = (int)CellType.EvilSlime;
            enemySlimes[0].NewValues(posValues.Item1, posValues.Item2, 10, 1);
            enemySlimesCount = 1;
            hittableObjects.Add(enemySlimes[0]);

            mainCrate = new LootCrate(false);
            lootCrates.Add(mainCrate);

            Array.Clear(_gridSpaces, 0, _gridSpaces.Length);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _spriteBatch2 = new SpriteBatch(GraphicsDevice);

            _tilemap.LoadContent(Content);
            hero.LoadContent(Content);
            //enemySlime.LoadContent(Content);
            mainCrate.LoadContent(Content);

            foreach (EvilSlime evilSlime in enemySlimes)
            {
                evilSlime.LoadContent(Content);
            }

            crate = new Crate(this, CrateType.Slats, Matrix.Identity);
            camera = new CirclingCamera(this, new Vector3(0, 3, 5), 2f);

            foreach (var tile in indicationTiles)
            {
                tile.LoadContent(Content);
            }

            title = Content.Load<Texture2D>("PNGs/SlimeLogo");
            potion = Content.Load<Texture2D>("PNGs/Potion");
            baseHP = Content.Load<Texture2D>("EmptyHPBar");
            HP = Content.Load<Texture2D>("FullHPBar");
            clock = Content.Load<Texture2D>("PNGs/TickingClock");
            heal = Content.Load<SoundEffect>("Heal");
            intro = Content.Load<Song>("MP3s/IntroSong");
            gameplay = Content.Load<Song>("MP3s/BeepBox-Song");
            lootbox = Content.Load<Song>("MP3s/LootboxOpening");
            selectionScreen = Content.Load<Song>("MP3s/SelectionSong");
            transitionScreen = Content.Load<Song>("MP3s/RoundTransition");
            gameOver = Content.Load<Song>("MP3s/GameOver");
            spriteFont = Content.Load<SpriteFont>("Arial");
            gameOverHeader = Content.Load<SpriteFont>("GameOver");

            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(intro);
        }

        protected override void Update(GameTime gameTime)
        {
            int x = hero.XPos;
            int y = hero.YPos;

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
                        round = 1;

                        gameState = state.transition;
                        MediaPlayer.IsRepeating = false;
                        MediaPlayer.Play(transitionScreen);
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
                    // Checking for game pause
                    if ((gamePadState.Buttons.Start == ButtonState.Pressed && previousGamePadSate.Buttons.Start != ButtonState.Pressed) ||
                        (keyboardState.IsKeyDown(Keys.Enter)) && !previousKeyboardState.IsKeyDown(Keys.Enter))
                    {
                        gameState = state.pause;
                        break;
                    }

                    // Updates for the hero and enemy slimes and checking the consequences of their updates
                    for (int i = 0; i < enemySlimesCount; i++)
                    {
                        if (enemySlimes[i].Inactive && enemySlimes[i].Scale < .02f)
                        {
                            for (int j = i + 1; j < enemySlimesCount; j++)
                            {
                                //enemySlimes[j - 1] = enemySlimes[j];
                                hittableObjects.Remove(enemySlimes[j]);
                                _gridSpaces[enemySlimes[j-1].XPos, enemySlimes[j-1].YPos] = (int)CellType.Open;
                                enemySlimes[j - 1].ReplaceSlime(enemySlimes[j].XPos, enemySlimes[j].YPos, enemySlimes[j].Health, enemySlimes[j].MaxHealth, enemySlimes[j].Attack, enemySlimes[j].CooldownTimer);
                                _gridSpaces[enemySlimes[j-1].XPos, enemySlimes[j-1].YPos] = (int)CellType.EvilSlime;
                                hittableObjects.Add(enemySlimes[j - 1]);
                            }

                            enemySlimesCount--;
                        }
                        enemySlimes[i].Update(gameTime, ref _gridSpaces, x, y);
                    }

                    // Iterating through the hittable objects and checking if there are any updates to be made
                    if (hittableObjects.Count > 0)
                    {
                        int i = 0;

                        while(i < hittableObjects.Count)
                        {
                            var item = hittableObjects[i];
                            if (item.Inactive)
                            {
                                hittableObjects.Remove(item);
                                if (item.GetType() == typeof(EvilSlime))
                                {
                                    EvilSlime slime = (EvilSlime)item;
                                    int v = (slime.Attack + slime.MaxHealth) / 4;
                                    var drop = objectGeneration.RandomizeDrop(round, v);

                                    if (drop.item == CellType.Potion)
                                    {
                                        Potion p = new Potion(slime.XPos, slime.YPos, drop.value);
                                        p.SetTexture(potion);
                                        _gridSpaces[p.XPos, p.YPos] = (int)CellType.Potion;
                                        potions.Add(p);
                                    }
                                }
                            }
                            i++;
                        }
                    }

                    // Potion checks
                    if (potions.Count > 0)
                    {
                        int i = 0;

                        while(i < potions.Count)
                        {
                            if (hero.XPos == potions[i].XPos && hero.YPos == potions[i].YPos)
                            {
                                hero.Heal(potions[i].Collect());
                                heal.Play();
                            }
                            if (potions[i].Available)
                            {
                                _gridSpaces[potions[i].XPos, potions[i].YPos] = (int)CellType.Open;
                                potions.Remove(potions[i]);
                            }
                            i++;
                        }
                    }

                    // Checking if the crate hit flag has been triggered to end the round
                    if (_crateHit)
                    {
                        gameState = state.lootChest;
                        MediaPlayer.IsRepeating = false;
                        MediaPlayer.Play(lootbox);
                        break;
                    }

                    //enemySlime.Update(gameTime, ref _gridSpaces, x, y);
                    hero.Update(gameTime, ref _gridSpaces);

                    for (int i = 0; i < enemySlimesCount; i++)
                    {
                        if (enemySlimes[i].Attacking)
                        {
                            DamageCheck(enemySlimes[i].XPos, enemySlimes[i].YPos, enemySlimes[i].AttackDistance, enemySlimes[i].Attack, true, enemySlimes[i].AttackClass, enemySlimes[i].Direction);
                        }
                    }                    

                    if (hero.HasAttacked)
                    {
                          DamageCheck(hero.XPos, hero.YPos, hero.AttackDistance, hero.Attack, false, hero.AttackClass, hero.Direction);
                    }

                    // If enemy slimes are inactive try to add the main loot crate to the arena
                    if (enemySlimesCount == 0)
                    {
                        if (mainCrate.Inactive && !_crateHit)
                        {
                            if (!mainCrate.Spawn(13, 6, ref _gridSpaces))
                            {
                                mainCrate.TakeDamage(1);
                                gameState = state.lootChest;
                                MediaPlayer.IsRepeating = false;
                                MediaPlayer.Play(lootbox);
                            }
                            else
                            {
                                hittableObjects.Add(mainCrate);
                            }
                        }
                    }

                    // Checking for game over
                    if (hero.Death)
                    {
                        gameState = state.gameOver;
                        MediaPlayer.Play(gameOver);
                    }
                    break;

                case state.pause:
                    if ((gamePadState.Buttons.Start == ButtonState.Pressed && previousGamePadSate.Buttons.Start != ButtonState.Pressed) ||
                    (keyboardState.IsKeyDown(Keys.Enter)) && !previousKeyboardState.IsKeyDown(Keys.Enter))
                        gameState = state.gameLive;

                    if ((gamePadState.Buttons.Back == ButtonState.Pressed && previousGamePadSate.Buttons.Back != ButtonState.Pressed) ||
                        (keyboardState.IsKeyDown(Keys.Escape)) && !previousKeyboardState.IsKeyDown(Keys.Escape))
                    {
                        potions.Clear();
                        lootCrates.Clear();
                        hero.ResetValues();

                        (int, int) posValues = objectGeneration.FindEnemySpawnLocation(_gridSpaces);
                        _gridSpaces[posValues.Item1, posValues.Item2] = (int)CellType.EvilSlime;
                        enemySlimes[0].NewValues(posValues.Item1, posValues.Item2, 10, 1);
                        hittableObjects.Add(enemySlimes[0]);
                        enemySlimesCount = 1;
                        MediaPlayer.Play(intro);
                        gameState = state.titleScreen;
                    }
                    break;

                case state.lootChest:
                    camera.Update(gameTime);

                    if (MediaPlayer.State == MediaState.Stopped)
                    {
                        MediaPlayer.Play(selectionScreen);
                        MediaPlayer.IsRepeating = true;
                        string attribute;
                        int value;
                        (attribute, value) = objectGeneration.RandomizeAttributes();

                        hero.IncreaseStat(attribute, value, AttackType.StraightAhead);
                        lootScreenStatText = $"{attribute} was increased by {value}!";

                        _lootScreenTransition = true;
                        completeTextPos = -50f;

                        // Temp variables to get game running
                        hittableObjects.Remove(mainCrate);
                        _gridSpaces[13, 6] = (int)CellType.Open;
                        _gridSpaces[13, 7] = (int)CellType.Open;
                        _gridSpaces[14, 6] = (int)CellType.Open;
                        _gridSpaces[14, 7] = (int)CellType.Open;
                    }

                    if (_lootScreenTransition)
                    {
                        if ((gamePadState.Buttons.Start == ButtonState.Pressed && previousGamePadSate.Buttons.Start != ButtonState.Pressed) ||
                        (keyboardState.IsKeyDown(Keys.Enter)) && !previousKeyboardState.IsKeyDown(Keys.Enter) && MediaPlayer.IsRepeating == true)
                        {
                            potions.Clear();
                            lootCrates.Clear();
                            _crateHit = false;
                            round++;

                            enemySlimes = objectGeneration.SpawnEnemies(objectGeneration.CalculateDifficulty(round, hero), enemySlimes, ref enemySlimesCount, ref _gridSpaces);
                            for (int i = 0; i < enemySlimesCount; i++)
                            {
                                hittableObjects.Add(enemySlimes[i]);
                            }
                            _lootScreenTransition = false;
                            camera.SetPosition(cameraStartPosition);
                            gameState = state.transition;
                            MediaPlayer.IsRepeating = false;
                            MediaPlayer.Play(transitionScreen);
                        }

                        if ((gamePadState.Buttons.Back == ButtonState.Pressed && previousGamePadSate.Buttons.Back != ButtonState.Pressed) ||
                        (keyboardState.IsKeyDown(Keys.Escape)) && !previousKeyboardState.IsKeyDown(Keys.Escape))
                        {
                            potions.Clear();
                            lootCrates.Clear();
                            _crateHit = false;
                            hero.ResetValues();

                            (int, int) posValues = objectGeneration.FindEnemySpawnLocation(_gridSpaces);
                            _gridSpaces[posValues.Item1, posValues.Item2] = (int)CellType.EvilSlime;
                            enemySlimes[0].NewValues(posValues.Item1, posValues.Item2, 10, 1);
                            hittableObjects.Add(enemySlimes[0]);
                            enemySlimesCount = 1;
                            _lootScreenTransition = false;
                            camera.SetPosition(cameraStartPosition);
                            MediaPlayer.Play(intro);
                            gameState = state.titleScreen;
                        }
                    }
                    break;

                case state.transition:
                    // TODO: Still need to implement screen between stages and decisions for player to improve slime
                    if (MediaPlayer.State == MediaState.Stopped)
                    {
                        transitionTextPos = -300f;
                        MediaPlayer.Play(gameplay);
                        MediaPlayer.IsRepeating = true;
                        gameState = state.gameLive;
                    } else
                    {
                        if (transitionTextPos < 235f) transitionTextPos += 2f;
                    }
                    break;

                case state.gameOver:
                    if ((gamePadState.Buttons.Start == ButtonState.Pressed && previousGamePadSate.Buttons.Start != ButtonState.Pressed) ||
                        (keyboardState.IsKeyDown(Keys.Enter)) && !previousKeyboardState.IsKeyDown(Keys.Enter))
                    {
                        potions.Clear();
                        lootCrates.Clear();
                        hero.ResetValues();

                        (int, int) posValues = objectGeneration.FindEnemySpawnLocation(_gridSpaces);
                        _gridSpaces[posValues.Item1, posValues.Item2] = (int)CellType.EvilSlime;
                        enemySlimes[0].NewValues(posValues.Item1, posValues.Item2, 10, 1);
                        hittableObjects.Add(enemySlimes[0]);
                        enemySlimesCount = 1;
                        MediaPlayer.Play(intro);
                        gameState = state.titleScreen;
                    }
                    if ((gamePadState.Buttons.Back == ButtonState.Pressed && previousGamePadSate.Buttons.Back != ButtonState.Pressed) ||
                        (keyboardState.IsKeyDown(Keys.Escape)) && !previousKeyboardState.IsKeyDown(Keys.Escape))
                        Exit();
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
            _tilemap.Draw(gameTime, _spriteBatch);
            if (gameState > state.tutorial)
            {
                foreach(IndicationTile tile in indicationTiles)
                {
                    tile.Draw(gameTime, _spriteBatch);
                }

                foreach(Potion potion in potions)
                {
                    potion.Draw(gameTime, _spriteBatch);
                }

                if (enemySlimesCount != 0)
                {
                    for (int i = 0; i < enemySlimesCount; i++)
                    {
                        enemySlimes[i].Draw(gameTime, _spriteBatch);
                    }
                } else
                {
                    mainCrate.Draw(gameTime, _spriteBatch);
                }

                hero.Draw(gameTime, _spriteBatch);

                int avg = (int)(180 * (float)hero.Health / (float)hero.MaxHealth);

                _spriteBatch.Draw(baseHP, new Vector2(30, 30), new Rectangle(0, 0, 180, 20), Color.White);
                _spriteBatch.Draw(HP, new Vector2(30, 30), new Rectangle(0, 0, avg, 20), Color.White);

                if (hero.Waiting && !hero.Death)
                {
                    clockAnimationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

                    if (clockAnimationTimer > 0.1f)
                    {
                        if (clockAnimationFrame < 3) clockAnimationFrame++;
                        else clockAnimationFrame = 0;

                        clockAnimationTimer -= 0.1f;
                    }

                    _spriteBatch.Draw(clock, new Vector2(890, 547), new Rectangle(clockAnimationFrame * 64, 0, 64, 64), Color.White, 0, new Vector2(0, 0), .5f, SpriteEffects.None, 0);
                }
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
            if (gameState == state.transition)
            {
                _spriteBatch2.DrawString(gameOverHeader, $"Round {round}",
                        new Vector2((transitionTextPos), _graphics.GraphicsDevice.Viewport.Height * 0.5f - 30), Color.MintCream);
            }
            if (gameState == state.lootChest)
            {
                crate.Draw(camera);
                if (MediaPlayer.IsRepeating == false)
                {
                    if (completeTextPos <= _graphics.GraphicsDevice.Viewport.Height * 0.5f - 20) completeTextPos += 5;

                    _spriteBatch2.DrawString(gameOverHeader, "Round Complete",
                        new Vector2(_graphics.GraphicsDevice.Viewport.Width * 0.5f - 330,
                        (completeTextPos)), Color.ForestGreen);
                }
                else
                {
                    camera.MoveYPosition(15);

                    if (_lootScreenTransition)
                    {
                        _spriteBatch2.DrawString(spriteFont, $"HP: {hero.MaxHealth}\nAttack: {hero.Attack}\nSpeed: {hero.Speed}\nRange: {hero.AttackDistance}",
                        new Vector2(_graphics.GraphicsDevice.Viewport.Width * 0.14f,
                        (_graphics.GraphicsDevice.Viewport.Height * 0.3f) - 20), Color.LightGoldenrodYellow);

                        _spriteBatch2.DrawString(spriteFont, lootScreenStatText,
                        new Vector2(_graphics.GraphicsDevice.Viewport.Width * 0.52f - 304,
                        (_graphics.GraphicsDevice.Viewport.Height * 0.62f) - 20), Color.Firebrick);

                        _spriteBatch2.DrawString(spriteFont, "- Press Start or Enter to Continue Game -\n  - Press Back or Esc to Exit to the Menu -",
                        new Vector2(_graphics.GraphicsDevice.Viewport.Width * 0.5f - 304,
                        (_graphics.GraphicsDevice.Viewport.Height * 0.73f) - 20), Color.LightGoldenrodYellow);
                    }
                }
            }
            if (gameState == state.gameOver)
            {
                _spriteBatch2.DrawString(gameOverHeader, "GAME OVER",
                    new Vector2(_graphics.GraphicsDevice.Viewport.Width * 0.5f - 255,
                    (_graphics.GraphicsDevice.Viewport.Height * 0.35f) - 20), Color.DarkRed);

                _spriteBatch2.DrawString(spriteFont, "- Press Start or Enter to Return to Menu -\n    - Press Back or Esc to Exit the Game -",
                    new Vector2(_graphics.GraphicsDevice.Viewport.Width * 0.5f - 304,
                    (_graphics.GraphicsDevice.Viewport.Height * 0.60f) - 20), Color.LightGoldenrodYellow);
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
                                if (_gridSpaces[x, y] == (int)CellType.Slime)
                                {
                                    indicationTiles[x, y].Activate(CellType.HitIndicator);
                                    hero.TakeDamage(damage);
                                }
                                else if (x != xCord || y != yCord)
                                {
                                    indicationTiles[x, y].Activate(CellType.EnemyIndicator);
                                }
                            }
                            else if (_gridSpaces[x, y] >= (int)CellType.Crate)
                            {
                                indicationTiles[x, y].Activate(CellType.HitIndicator);
                                foreach (var victim in hittableObjects)
                                {
                                    if (victim.Equals(mainCrate))
                                    {
                                        if ((x == victim.XPos || x == victim.XPos + 1)
                                            && (y == victim.YPos || y == victim.YPos + 1))
                                        {
                                            victim.TakeDamage(damage);
                                            _crateHit = true;
                                            _gridSpaces[victim.XPos, victim.YPos] = (int)CellType.Open;
                                            _gridSpaces[victim.XPos, victim.YPos + 1] = (int)CellType.Open;
                                            _gridSpaces[victim.XPos + 1, victim.YPos] = (int)CellType.Open;
                                            _gridSpaces[victim.XPos + 1, victim.YPos + 1] = (int)CellType.Open;
                                        }
                                    }
                                    else if (victim.XPos == x && victim.YPos == y)
                                    {
                                        victim.TakeDamage(damage);
                                    }
                                }
                                //enemySlime.TakeDamage(damage);
                            }
                            else if (x != xCord || y != yCord)
                            {
                                indicationTiles[x, y].Activate(CellType.HeroIndicator);
                            }
                        }
                    }
                    break;

                case AttackType.Plus:
                    for (int x = xLowerBounds; x <= xUpperBounds; x++)
                    {
                        if (enemy)
                        {
                            if (_gridSpaces[x, yCord] == (int)CellType.Slime)
                            {
                                indicationTiles[x, yCord].Activate(CellType.HitIndicator);
                                hero.TakeDamage(damage);
                            }
                            else if (x != xCord) indicationTiles[x, yCord].Activate(CellType.EnemyIndicator);
                        }
                        else if (_gridSpaces[x, yCord] >= (int)CellType.Crate)
                        {
                            indicationTiles[x, yCord].Activate(CellType.HitIndicator);
                            foreach (var victim in hittableObjects)
                            {
                                if (victim.Equals(mainCrate))
                                {
                                    if ((x == victim.XPos || x == victim.XPos + 1)
                                        && (yCord == victim.YPos || yCord == victim.YPos + 1))
                                    {
                                        victim.TakeDamage(damage);
                                        _crateHit = true;
                                    }
                                }
                                else if (victim.XPos == x && victim.YPos == yCord)
                                {
                                    victim.TakeDamage(damage);
                                }
                            }
                            //enemySlime.TakeDamage(damage);
                        }
                        else if (x != xCord)
                        {
                            indicationTiles[x, yCord].Activate(CellType.HeroIndicator);
                        }
                    }
                    for (int y = yLowerBounds; y <= yUpperBounds; y++)
                    {
                        if (enemy)
                        {
                            if (_gridSpaces[xCord, y] == (int)CellType.Slime)
                            {
                                indicationTiles[xCord, y].Activate(CellType.HitIndicator);
                                hero.TakeDamage(damage);
                            }
                            else if (y != yCord) indicationTiles[xCord, y].Activate(CellType.EnemyIndicator);
                        }
                        else if (_gridSpaces[xCord, y] >= (int)CellType.Crate)
                        {
                            indicationTiles[xCord, y].Activate(CellType.HitIndicator);
                            foreach (var victim in hittableObjects)
                            {
                                if (victim.Equals(mainCrate))
                                {
                                    if ((xCord == victim.XPos || xCord == victim.XPos + 1)
                                        && (y == victim.YPos || y == victim.YPos + 1))
                                    {
                                        victim.TakeDamage(damage);
                                        _crateHit = true;
                                        _gridSpaces[victim.XPos, victim.YPos] = (int)CellType.Open;
                                        _gridSpaces[victim.XPos, victim.YPos + 1] = (int)CellType.Open;
                                        _gridSpaces[victim.XPos + 1, victim.YPos] = (int)CellType.Open;
                                        _gridSpaces[victim.XPos + 1, victim.YPos + 1] = (int)CellType.Open;
                                    }
                                }                                
                                else if (victim.XPos == xCord && victim.YPos == y)
                                {
                                    victim.TakeDamage(damage);
                                }
                            }
                            //enemySlime.TakeDamage(damage);
                        }
                        else if (y != yCord)
                        {
                            indicationTiles[xCord, y].Activate(CellType.HeroIndicator);
                        }
                    }
                    break;

                case AttackType.Ranged:
                    if (enemy)
                    {
                        switch (dir)
                        {
                            case 'N':
                                if (_gridSpaces[xCord, yCord + range] == (int)CellType.Slime)
                                {
                                    indicationTiles[xCord, yCord + range].Activate(CellType.HitIndicator);
                                    hero.TakeDamage(damage);
                                }
                                break;
                            case 'W':
                                if (_gridSpaces[xCord - range, yCord] == (int)CellType.Slime)
                                {
                                    indicationTiles[xCord - range, yCord].Activate(CellType.HitIndicator);
                                    hero.TakeDamage(damage);
                                }
                                break;
                            case 'S':
                                if (_gridSpaces[xCord, yCord - range] == (int)CellType.Slime)
                                {
                                    indicationTiles[xCord, yCord - range].Activate(CellType.HitIndicator);
                                    hero.TakeDamage(damage);
                                }
                                break;
                            default:
                                if (_gridSpaces[xCord + range, yCord] == (int)CellType.Slime)
                                {
                                    indicationTiles[xCord + range, yCord].Activate(CellType.HitIndicator);
                                    hero.TakeDamage(damage);
                                }
                                break;
                        }
                    }
                    /*
                     * This will be implemented as more of a powerup where the use selects a tile to shoot at and 
                     * the coordinates of the selected tile will be fed into this function to check occupancy of enemy
                     */
                    else if (_gridSpaces[xCord, yCord] >= (int)CellType.Crate)
                    {
                        indicationTiles[xCord, yCord].Activate(CellType.HitIndicator);
                        foreach (var victim in hittableObjects)
                        {
                            if (victim.Equals(mainCrate))
                            {
                                if ((xCord == victim.XPos || xCord == victim.XPos + 1)
                                    && (yCord == victim.YPos || yCord == victim.YPos + 1))
                                {
                                    victim.TakeDamage(damage);
                                    _crateHit = true;
                                    _gridSpaces[victim.XPos, victim.YPos] = (int)CellType.Open;
                                    _gridSpaces[victim.XPos, victim.YPos + 1] = (int)CellType.Open;
                                    _gridSpaces[victim.XPos + 1, victim.YPos] = (int)CellType.Open;
                                    _gridSpaces[victim.XPos + 1, victim.YPos + 1] = (int)CellType.Open;
                                }
                            }                            
                            else if (victim.XPos == xCord && victim.YPos == yCord)
                            {
                                victim.TakeDamage(damage);
                            }
                        }
                        //enemySlime.TakeDamage(damage);
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
                                    indicationTiles[x, y].Activate(CellType.HitIndicator);
                                    hero.TakeDamage(damage);
                                    DamageCheck(x, y, 1, damage / 2, true, AttackType.Shock);
                                }
                                else if (x != xCord || y != yCord)
                                {
                                    indicationTiles[x, y].Activate(CellType.EnemyIndicator);
                                }
                            }
                            /*
                            * TODO: Need to change once I add more than one enemy and the if(!enemySlime.Damaged) to account
                            * for more enemies that have already been damaged... maybe check inside conditional with for loop?
                            */
                            else if (_gridSpaces[x, y] >= (int)CellType.Crate && !enemySlime.Damaged)
                            {
                                indicationTiles[x, y].Activate(CellType.HitIndicator);
                                foreach (var victim in hittableObjects)
                                {
                                    if (victim.XPos == x && victim.YPos == y)
                                    {
                                        victim.TakeDamage(damage);
                                    }
                                }
                                //enemySlime.TakeDamage(damage);
                                DamageCheck(x, y, 1, damage / 2, false, AttackType.Shock);
                            }
                            else if (x != xCord || y != yCord)
                            {
                                indicationTiles[x, y].Activate(CellType.HeroIndicator);
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
                                if (_gridSpaces[x, y] == (int)CellType.Slime)
                                {
                                    indicationTiles[x, y].Activate(CellType.HitIndicator);
                                    hero.TakeDamage(damage);
                                }
                                else if (x != xCord || y != yCord)
                                {
                                    indicationTiles[x, y].Activate(CellType.EnemyIndicator);
                                }
                            }
                            else if (_gridSpaces[x, y] >= (int)CellType.Crate)
                            {
                                indicationTiles[x, y].Activate(CellType.HitIndicator);
                                foreach (var victim in hittableObjects)
                                {
                                    if (victim.Equals(mainCrate))
                                    {
                                        if ((x == victim.XPos || x == victim.XPos + 1)
                                            && (y == victim.YPos || y == victim.YPos + 1))
                                        {
                                            victim.TakeDamage(damage);
                                            _crateHit = true;
                                            _gridSpaces[victim.XPos, victim.YPos] = (int)CellType.Open;
                                            _gridSpaces[victim.XPos, victim.YPos + 1] = (int)CellType.Open;
                                            _gridSpaces[victim.XPos + 1, victim.YPos] = (int)CellType.Open;
                                            _gridSpaces[victim.XPos + 1, victim.YPos + 1] = (int)CellType.Open;
                                        }
                                    }
                                    else if(victim.XPos == x && victim.YPos == y)
                                    {
                                        victim.TakeDamage(damage);
                                    }
                                }
                                //enemySlime.TakeDamage(damage);
                            }
                            else if (x != xCord || y != yCord)
                            {
                                indicationTiles[x, y].Activate(CellType.HeroIndicator);
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
