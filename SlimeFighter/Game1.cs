using Microsoft.Xna.Framework;
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
        private IndicationTile[,] indicationTiles = new IndicationTile[28, 13];
        private static float _tileSize = 32f;

        /// <summary>
        /// Need to make classes for these objects just had to patch over to finish
        /// </summary>
        private bool _crateHit = false;
        private bool _potionCollected = false;
        private bool _lootScreenTransition = false;
        private string lootScreenStatText = string.Empty;
        private Texture2D potion;
        private Vector3 cameraStartPosition = new Vector3(0, 3, 5);
        private float completeTextPos = -50f;

        /// <summary>
        /// All the necessary in-game objects probably could export some of these outside the Game class
        /// </summary>
        private Slime hero;
        private EvilSlime enemySlime;
        private Crate crate;
        private CirclingCamera camera;
        private LootCrate mainCrate;
        private Texture2D title;
        private Texture2D baseHP;
        private Texture2D HP;
        private Texture2D clock;
        private Song intro;
        private Song gameplay;
        private Song lootbox;
        private Song gameOver;
        private SpriteFont spriteFont;
        private SpriteFont gameOverHeader;

        /// <summary>
        /// Collections of objects used in the game
        /// </summary>
        private List<LootCrate> lootCrates = new List<LootCrate>();
        private List<Potion> potions = new List<Potion>();
        private int numberOfEnemySlimes = 0;
        private EvilSlime[] enemySlimes = new EvilSlime[20];
        private List<HittableObject> hittableObjects = new List<HittableObject>();

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

            for (int i = 0; i < 28; i++)
            {
                for (int j = 0; j < 13; j++)
                {
                    indicationTiles[i, j] = new IndicationTile();
                    indicationTiles[i, j].Spawn(i, j);
                }
            }

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
            hittableObjects.Add(enemySlime);
            mainCrate = new LootCrate(false);
            lootCrates.Add(mainCrate);

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
            mainCrate.LoadContent(Content);

            crate = new Crate(this, CrateType.Slats, Matrix.Identity);
            camera = new CirclingCamera(this, new Vector3(0, 3, 5), 2f);

            foreach (var tile in indicationTiles)
            {
                tile.LoadContent(Content);
            }

            potion = Content.Load<Texture2D>("PNGs/Potion");
            title = Content.Load<Texture2D>("PNGs/SlimeLogo");
            baseHP = Content.Load<Texture2D>("EmptyHPBar");
            HP = Content.Load<Texture2D>("FullHPBar");
            clock = Content.Load<Texture2D>("PNGs/TickingClock");
            intro = Content.Load<Song>("MP3s/IntroSong");
            gameplay = Content.Load<Song>("MP3s/BeepBox-Song");
            lootbox = Content.Load<Song>("MP3s/LootboxOpening");
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
                    if ((gamePadState.Buttons.Start == ButtonState.Pressed && previousGamePadSate.Buttons.Start != ButtonState.Pressed) ||
                        (keyboardState.IsKeyDown(Keys.Enter)) && !previousKeyboardState.IsKeyDown(Keys.Enter))
                    {
                        gameState = state.pause;
                        break;
                    }

                    if (hittableObjects.Count > 0)
                    {
                        int i = 0;

                        while(i < hittableObjects.Count)
                        {
                            var item = hittableObjects[i];
                            if (item.Inactive) hittableObjects.Remove(item);
                            i++;
                        }
                    }

                    if (enemySlime.Inactive && !_potionCollected && Vector2.Distance(hero.Position, enemySlime.Position) < 10f)
                    {
                        hero.Heal(5);
                        _potionCollected = true;
                    }
                    
                    if (_crateHit)
                    {
                        gameState = state.lootChest;
                        MediaPlayer.IsRepeating = false;
                        MediaPlayer.Play(lootbox);
                        break;
                    }

                    enemySlime.Update(gameTime, ref _gridSpaces, x, y);
                    hero.Update(gameTime, ref _gridSpaces);

                    if (enemySlime.Attacking)
                    {
                        DamageCheck(enemySlime.XPos, enemySlime.YPos, enemySlime.AttackDistance, enemySlime.Attack, true, enemySlime.AttackClass, enemySlime.Direction);
                    }

                    if (hero.HasAttacked)
                    {
                        DamageCheck(hero.XPos, hero.YPos, hero.AttackDistance, hero.Attack, false, hero.AttackClass, hero.Direction);
                    }

                    if (enemySlime.Inactive)
                    {
                        if (mainCrate.Inactive && !_crateHit)
                        {
                            _gridSpaces[enemySlime.XPos, enemySlime.YPos] = (int)CellType.Potion;

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
                        gameState = state.titleScreen;
                        hero.ResetValues();
                        enemySlime.NewValues(_random.Next(27), _random.Next(12), _random.Next(20) + 1, _random.Next(3) + 1);
                        MediaPlayer.Play(intro);
                    }
                    break;

                case state.lootChest:
                    camera.Update(gameTime);

                    if (MediaPlayer.State == MediaState.Stopped)
                    {
                        MediaPlayer.IsRepeating = true;
                        MediaPlayer.Play(gameplay);
                        string attribute;
                        int value;
                        (attribute, value) = RandomizeAttributes();

                        hero.IncreaseStat(attribute, value, AttackType.StraightAhead);
                        lootScreenStatText = $"{attribute} was increased by {value}!";

                        _lootScreenTransition = true;

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
                            enemySlime.NewValues(_random.Next(27), _random.Next(12), _random.Next(20) + 1, _random.Next(3) + 1);
                            hittableObjects.Add(enemySlime);
                            _crateHit = false;
                            _potionCollected = false;
                            _lootScreenTransition = false;
                            camera.SetPosition(cameraStartPosition);
                            gameState = state.gameLive;
                        }

                        if ((gamePadState.Buttons.Back == ButtonState.Pressed && previousGamePadSate.Buttons.Back != ButtonState.Pressed) ||
                        (keyboardState.IsKeyDown(Keys.Escape)) && !previousKeyboardState.IsKeyDown(Keys.Escape))
                        {
                            gameState = state.titleScreen;
                            _crateHit = false;
                            _potionCollected = false;
                            _lootScreenTransition = false;
                            hero.ResetValues();
                            enemySlime.NewValues(_random.Next(27), _random.Next(12), _random.Next(20) + 1, _random.Next(3) + 1);
                            hittableObjects.Add(enemySlime);
                            camera.SetPosition(cameraStartPosition);
                            MediaPlayer.Play(intro);
                        }
                    }
                    break;

                case state.transition:
                    // TODO: Still need to implement screen between stages and decisions for player to improve slime
                    break;

                case state.gameOver:
                    if ((gamePadState.Buttons.Start == ButtonState.Pressed && previousGamePadSate.Buttons.Start != ButtonState.Pressed) ||
                        (keyboardState.IsKeyDown(Keys.Enter)) && !previousKeyboardState.IsKeyDown(Keys.Enter))
                    {
                        gameState = state.titleScreen;
                        MediaPlayer.Play(intro);
                        hero.ResetValues();
                        enemySlime.NewValues(_random.Next(27), _random.Next(12), _random.Next(20) + 1, _random.Next(3) + 1);
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
            // TODO: Add your drawing code here            
            _tilemap.Draw(gameTime, _spriteBatch);
            if (gameState > state.tutorial)
            {
                foreach(IndicationTile tile in indicationTiles)
                {
                    tile.Draw(gameTime, _spriteBatch);
                }

                if (enemySlime.Inactive)
                {
                    if (!_potionCollected) _spriteBatch.Draw(potion, enemySlime.Position, Color.White);
                    //if (!_crateHit) _spriteBatch.Draw(crate2D, cratePos, Color.White);
                    if (!mainCrate.Inactive) mainCrate.Draw(gameTime, _spriteBatch);
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
                //_spriteBatch2.DrawString(spriteFont, "- This game is still a Work in Progress\n I intend to make it so each crate destroyed\n upgrades the player through stats,\n abilities or attack types.\n- Also more enemies to come with\n varied spawns and ammounts\n- Also AI could use a little work, but solid start\n\n   - Press Enter or Start to Return to Menu -\n   - Or Press ESC or Back to Exit the Game -",
                //   new Vector2(75, 60), Color.LightGoldenrodYellow);
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
                        _spriteBatch2.DrawString(spriteFont, $"HP: {hero.MaxHealth}\nAttack: {hero.Attack}\nSpeed: {hero.Speed}\nAttack Distance: {hero.AttackDistance}",
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
                            /*
                            * TODO: Need to change once I add more than one enemy
                            */
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
                        /*
                        * TODO: Need to change once I add more than one enemy
                        */
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
                        /*
                        * TODO: Need to change once I add more than one enemy
                        */
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
                            /*
                            * TODO: Need to change once I add more than one enemy
                            */
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

        /// <summary>
        /// This function is purely temporary to fulfill gameplay loop by giving random stat increase, I will replace with a better loot table styled version in the future
        /// </summary>
        // Function to randomize and return a dictionary of attributes and their values
        public (string attribute, int amount) RandomizeAttributes()
        {
            // Attributes to randomize
            string[] attributes = { "Attack", "HP", "Speed", "Range"};

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
    }
}
