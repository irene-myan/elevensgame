//Author: Irene Martirosyan
//FileName: ElevensGame.cs
//Project Name: Elevens
//Creation Date: 07/02/20
//Modified Date: 16/02/20
//Description: This program is the card game Elevens.

using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Animation2D;

namespace Elevens
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        // Graphical //

        // The width and height of the screen, the background image, and the background image box.
        int screenWidth;
        int screenHeight;
        Texture2D backgroundImage;
        Rectangle backgroundImageBox;

        // The cursor image, mouse position, and current and previous states for the mouse.
        Texture2D cursor;
        Vector2 positionOfMouse;
        MouseState currentMouseState;
        MouseState previousMouseState;

        // Background music and sound effects used.
        // SoundCount to ensure each sound is played only once (if in a loop).
        Song backgroundMusic;
        SoundEffect newCardSound;
        SoundEffect winSound;
        SoundEffect loseSound;
        int soundCount;

        // The title font and 3 seperate positions to add shading.
        SpriteFont titleFont;
        Vector2 titlePos;
        Vector2 titlePos2;
        Vector2 titlePos3;

        // The message, its font, and its position on the screen.
        SpriteFont messageFont;
        string message;
        Vector2 messageTxtPos;

        // The image of the back of the card, the number of cards left, and its position.
        Texture2D cardBack;
        Vector2 numCardsLeftTxtPos;

        // The image of the card faces, a white box as to highlight cards as they are chosen, and
        // a 2D array of bools refrencing the column and row, stating whether a card is highlighted.
        Texture2D cardFaces;
        Texture2D highlightBox;
        bool[,] highlight = new bool[6, 2];

        // The card spot size, column position and row position arrays are used to create the 2D 
        //array storing the rectangles for displaying the cards on the board.
        int[] cardSpotSize;
        int[] columnPosForCard;
        int[] rowPosForCard;
        Rectangle[,] cardSpotRect = new Rectangle[6, 2];


        // Game Setup //

        // A normal deck always has 52 cards.
        const int DECK_SIZE = 52;

        // A 2D array that will hold all the cards, initially being unshuffled, then later being a shuffled deck.
        // The first dimension holds the value of all the cards (from 1-13) and the second holding its corresponding suit.
        int[,] deckOfCards = new int[DECK_SIZE, 2];


        // A 3D array containing the card that is currently in the alloted spot. The first layer holds the value of 
        // the cards, the second layer holds the suit, both based on the column and row the card is in.
        int[,,] cardInPile = new int[6, 2, 2];

        // The variable used to iterate through the deck and the variable used to place the face cards at the bottom
        // of the deck.
        int currentSpotInDeck;
        int bottomOfDeckPos;

        // A 2d array holding the number of cards in each pile, by column and row.
        // The total number of cards left in the deck.
        int[,] numCardsInPile = new int[6, 2];
        int numCardsLeftInDeck;

        // Used to assign the dealt card to the card in the pile. The 1st index holds the value and the 2nd holds the suit.
        int[] card;

        // The first and second cards chosen. The 1st index holds the column and the 2nd holds the row.
        int[] chosenCard1;
        int[] chosenCard2;


        //Pre: The deck of cards is a shuffled 2D array holding the card value and suit. 
        //Post: Return the card drawn from the deck as an array holding the value and suit
        //Description: Deal a new card from the deck, increase the current spot in the deck
        // by one, and check if the current spot in the deck has reached its end(the deck size).
        // If so, reset it to iterate through from the begining of the deck where the face cards
        // that were removed now are.
        private int[] DealCard()
        {
            card = new int[] { deckOfCards[currentSpotInDeck, 0], deckOfCards[currentSpotInDeck, 1]};
            
            currentSpotInDeck++;

            // Resets the current spot in the deck to the begining of the deck.
            if (currentSpotInDeck == DECK_SIZE)
            {
                currentSpotInDeck = 0;
            }
            
            return card;
        }

        // Pre: The deck of cards is a 2D array holding the card value and suit.
        // Post: The deck of cards is shuffled.
        // Description: Shuffle the deck of cards by switching a card with a card in a random 
        // position above its own. 
        private void Shuffle()
        {
            // Generate a random number and create variables to hold the value and suit of the card being swapped.
            Random random = new Random();
            int value;
            int suit;

            // Iterates through the deck, swapping cards until it goes through the whole deck.
            for (int i = 0; i < DECK_SIZE; i++)                                                                                                                                    
            {
                // Hold the value and suit of the current card.
                value = deckOfCards[i, 0];
                suit = deckOfCards[i, 1];

                //Swap the value and suit of the current card with the value and suit a 
                // card in a random position after the current.
                int newSpot = random.Next(52 - 1);
                deckOfCards[i, 0] = deckOfCards[newSpot, 0];
                deckOfCards[i, 1] = deckOfCards[newSpot, 1];
                deckOfCards[newSpot, 0] = value;
                deckOfCards[newSpot, 1] = suit;
            }
        }

        // Pre: The position of the mouse on the screen as a Vector2.
        // Post: Returns the card chosen or -1 if no card was chosen.
        // Description: Based on which card spot the cursor was on, the chosen card is returned.
        public int[] CardChosen()
        {
            // Setting the card chosen as -1 (indicating no value) if no card is chosen.
            int[] cardChosen = new int[] { -1, -1 };

            for (int column = 0; column < cardInPile.GetLength(0); column++)
            {
                for (int row = 0; row < cardInPile.GetLength(1); row++)
                {
                    if (cardSpotRect[column, row].Contains(positionOfMouse))
                    {
                        cardChosen[0] = column;
                        cardChosen[1] = row;
                    }
                }
            }   
            return cardChosen;
        }

        // Pre: An array holding the column and row of the card chosen.
        // Post: Replaces the card chosen with a new one from the deck.
        // Description: Replaces the chosen card with a new one dealt from the deck.
        // Accordingly reduces the number of cards left in the deck and pile (depending on if 
        // it was a face card or not). Also, plays a sound effect because theres a new card. 
        private void ReplaceCard(int[] chosenCard)
        {
            // If the chosen card is not a face card, then we are not placing the card back in the deck.
            // The number of cards in that pile increases and the number of cards in the deck decreases.
            if (cardInPile[chosenCard[0], chosenCard[1], 0] < 11)
            {
                numCardsInPile[chosenCard[0], chosenCard[1]]++;
                numCardsLeftInDeck--;
            }
            
            // Picking a new card for the pile to replace it with
            card = DealCard();
            cardInPile[chosenCard[0], chosenCard[1], 0] = card[0];
            cardInPile[chosenCard[0], chosenCard[1], 1] = card[1];    
                
            // Playing the new card sound effect
            newCardSound.CreateInstance().Play();
        }

        // Pre: An array holding the column and row of the card chosen.
        // Post: The chosen card is highlighted.
        // Description: Highlights the card the user chose.
        private void HighlightCard(int[] chosenCard)
        {
            if (chosenCard[0] != -1)
            {
                highlight[chosenCard[0], chosenCard[1]] = true;
            }
        }

        // Pre: One of the cards was highlighted.
        // Post: All cards are un-highlighted.
        // Description: Ensures all the cards are not highlighted.
        private void UnHighlightCards()
        {
            for (int column = 0; column < columnPosForCard.Length; column++)
            {
                for (int row = 0; row < rowPosForCard.Length; row++)
                {
                    highlight[column, row] = false;
                }
            }
        }
        
        // Pre: The cards in the piles are stored in a 3D array. The first layer holds the value of 
        // the cards, the second layer holds the suit, both based on the column and row the card is in. 
        // Post: Returns false if even one of the cards in the piles is not a face card ( value of 10 or less).
        // Returns true if the player has won and all the cards on top are face cards.
        // Description: Checks to see whether the player has won.
        private bool Won()
        {
            //
            for (int column = 0; column < cardInPile.GetLength(0); column++)
            {
                for (int row = 0; row < cardInPile.GetLength(1); row++)
                {
                    if (cardInPile[column, row, 0] <= 10)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        //Pre: cardInPile holds the value and suit of the card based on its column and row.
        // numCardsInPile holds the number of cards that are in the pile.
        // Post: Returns true if any moves are possible. Returns false if no moves are possible.
        // Description: Checks all possibilities of loss conditions. No cards add to eleven and there
        // are no face cards that can be replaced.
        private bool MovePossible()
        {
            // Iterates through two sets of columns and rows, checking to see if any add to 11 or are
            // face cards that can be replaced.
            for (int column1 = 0; column1 < cardInPile.GetLength(0); column1++)
            {
                for (int row1 = 0; row1 < cardInPile.GetLength(1); row1++)
                {
                    for (int column2 = 0; column2 < cardInPile.GetLength(0); column2++)
                    {
                        for (int row2 = 0; row2 < cardInPile.GetLength(1); row2++)
                        {
                            if (cardInPile[column1, row1, 0] + cardInPile[column2, row2, 0] == 11)
                            {
                                return true;
                            }
                            else if (numCardsInPile[column1, row1] == 1 && cardInPile[column1,row1,0] > 10)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        // Pre: deckOfCards
        // Post: Creates and shuffles the deck of cards. Resets the current spot in deck and bottom of deck positions.
        // Deals the first hand of cards. Resets the chosen cards to have no value (-1). Sets the number of cards in a pile
        // to 1. Calculates the number of cards left in the deck after dealing the first hand. Plays the background music to start or
        // un-pause if previously paused. Sets the sound count to zero to allow sound effects to be played at the end.
        // Description: Sets up the game in the begining and at the end if they wish to restart.
        private void SetupGame()
        {
            // Assigning a card value and card suit to the cards in the deck
            for (int row = 0; row < DECK_SIZE; row++)
            {
                // Assigns a value from 1-13 to all 52 cards, Ace being 1 to Kind being 13. End up with 4 of each value in the deck
                deckOfCards[row, 0] = row % 13 + 1;
                
                // Assigns the suit to each card, 0-3, each representing a different suit
                deckOfCards[row, 1] = row % 4; 
            }

            // Shuffle the deck
            Shuffle();
            Shuffle();
            Shuffle();
            Shuffle();
            Shuffle();
            
            // Set the current spot in the deck and the position from the bottom of the deck to 0
            currentSpotInDeck = 0;
            bottomOfDeckPos = 0;

            // Deal the first hand of cards
            for (int column = 0; column < cardInPile.GetLength(0); column++)
            {
                for (int row = 0; row < cardInPile.GetLength(1); row++)
                {
                    card = DealCard();
                    cardInPile[column, row, 0] = card[0];
                    cardInPile[column, row, 1] = card[1];
                }
            }
            
            // Reset the chosen cards to have no value (-1)
            chosenCard1 = new int[] { -1, -1 };
            chosenCard2 = new int[] { -1, -1 };

            // Set the number of cards per pile to 1
            for (int row = 0; row < numCardsInPile.GetLength(0); row++)
            {
                for (int column = 0; column < numCardsInPile.GetLength(1); column++)
                {
                    numCardsInPile[row, column] = 1;
                }
            }

            // The number of cards left in the deck is the number of cards in the deck minus one card per pile (in the begining) 
            numCardsLeftInDeck = DECK_SIZE - numCardsInPile.GetLength(0) * numCardsInPile.GetLength(1);

            // Play the background music and reset the music counter to 0.
            MediaPlayer.Play(backgroundMusic);
            soundCount = 0;
        }

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Set window size.
            screenWidth = graphics.PreferredBackBufferWidth = 800;
            screenHeight = graphics.PreferredBackBufferHeight = 500;
            graphics.ApplyChanges();

            // Set the background image texture and image rectangle.
            backgroundImage = this.Content.Load<Texture2D>("Background/backgroundImage");
            backgroundImageBox = new Rectangle(0, 0, screenWidth, screenHeight);

            // Load the cursor that will be displayed.
            cursor = this.Content.Load<Texture2D>("Sprites/cursor");

            // Load the sounds that will be played during the game. 
            // Set the media player to loop once the background music is done.
            backgroundMusic = this.Content.Load<Song>("Sounds/BackgroundMusic");
            newCardSound = this.Content.Load<SoundEffect>("Sounds/NewCardSound");
            winSound = this.Content.Load<SoundEffect>("Sounds/WinSound");
            loseSound = this.Content.Load<SoundEffect>("Sounds/LoseSound");
            MediaPlayer.IsRepeating = true;

            // Load the title font and the position of the three titles (used to make one title).
            titleFont = this.Content.Load<SpriteFont>("Fonts/TitleFont");
            titlePos = new Vector2(250, 30);
            titlePos2 = new Vector2(248, 28);
            titlePos3 = new Vector2(246, 26);
            
            // Load the message font, initialize the message as nothing, and set the message position.
            messageFont = this.Content.Load<SpriteFont>("Fonts/MessageFont");
            message = "";
            messageTxtPos = new Vector2(120, 435);

            // Display the card back and number of cards left in the deck.
            cardBack = this.Content.Load<Texture2D>("Sprites/CardBack");
            numCardsLeftTxtPos = new Vector2(0, 27);

            // Load the card faces, initialize the size of the card spot, the positions of the rows and columns, and the box used to highlight cards.
            cardFaces = this.Content.Load<Texture2D>("Sprites/CardFaces");
            cardSpotSize = new int[] { 91, 128 };
            columnPosForCard = new int[] { 100, 200, 300, 400, 500, 600 };
            rowPosForCard = new int[] { 150, 290 };
            highlightBox = this.Content.Load<Texture2D>("Sprites/Square");

            // Create a rectangle for each spot and store it in a 2D array
            for (int column = 0; column < columnPosForCard.Length; column++)
            {
                for (int row = 0; row < rowPosForCard.Length; row++)
                {
                    cardSpotRect[column, row] = new Rectangle(columnPosForCard[column], rowPosForCard[row], cardSpotSize[0], cardSpotSize[1]);
                }
            }

            base.Initialize();

        }



        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {

            // TODO: use this.Content to load your game content here
            
            // Setup the game for the first time
            SetupGame();
        }



        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }
        


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        /// 
        protected override void Update(GameTime gameTime)
        {
            // Reset the game if the user clicks the escape button
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                SetupGame();
            }
                

            base.Update(gameTime);

            // TODO: Add your update logic here

            // Assign the position of the mouse, the position of the previous mouse state, and get the current mouse state
            positionOfMouse = new Vector2(currentMouseState.X, currentMouseState.Y);
            previousMouseState = currentMouseState;
            currentMouseState = Mouse.GetState();


            // Check whether there is a new click and if the first card chosen has no value. If so, identify which card was chosen. 
            // If the first card does have a value, check if the card chosen is a face card or if the user is choosing the second card.
            if (currentMouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton != ButtonState.Pressed && chosenCard1[0] == -1)
            {
                chosenCard1 = CardChosen();

                // Highlight the chosen card and reset the message
                HighlightCard(chosenCard1);
                message = "";
            }
            else if (chosenCard1[0] != -1)
            {
                // Face card chosen:
                if (cardInPile[chosenCard1[0], chosenCard1[1], 0] > 10)
                {
                    // Check if there is only one card in the chosen pile. If so, place the card at the bottom of the deck then replace it with a new card.
                    // If not, display a message to the player that says it is an invalid move.
                    if (numCardsInPile[chosenCard1[0], chosenCard1[1]] == 1)
                    {
                        // Put the face card at the bottom of the deck (begining of the array)
                        deckOfCards[bottomOfDeckPos, 0] = cardInPile[chosenCard1[0], chosenCard1[1], 0];
                        deckOfCards[bottomOfDeckPos, 1] = cardInPile[chosenCard1[0], chosenCard1[1], 1];
                        bottomOfDeckPos++;
                        
                        ReplaceCard(chosenCard1);
                    }
                    else
                    {
                        message = "There are too many cards in that pile.";
                    }

                   // Remove the value of the first chosen card and unhighlight the card
                    chosenCard1[0] = -1;
                    UnHighlightCards();
                }
                // New card chosen:
                else
                {
                    if (currentMouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton != ButtonState.Pressed && chosenCard2[0] == -1)
                    {
                        chosenCard2 = CardChosen();

                        // Check whether the two cards have a value and add to 11. If so, replace both. If not, display an error message.
                        if ((chosenCard1[0] != -1) && (chosenCard2[0] != -1)) 
                        {
                            if (cardInPile[chosenCard1[0], chosenCard1[1], 0] + cardInPile[chosenCard2[0], chosenCard2[1], 0] == 11)
                            {
                                ReplaceCard(chosenCard1);
                                ReplaceCard(chosenCard2);
                            }
                            else
                            {
                                message = "Those don't add to 11, try again.";
                            }
                            
                           // Remove the value of the first and second chosen cards, then unhighlight the cards
                            chosenCard1[0] = -1;
                            chosenCard2[0] = -1;
                            UnHighlightCards();
                        }
                    }
                }
            }
            
            // Check whether the player has won or lost (if no more moves are possible).
            // Display a corresponding message for each and play a sound after pausing the background music.
            // SoundCount ensures the sound is only played once.
            if (Won() && soundCount == 0)
            {
                message = "You won! Want to play again? Press ESC";

                MediaPlayer.Pause();
                winSound.CreateInstance().Play();
                soundCount++;
            }
            else if (MovePossible() == false && soundCount == 0)
            {
                message = "No more moves. :( Want to try again? Press ESC";

                MediaPlayer.Pause();
                loseSound.CreateInstance().Play();
                soundCount++;
            }
        }
        


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.LightPink);

            // TODO: Add your drawing code here

            spriteBatch.Begin();

            // Setting the background image
            spriteBatch.Draw(backgroundImage, backgroundImageBox, Color.White);

            // Displaying the layered title
            spriteBatch.DrawString(titleFont, "Elevens!", position: titlePos3, color: Color.Green);
            spriteBatch.DrawString(titleFont, "Elevens!", position: titlePos2, color: Color.Red);
            spriteBatch.DrawString(titleFont, "Elevens!", position: titlePos, color: Color.AntiqueWhite);


            // Drawing the back of the card (the deck) and displaying the amount of cards left in the deck
            spriteBatch.Draw(cardBack, Vector2.Zero);
            spriteBatch.DrawString(titleFont, numCardsLeftInDeck.ToString(), position: numCardsLeftTxtPos, color: Color.AntiqueWhite);

            // Drawing the card that is in each spot
            // Drawing the highlight box if the certain box is highlighted
            for (int column = 0; column < columnPosForCard.Length; column++)
            {
                for (int row = 0; row < rowPosForCard.Length; row++)
                {
                    spriteBatch.Draw(cardFaces, cardSpotRect[column, row], new Rectangle(cardSpotSize[0] * (cardInPile[column, row, 0] - 1), cardSpotSize[1] * cardInPile[column, row, 1], cardSpotSize[0], cardSpotSize[1]), Color.White);

                    if (highlight[column, row] == true)
                    {
                        spriteBatch.Draw( highlightBox, cardSpotRect[column, row], Color.Green * 0.6f);
                    }
                }
            }

            // Message displayed indicating a win, lose, error, etc.
            spriteBatch.DrawString(messageFont, message, position: messageTxtPos, color: Color.AntiqueWhite);

            // Drawing the mouse
            spriteBatch.Draw(cursor, positionOfMouse);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
