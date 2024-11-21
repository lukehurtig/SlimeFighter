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
        private float _tileSize = 32f;

        /// <summary>
        /// Need to make classes for these objects just had to patch over to finish
        /// </summary>
        private bool _crateHit = false;
        private bool _potionCollected = false;
        private Texture2D crate2D;
        private Texture2D potion;
        private Vector2 cratePos = new Vector2((float)(13 * 32f) + 30, (float)(6 * 32f) + 125);
        private Vector2 cratePos2 = new Vector2((float)(14 * 32f) + 30, (float)(7 * 32f) + 125);

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
                    int damageToHero = 0;
                    int damageToEnemy = 0;

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

                    if (hero.HasMoved) enemySlime.Update(gameTime, ref _gridSpaces, hero.Position, damageToEnemy);
                    hero.Update(gameTime, ref _gridSpaces, damageToHero);

                    if (enemySlime.Death)
                    {
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
    }
}
