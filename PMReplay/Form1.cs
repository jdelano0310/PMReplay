using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PMReplay
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        private class Hand
        {
            public int HandNumber { set; get; }
            public String HandTime { set; get; }
            public List<String> HandLines { set; get; }
        }

        private class Card
        {
            public String Value { set; get; }
            public String CardImg { set; get; }
        }

        // an array of card values and what image represents the card
        // built by BuildDeck, CardValue, CardImageName
        Card[] deck;
        String backOfCardImageFile = "";

        Label FindSpecificLabel(String controlName)
        {
            var res = from lbl in grpHandDisplay.Controls.OfType<Label>()
                      where lbl.Name == controlName
                      select lbl;
            Label firstLbl = res.First();

            return firstLbl;
        }

        private List<Hand> hands;
        String[] PlayersSeatNumber;
        double[] PlayersPrevBet = {0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0};

        private void ReadHandHistoryFile(String filename)
        {
            string[] lines = System.IO.File.ReadAllLines(@filename);
            List<string> handLines = new List<string>();

            int currentHandNumber = 0;
            String handTime = "";
            Hand currentHand = null;

            hands = new List<Hand>();

            // Display the file contents by using a foreach loop.
            cboHands.Items.Clear();
            foreach (string line in lines)
            {
                if (line.Contains("Hand #"))
                {
                    // only add the hand number line from the text file to the listbox
                    cboHands.Items.Add(line.Substring(line.IndexOf('-') + 1));

                    if (currentHandNumber > 0)
                    {
                        // add the hand's information to the hands list to be used by the listbox
                        currentHand = new Hand
                        {
                            HandNumber = currentHandNumber,
                            HandTime = handTime,
                            HandLines = new List<String>()
                        };
                        currentHand.HandLines.AddRange(handLines);

                        hands.Add(currentHand);
                        handLines.Clear();
                    }

                    currentHandNumber = int.Parse(line.Substring(6).Split('-')[1]);
                    handTime = $"{line.Substring(6).Split(' ')[2]} {line.Substring(6).Split(' ')[3]}";
                }
                else
                {
                    // save each line of the hand history for use if the user selects it from the list
                    handLines.Add(line);
                }
            }

            // add the very last hand in the file to the list 
            currentHand = new Hand
            {
                HandNumber = currentHandNumber,
                HandTime = handTime,
                HandLines = new List<String>()
            };
            currentHand.HandLines.AddRange(handLines);

            hands.Add(currentHand);
            handLines.Clear();

            hands.Add(currentHand);

            label1.Text = $"{cboHands.Items.Count} hands";
        }

        private string FindStringInBetween(String Source, char char1, char char2)
        {
            String temp = Source.Substring(Source.IndexOf(char1) + 1,
                         (Source.IndexOf(char2) - Source.IndexOf(char1)) - 1);

            return temp;
        }

        private void SetPlayerInfo(String SeatNumber, String PlayerName, String PlayerStack, bool SittingOut)
        {
            // find the label that coinsides with the seat poistion of the player
            Label lblPlayerName = FindSpecificLabel($"lblSeatPlayer{SeatNumber}");

            Label lblStackSize = FindSpecificLabel($"lblStackSize{SeatNumber}");

            lblPlayerName.Text = SittingOut ? $"{PlayerName} SO" : PlayerName;
            lblStackSize.Text = PlayerStack;

            lblPlayerName.Visible = true;
            lblStackSize.Visible = true;

            if (!SittingOut)
            {
                DisplayCardImage($"P{SeatNumber}C1", "back");
                DisplayCardImage($"P{SeatNumber}C2", "back");
            }
        }

        private void SetPoistionInfo(String SeatNumber, String Postition)
        {
            // position being Small Blind, Big Blind, Straddle
            // Small and Big - after sitting out and missing a round of hands
            // Big after missing the big blind sitting out
            Label lblSeatPosition = FindSpecificLabel($"lblSeatPosition{SeatNumber}");

            if (lblSeatPosition.Text.Length > 0)
            {
                lblSeatPosition.Text = $"{lblSeatPosition.Text} / {Postition}";
            }
            else
            {
                lblSeatPosition.Text = Postition;
                lblSeatPosition.Visible = true;
            }
        }

        Label lblLastHighlightedPlayer;
        private void HighlightActionOnSeat(String SeatNumber)
        {
            if (lblLastHighlightedPlayer != null)
            {
                lblLastHighlightedPlayer.BackColor = Color.Khaki;
            }

            lblLastHighlightedPlayer = FindSpecificLabel($"lblSeatPlayer{SeatNumber}");
            lblLastHighlightedPlayer.BackColor = Color.Yellow;
        }

        private void UpdateStackSizeForSeat(String SeatNumber, String amount, String math, String HandActionline = "")
        {
            Label lblStackSize = FindSpecificLabel($"lblStackSize{SeatNumber}");
            double stack = 0.0;
            double amountToPot = double.Parse(amount);

            if (HandActionline.Contains("raises")) {
                // if they raise you must take out the previous bet/posted amount of the
                // total raise value 
                amountToPot -= PlayersPrevBet[int.Parse(SeatNumber)];
            }
            
            // there is a negative number - the hand history doesn't calc correctly
            // it doesn't remove the blinds from the players stack on an all-in
            if (!lblStackSize.Text.Contains("("))
            {
                stack = double.Parse(lblStackSize.Text.Substring(1));
            }

            if (math == "subtract")
            {
                stack -= amountToPot;
                lblThePot.Text = lblThePot.Text.Length == 0 ? amountToPot.ToString("C2")
                    : (double.Parse(lblThePot.Text.Substring(1)) + amountToPot).ToString("C2");
            }
            else
            {
                stack += double.Parse(amount);
            }

            lblStackSize.Text = stack.ToString("C2");

            // save the players last bet in case there is a raise, the code must then 
            // only deduct the difference bwteen the prev bet and the current raise
            PlayersPrevBet[int.Parse(SeatNumber)] = double.Parse(amount);
            Application.DoEvents();
        }

        private void AddLineToHandActionListbox(string lineToAdd)
        {
            // add the line to the listbox and then highlite it so the line is in view
            lbHandAction.Items.Add(lineToAdd);
            lbHandAction.SelectedIndex = lbHandAction.Items.Count - 1;

        }

        private int FindCardIndex(String cardValue)
        {
            int index = 0;
            foreach (Card c in deck)
            {
                if (c.Value == cardValue)
                {
                    break;
                }

                index += 1;
            }
            return index;
        }

        private void DisplayCardImage(String cardPosition, String cardValue)
        {
            Card card = cardValue.Length == 2 ? deck[FindCardIndex(cardValue)] : null;
            Image image = cardValue.Length == 2 ? Image.FromFile(card.CardImg) : null;

            switch (cardPosition)
            {
                case "F1":
                    pbFlopCard1.Image = image;
                    pbFlopCard1.Visible = true;
                    break;
                case "F2":
                    pbFlopCard2.Image = image;
                    pbFlopCard2.Visible = true;
                    break;
                case "F3":
                    pbFlopCard3.Image = image;
                    pbFlopCard3.Visible = true;
                    break;
                case "T":
                    pbTurnCard.Image = image;
                    pbTurnCard.Visible = true;
                    break;
                case "R":
                    pbRiverCard.Image = image;
                    pbRiverCard.Visible = true;
                    break;
                default:
                    // player cards

                    // the hole cards will not be sent with a cardvalue
                    // it'll get it from the tag property of the control
                    string seatNumber = cardPosition.Substring(1, 1);
                    string cardNumber = cardPosition.Substring(3, 1);
                    string pbName = $"pbPlayer{seatNumber}Card{cardNumber}";

                    var res = from pb in grpHandDisplay.Controls.OfType<PictureBox>()
                              where pb.Name == pbName
                              select pb;
                    PictureBox pictureBox = res.First();

                    try
                    {
                        if (cardValue != "back")
                        {
                            card = deck[FindCardIndex(pictureBox.Tag.ToString())];
                            image = Image.FromFile(card.CardImg);
                        } else
                        {
                            image = Image.FromFile(backOfCardImageFile);
                        }

                        pictureBox.Image = image;
                        pictureBox.Visible = true;
                    }
                    catch { }

                    break;
            }
        }

        private void ShowHand(int HandNumber)
        {
            String seatNumber = "";
            String siteName = "";
            String tableName = "";
            String theFlop = "";
            String moneyLine = "";

            bool processedSeats = false;
            bool processedPreDeal = false;
            bool potShow = false;

            bool handIsfinished = false;
            bool holeCardsLineReached = false;
            bool summaryLineReached = false;

            PlayersSeatNumber = new string[9]; // small array for quick seat number reference

            // during an round of betting keep track of the previous bet in case the player raises a raise 
            // only deduct the difference from their last bet to the current raise amount
            PlayersPrevBet = new double[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };

            Hand hand = hands[HandNumber];

            foreach (String line in hand.HandLines)
            {

                if (line.Contains("Summary"))
                {
                    summaryLineReached = true;
                }

                if (holeCardsLineReached &&
                    !NextButtonClicked && !summaryLineReached &&
                    !line.Contains("refund") && !line.Contains("Pot Show Down"))
                {
                    while (!NextButtonClicked && !UserClickedClose)
                    {
                        Application.DoEvents();
                    };

                    NextButtonClicked = false;

                }

                if (UserClickedClose)
                {
                    // the user has clicked the form's red x - jump out of the display hand loop
                    break;
                }

                if (line.Contains("Game"))
                {
                    lblGameType.Text = $"{line.Substring(5)} Hand #: {HandNumber + 1} Time: {hand.HandTime}";
                }
                else if (siteName.Length == 0 && line.Contains("Site"))
                {
                    siteName = line.Substring(5);
                }
                else if (tableName.Length == 0 && line.Contains("Table"))
                {
                    tableName = line.Substring(7);
                }
                else 

                if (line.Contains("Seat") && !processedSeats)
                {
                    // add a seat information to the hand
                    seatNumber = line.Substring(5, 1);
                    string _PlayerName = line.Substring(8, (line.Substring(8).IndexOf("(") - 1));
                    string _StackSize = $"${FindStringInBetween(line, '(', ')')}";
                    bool _PlayerSittingOut = line.Contains("sitting out");

                    PlayersSeatNumber[int.Parse(seatNumber)] = _PlayerName;

                    SetPlayerInfo(seatNumber, _PlayerName, _StackSize, _PlayerSittingOut);

                }
                else
                {
                    // done looping through seat data
                    if (!processedSeats && seatNumber.Length > 0)
                    {
                        processedSeats = true;
                    }
                }

                if (processedSeats && !processedPreDeal)
                {
                    // preDeal - player position (who is the Button, SB, and BB)
                    String preDealPlayer = line.Substring(0, line.IndexOf(' '));
                    String preDealPlayerPosition = line.Contains("dealer") ? "D" : line.Contains("small blind")
                                                        ? "SB" : line.Contains("big blind") && !line.Contains("small")
                                                        ? "BB" : line.Contains("small & big blind") ? "SB+BB" :
                                                        line.Contains("straddle") ? "STRDL" : "NF";

                    seatNumber = Array.IndexOf(PlayersSeatNumber, preDealPlayer).ToString();

                    if (seatNumber != "-1")
                    {
                        SetPoistionInfo(seatNumber, preDealPlayerPosition);
                    }
                }

                if (line.Contains("wins") || line.Contains("splits"))
                {
                    // encountered the line that indicates a player has won the pot
                    handIsfinished = true;

                    string _PlayerName = line.Substring(0, (line.IndexOf(' ')));
                    seatNumber = Array.IndexOf(PlayersSeatNumber, _PlayerName).ToString();
                    string[] fromPot = line.Split(' ');
                    string amountToStack;

                    HighlightActionOnSeat(seatNumber);

                    if (!potShow)
                    {
                        // wins with no show down
                        amountToStack = fromPot[fromPot.Length - 1];
                        amountToStack = amountToStack.Substring(1, amountToStack.Length - 2);
                        moneyLine = CreateMoneyLine(fromPot, fromPot.Length - 1, amountToStack);
                    }
                    else
                    {
                        // wins with show down
                        amountToStack = fromPot[3];
                        amountToStack = amountToStack.Substring(1, amountToStack.Length - 2);
                        moneyLine = CreateMoneyLine(fromPot,3, amountToStack);
                    }

                    UpdateStackSizeForSeat(seatNumber, amountToStack, "add");
                    AddLineToHandActionListbox(moneyLine);
                }

                if (line.Contains("Hole Cards"))
                {
                    lbHandAction.Visible = true;
                    btnNext.Visible = true;

                    processedPreDeal = true;
                    holeCardsLineReached = true;
                    AddLineToHandActionListbox(FindStringInBetween(line, '[', ']'));
                }
                else
                {

                    if (!handIsfinished && holeCardsLineReached)
                    {

                        if (line.Contains("Flop"))
                        {
                            AddLineToHandActionListbox(line);
                            //processedPreFlop = true;
                            theFlop = FindStringInBetween(line, '[', ']');
                            var flop = theFlop.Split(' ');
                            DisplayCardImage("F1", flop[0]);
                            DisplayCardImage("F2", flop[1]);
                            DisplayCardImage("F3", flop[2]);

                            // reset preflop bets
                            PlayersPrevBet = new double[] { 0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0};
                        }
                        else if (line.Contains("Turn"))
                        {
                            AddLineToHandActionListbox(line);
                            DisplayCardImage("T", FindStringInBetween(line, '[', ']'));

                            // reset preflop bets
                            PlayersPrevBet = new double[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };

                        }
                        else if (line.Contains("River"))
                        {
                            AddLineToHandActionListbox(line);
                            DisplayCardImage("R", FindStringInBetween(line, '[', ']'));
                        }
                        else if (line.Contains("Pot Show Down"))
                        {
                            potShow = true;
                        }
                        else if (line.Contains("refund"))
                        {
                            // take the bet back because the other player(s) folded to it
                            string[] actionLine = line.Split(' ');
                            string amountFromPot;
                            if (line.Contains("(All-in"))
                            {
                                amountFromPot = actionLine[actionLine.Length - 2];
                            }
                            else
                            {
                                amountFromPot = actionLine[actionLine.Length - 1];
                            }

                            // remove the bet from the pot
                            lblThePot.Text = (double.Parse(lblThePot.Text.Substring(1)) -
                                                double.Parse(amountFromPot)).ToString("C2");

                            // add it bet back to the player's stack
                            string _PlayerName = line.Substring(0, (line.IndexOf(' ')));
                            seatNumber = Array.IndexOf(PlayersSeatNumber, _PlayerName).ToString();

                            UpdateStackSizeForSeat(seatNumber, amountFromPot, "add");
                            AddLineToHandActionListbox($"Bet not called returned to {_PlayerName}");

                        }
                        else if (line.Contains("folds"))
                        {
                            AddLineToHandActionListbox(line);

                            string _PlayerName = line.Substring(0, (line.IndexOf(' ')));
                            seatNumber = Array.IndexOf(PlayersSeatNumber, _PlayerName).ToString();
                            HighlightActionOnSeat(seatNumber);
                            PlayerFoldsHideCardImages(seatNumber);

                            if (line.Contains("shows"))
                            {
                                // the player folded and then showed their cards
                                string _HoleCards = $"{FindStringInBetween(line, '[', ']')}";

                                SetPictureBoxTag(seatNumber, _HoleCards);
                                ShowHoleCards(seatNumber);
                            }

                        }
                        else
                        {
                            if (!summaryLineReached)
                            {

                                string _PlayerName = line.Substring(0, (line.IndexOf(' ')));
                                seatNumber = Array.IndexOf(PlayersSeatNumber, _PlayerName).ToString();
                                if (seatNumber != "-1")
                                {
                                    HighlightActionOnSeat(seatNumber);
                                }

                                moneyLine = "";
                                if (line.Contains("raises") || line.Contains("bets")
                                    || line.Contains("calls") || line.Contains("posts")
                                    || line.Contains("adds"))
                                {
                                    // remove the amount from their StackSize
                                    if (seatNumber != "-1")
                                    {
                                        string[] toPot = line.Split(' ');
                                        string amountToPot;
                                        if (line.Contains("(All-in") || line.Contains("adds"))
                                        {
                                            amountToPot = toPot[toPot.Length - 2];
                                            moneyLine = CreateMoneyLine(toPot, toPot.Length - 2, amountToPot);
                                        }
                                        else
                                        {
                                            amountToPot = toPot[toPot.Length - 1];
                                            moneyLine = CreateMoneyLine(toPot, toPot.Length - 1, amountToPot);
                                        }

                                        if (line.Contains("adds"))
                                        {
                                            // player add-on to their stack
                                            UpdateStackSizeForSeat(seatNumber, amountToPot, "add");
                                        }
                                        else
                                        {
                                            UpdateStackSizeForSeat(seatNumber, amountToPot, "subtract", line);
                                        }

                                    }
                                }

                                if (moneyLine.Length > 0) {
                                    AddLineToHandActionListbox(moneyLine);
                                }
                                else
                                {
                                    AddLineToHandActionListbox(line);
                                    //moneyLine = "";
                                }

                                if (line.Contains("shows"))
                                {
                                    string _HoleCards = $"{FindStringInBetween(line, '[', ']')}";

                                    SetPictureBoxTag(seatNumber, _HoleCards);
                                    ShowHoleCards(seatNumber);
                                }
                            }
                            else
                            {
                                if (summaryLineReached)
                                {
                                    // set the hole cards for each player
                                    if (line.Contains("Seat"))
                                    {
                                        seatNumber = line.Substring(5, 1);
                                        string _PlayerName = line.Substring(8, (line.Substring(8).IndexOf("(") - 1));
                                        string _HoleCards = $"{FindStringInBetween(line, '[', ']')}";
                                        SetPictureBoxTag(seatNumber, _HoleCards);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (summaryLineReached)
                        {
                            // set the hole cards for each player
                            if (line.Contains("Seat"))
                            {
                                seatNumber = line.Substring(5, 1);
                                string _PlayerName = line.Substring(8, (line.Substring(8).IndexOf("(") - 1));
                                string _HoleCards = $"{FindStringInBetween(line, '[', ']')}";
                                SetPictureBoxTag(seatNumber, _HoleCards);
                            }
                        }
                        else
                        {
                            if (!line.Contains("wins") && !line.Contains("Rake") && !line.Contains("Board") && !line.Contains("splits"))
                            {
                                
                                string _PlayerName = line.Substring(0, (line.IndexOf(' ')));
                                seatNumber = Array.IndexOf(PlayersSeatNumber, _PlayerName).ToString();
                                if (seatNumber != "-1")
                                {
                                    HighlightActionOnSeat(seatNumber);
                                }

                                moneyLine = "";
                                if (line.Contains("raises") || line.Contains("bets")
                                    || line.Contains("calls") || line.Contains("posts")
                                    || line.Contains("adds"))
                                {
                                    // remove the amount from their StackSize
                                    if (seatNumber != "-1")
                                    {
                                        string[] toPot = line.Split(' ');
                                        string amountToPot;
                                        if (line.Contains("(All-in") || line.Contains("adds"))
                                        {
                                            amountToPot = toPot[toPot.Length - 2];
                                            moneyLine = CreateMoneyLine(toPot, toPot.Length - 2, amountToPot);
                                        }
                                        else
                                        {
                                            amountToPot = toPot[toPot.Length - 1];
                                            moneyLine = CreateMoneyLine(toPot, toPot.Length - 1, amountToPot);
                                        }

                                        if (line.Contains("adds"))
                                        {
                                            UpdateStackSizeForSeat(seatNumber, amountToPot, "add");
                                        }
                                        else
                                        {
                                            UpdateStackSizeForSeat(seatNumber, amountToPot, "subtract", line);
                                        }

                                    }
                                }

                                if (moneyLine.Length > 0)
                                {
                                    AddLineToHandActionListbox(moneyLine);
                                }
                                else
                                {
                                    AddLineToHandActionListbox(line);
                                    //moneyLine = "";
                                }

                                if (line.Contains("shows"))
                                {
                                    string _HoleCards = $"{FindStringInBetween(line, '[', ']')}";

                                    SetPictureBoxTag(seatNumber, _HoleCards);
                                    ShowHoleCards(seatNumber);
                                }
                            }
                        }
                    }
                }
            }
            ShowHoleCards("");
            btnNext.Visible = false;
            lblThePot.Text = "";
            Application.DoEvents();
        }

        private String CreateMoneyLine(string[] toPot, int moneyIndex, string amountToPot)
        {
            // converts the amount in the hand history to a dollar amount
            String moneyLine = "";
            for (int i = 0; i < toPot.Length; i++)
            {
                if (i != moneyIndex)
                {
                    moneyLine += $"{toPot[i]} ";
                }
                else
                {
                    moneyLine += $"{double.Parse(amountToPot).ToString("C2")} ";
                }
            }

            return moneyLine;
        }

        private void SetPictureBoxTag(String seatNumber, String HoleCards)
        {
            // using the tag property to indicate which card should be displayed 
            var res = from pb in grpHandDisplay.Controls.OfType<PictureBox>()
                      where pb.Name.Contains($"pbPlayer{seatNumber}")
                      select pb;

            String[] _holeCards = HoleCards.Split(' ');

            foreach (PictureBox pb in res)
            {
                if (pb.Name.Contains("Card1"))
                {
                    pb.Tag = _holeCards[0];
                }
                else
                {
                    pb.Tag = _holeCards[1];
                }

            }
        }

        private void ShowHoleCards(String seatNum)
        {
            // pass a seat number if you want to show 1 players hand (typically at showdown)
            // else this query will return all the lblSeatPlayer labels
            var res = from lbl in grpHandDisplay.Controls.OfType<Label>()
                      where lbl.Name.Contains($"lblSeatPlayer{seatNum}")
                      select lbl;

            String seatNumber;
            foreach (Label lbl in res)
            {
                if (lbl.Text != "" && !lbl.Text.Contains(" SO"))
                {
                    // there was a player seated at the start of the hand and they weren't sitting out
                    seatNumber = lbl.Name.Substring(13, 1);
                    DisplayCardImage($"P{seatNumber}C1", "");
                    DisplayCardImage($"P{seatNumber}C2", "");
                }
            }
        }

        private void PlayerFoldsHideCardImages(String seatNumber)
        {
            // hide the players cards when they fold 
            var res = from pb in grpHandDisplay.Controls.OfType<PictureBox>()
                      where pb.Name.Contains($"pbPlayer{seatNumber}")
                      select pb;

            foreach (PictureBox pb in res)
            {
                pb.Visible = false;
            }
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            if (dlgHandHistory.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    tsLBLFileName.Text = $"Hand history file: {dlgHandHistory.FileName}";
                    ReadHandHistoryFile(dlgHandHistory.FileName);
                    cboHands.Visible = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to use the selected file.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
            }
        }

        private String CardValue(int value, String suit)
        {
            String cardValue;
            switch (value)
            {
                case 0:
                    cardValue = "A";
                    break;
                case 9:
                    cardValue = "T";
                    break;
                case 10:
                    cardValue = "J";
                    break;
                case 11:
                    cardValue = "Q";
                    break;
                case 12:
                    cardValue = "K";
                    break;
                default:
                    cardValue = (value + 1).ToString();
                    break;
            }

            return $"{cardValue}{suit}";
        }

        private String CardImageName(String value)
        {
            String directory = AppDomain.CurrentDomain.BaseDirectory;
            String CardImagePath = $"{directory}images\\cards\\";
                        
            if (!Directory.Exists(CardImagePath))
            {
                throw new System.ArgumentException("Missing card image folder. A folders named 'images\\cards' must exist.");
            }
            String suit = value.Substring(1) == "s" ? "spades" : value.Substring(1) == "h" ? "hearts"
                                                : value.Substring(1) == "d" ? "diamonds" : "clubs";
            String CardImg;

            if (backOfCardImageFile.Length==0)
            {
                backOfCardImageFile = $"{CardImagePath}back.png";
            }

            switch (value.Substring(0, 1))
            {
                case "A":
                    CardImg = $"{CardImagePath}ace_of_{suit}.png";
                    break;
                case "T":
                    CardImg = $"{CardImagePath}10_of_{suit}.png";
                    break;
                case "J":
                    CardImg = $"{CardImagePath}jack_of_{suit}.png";
                    break;
                case "Q":
                    CardImg = $"{CardImagePath}queen_of_{suit}.png";
                    break;
                case "K":
                    CardImg = $"{CardImagePath}king_of_{suit}.png";
                    break;
                default:
                    CardImg = $"{CardImagePath}{value.Substring(0, 1)}_of_{suit}.png";
                    break;
            }

            return CardImg;
        }

        private void BuildDeck()
        {

            this.deck = new Card[52];
            String cardValue;

            // spades
            for (int i = 0; i < 13; i++)
            {
                cardValue = CardValue(i, "s");
                deck[i] = new Card
                {
                    Value = cardValue,
                    CardImg = CardImageName(cardValue)
                };
            }

            // hearts
            for (int i = 0; i < 13; i++)
            {
                cardValue = CardValue(i, "h");
                deck[i + 13] = new Card
                {
                    Value = cardValue,
                    CardImg = CardImageName(cardValue)
                };
            }

            // diamonds
            for (int i = 0; i < 13; i++)
            {
                cardValue = CardValue(i, "d");
                deck[i + 26] = new Card
                {
                    Value = cardValue,
                    CardImg = CardImageName(cardValue)
                };
            }

            // clubs
            for (int i = 0; i < 13; i++)
            {
                cardValue = CardValue(i, "c");
                deck[i + 39] = new Card
                {
                    Value = cardValue,
                    CardImg = CardImageName(cardValue)
                };
            }

            //for (int i = 0; i < 52; i++)
            //{
            //    lbHands.Items.Add($"card: {deck[i].Value}");
            //}

        }

        private void frmMain_Load(object sender, EventArgs e)
        {

            try
            {
                BuildDeck();
            } catch (Exception ex)
            {
                MessageBox.Show($"Unable to build the deck of cards.\n\nError message: {ex.Message}");
                Application.Exit();
            }

        }

        private void ClearTableInfo()
        {
            // hide and/or clear information from previously displayed hand
            foreach (Control c in grpHandDisplay.Controls)
            {
                if (c.Name.Contains("lblSeatPosition") || c.Name.Contains("lblSeatPlayer")
                    || c.Name.Contains("lblStackSize") || c.Name.Contains("pbPlayer"))
                {
                    if (c is Label) { c.Text = ""; }
                    c.Visible = false;
                }
            }
        }

        void ResetTable()
        {
            NextButtonClicked = false;
            btnNext.Visible = false;

            lbHandAction.Items.Clear();
            lbHandAction.Visible = false;

            pbFlopCard1.Visible = false;
            pbFlopCard2.Visible = false;
            pbFlopCard3.Visible = false;

            pbTurnCard.Visible = false;
            pbRiverCard.Visible = false;

            lblThePot.Text = "";

            ClearTableInfo();

        }

        bool NextButtonClicked = false;
        private void btnNext_Click(object sender, EventArgs e)
        {
            if (cboHands.SelectedIndex > 0)
            {
                // user clicked the next button which jumps out of a waiting loop in the showhand routine to 
                // show the next event that occurs in the hand
                NextButtonClicked = true;
            }
        }

        bool UserClickedClose = false;

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // indicate that the red x was clicked - so the ShowHand routine knows to end
            UserClickedClose = true;
        }

        private void cboHands_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                ResetTable();

                grpHandDisplay.Visible = true;
                lblGameType.Visible = true;
                lblPotTitle.Visible = true;

                ShowHand(cboHands.SelectedIndex);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to build display the selected hand.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
            }

        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
