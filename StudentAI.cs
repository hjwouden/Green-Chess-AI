//************************************************************************************************//
//                           Mean Green Chess StudentAI.cs file
//________________________________________________________________________________________________
//          Change Log
//================================================================================================
//  Date            Name               Changes
//================================================================================================
// 10-10-2014      Kiaya                Optimized the GreedyMove to reduce the number of calls to MoveResultsInCheck and CheckingForCheckAndCheckmate
//                                      Added three features to the ValueOfBoard
//                                              1. If our high ranking pieces are in danger reduce the value of the board (relative to the value of the piece)
//                                              2. If one of their pieces are in danger we increase the value of the board (relative to the value of the piece)
//                                              3. If we have more pieces on the board then they do we increase the value of the board (and of course the reverse)
//                                      Fix for the timer... I think.
// 10-9-2014       Jason                Added: Timer   (OnTimedEvent <sets bool value isTimeUp>)
//                                                     (StartTimer   <starts the timer to set interval currently 5 sec>)
//                                                     (StopTimer   <stops the timer, makes sure we dont carry time over to next turn>)
//                                      Implemented: check for isTimeUp in while loops (Greedymove, GetNextMove)
//
//  10-3-2014       Greg                Added:
//                                          bool IsTerminalNode(ChessBoard board, ChessColor myColor)
//                                          int MaxMove(ChessBoard board, ChessColor myColor, int depth, int alpha, int beta)
//                                          int MinMove(ChessBoard board, ChessColor myColor, int depth, int alpha, int beta)
//                                      Changed:
//                                          checkingForCheckOrCheckmate(ChessMove move, ChessBoard board, ChessColor myColor)
//                                          ChessMove GreedyMove(ChessBoard board, ChessColor myColor)
//                                          int ValueOfBoard(ChessBoard board, ChessColor color, MinMaxPlayer p)
//                                      Read the readme.txt for more information
//  9-29-2014       jason                Checking opponents move. Works but discovers error in putting opponent in checkmate, we dont signal they are in checkmate, just put them in check. need to fix.
//  9-28-2014       kiaya               Handle setting check flag when we move
//  9-28-2014       kiaya               Handle setting checkmate flag when we move
//  9-28-2014       kiaya               Weighting moves that put the opponent in check or check mate higher than other moves
//  9-28-2014       kiaya               Fix to Queen, Rook, and Bishop moves as discribed in readme file
//  9-28-2014       kiaya               playerInCheck(...) function to help with all the check/checkmate stuff we needed. Used in MoveResultsInCheck, CheckingForCheckAndCheckmate, and adjustValueOfMoveBasedOnCheckOrCheckMate
//  9-28-2014       kiaya               Fix to greedy move to handle if the "random" move is going to put us in check.  
//  9-27-2014       greg                GreedyMove now choses a "random" move if all move values are the same
//  9-24-2014       greg                IMPORTANT UPDATE
//  9-24-2014       kiaya               Kiaya_Knight_Canidate
//  9-26-2014       jason               reorganized code and clean up
//  9-26-2014       jason               Added attack value function
//  9-26-2014       jason               Implemented GetattackValue in Rook, ERR:Rook wont move right.
//  9-26-2014       jason               Implemented GetAttackValue in Knight
//  9-26-2014       jason               Added Joshs Bishop Canidate
//  9-26-2014       jason               Implemented GetattackValue in Bishop
//  9-26-2014       jason               Added Joshs Queen Canidate
//  9-26-2014       jason               Implemented GetattackValue in Queen
//************************************************************************************************//


using System;
using System.Timers;
using System.Collections.Generic;
using System.Text;
using UvsChess;

namespace StudentAI
{
    public class StudentAI : IChessAI
    {
        #region IChessAI Members that are implemented by the Student

        /// <summary>
        /// The name of your AI
        /// </summary>
        public string Name
        {
#if DEBUG
            get { return "Mean Green Chess Machine (DEBUGE)"; }
#else
            get { return "Mean Green Chess Machine"; }
#endif
        }

        private const int ROWS = 8;
        private const int COLS = 8;
        private const int VALUE_KING = 10;
        private const int VALUE_QUEEN = 8;
        private const int VALUE_ROOK = 5;
        private const int VALUE_KNIGHT = 4;
        private const int VALUE_BISHOP = 3;
        private const int VALUE_PAWN = 1;

        //Timer Test stuff here
        bool isTimeUp = false;
        private static Timer aTimer;
        private static Timer bTimer;
        double Time = 0.0;

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
#if DEBUG
            this.Log("Time is up. Setting isTimeUp to true");
#endif
            isTimeUp = true;
        }


#if DEBUG
        private void OnTimedEventB(object source, ElapsedEventArgs e)
        {
            Time += .5;
            this.Log("Time Elapsed during move: " + Time.ToString());
        }
#endif


        private void startTimer()
        {
            isTimeUp = false;
            double interval = 5000.0;
            //testing smaller interval
            //interval = 500.0;
#if DEBUG
            this.Log("Starting Timer for 5 sec");
            bTimer = new System.Timers.Timer(500.0);
            bTimer.Elapsed += new ElapsedEventHandler(OnTimedEventB);
            bTimer.AutoReset = false;
            bTimer.Enabled = true;
#endif
            aTimer = new System.Timers.Timer(interval);
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.AutoReset = false;
            aTimer.Enabled = true;
        }
        private void resetTimer()
        {
#if DEBUG
            this.Log("Reset Timer called");
            bTimer.Stop();
#endif
            aTimer.Stop();

        }

        // End of Timer Test stuff - HJW

        //Helper function to check if th move is valid,
        //Checks if out of bounds, if the location is empty or if the piece on the board is the same color as the piece trying to move
        private bool simpleValidateMove(ChessLocation loc, ChessBoard board, ChessColor color)
        {
            if (loc.X < 0 || loc.X > 7 || loc.Y < 0 || loc.Y > 7)
            {
                return false;
            }
            if (LocationEmpty(board, loc.X, loc.Y))
            {
                return true;
            }
            else if (ColorOfPieceAt(loc, board) != color)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //checks if the given move will result in check or checkmate and returns the appropriate flag.
        private ChessFlag checkingForCheckOrCheckmate(ChessMove move, ChessBoard board, ChessColor myColor)
        {
            ChessBoard tempBoard = board.Clone();
            if (move != null)
                tempBoard.MakeMove(move);
            // this.Log("######Checking for check with move [" + move.From.X + "," + move.From.Y + "] to [" + move.To.X + "," + move.To.Y + "].");
            ChessColor theirColor = myColor == ChessColor.White ? ChessColor.Black : ChessColor.White;
            if (playerInCheck(tempBoard, theirColor))
            {
                bool canGetOutOfCheck = false;
                List<ChessMove> kingMoves = GetMovesForKing(GetKingsPosition(tempBoard, theirColor), tempBoard, theirColor);
                foreach (var kingMove in kingMoves)
                {
                    ChessBoard temp2 = tempBoard.Clone();
                    temp2.MakeMove(kingMove);
                    if (!playerInCheck(temp2, theirColor))
                    {
                        canGetOutOfCheck = true;
                    }
                }
                if (!canGetOutOfCheck)
                {
                    //this.Log("King can't move to get out of check. Checking the other pieces to see if they can get the king out of check");
                    List<ChessMove> allEnemiesMoves = new List<ChessMove>();
                    for (int i = 0; i < ROWS; ++i)
                    {
                        for (int j = 0; j < COLS; ++j)
                        {
                            ChessLocation pieceLoc = new ChessLocation(i, j);
                            ChessColor theColor = ColorOfPieceAt(pieceLoc, tempBoard);
                            if (theColor != myColor && tempBoard[i, j] != ChessPiece.Empty)
                            {
                                allEnemiesMoves.AddRange(GetAllValidMovesFor(pieceLoc, tempBoard));
                            }
                        }
                    }
                    foreach (var enemyMove in allEnemiesMoves)
                    {
                        ChessBoard temp2 = tempBoard.Clone();
                        temp2.MakeMove(enemyMove);
                        if (!playerInCheck(temp2, theirColor))
                        {
                            canGetOutOfCheck = true;
                        }
                    }
                }
                if (!canGetOutOfCheck)
                {
                    return ChessFlag.Checkmate;
                }
                else
                {
                    return ChessFlag.Check;
                }
            }
            else
            {
                List<ChessMove> allEnemiesMoves = new List<ChessMove>();
                for (int i = 0; i < ROWS; ++i)
                {
                    for (int j = 0; j < COLS; ++j)
                    {
                        ChessLocation pieceLoc = new ChessLocation(i, j);
                        ChessColor theColor = ColorOfPieceAt(pieceLoc, tempBoard);
                        if (theColor != myColor && tempBoard[i, j] != ChessPiece.Empty)
                        {
                            allEnemiesMoves.AddRange(GetAllValidMovesFor(pieceLoc, tempBoard));
                        }
                    }
                }
                if(allEnemiesMoves.Count == 0)
                {
                    return ChessFlag.Stalemate;
                }
                return ChessFlag.NoFlag;
            }
        }

        //checks if a move will result in check or checkmate (if they do it will add the appropriate flag) and adds a large amount to the value of the move. 
        private void adjustValueOfMoveBasedOnCheckOrCheckMate(ref ChessMove move, ChessBoard board, ChessColor myColor)
        {
            move.Flag = checkingForCheckOrCheckmate(move, board, myColor);
            if (move.Flag == ChessFlag.Check)
            {
#if DEBUG
                this.Log("Found a move to put them in check, adjusting value of move");
#endif
                move.ValueOfMove += 10;
            }
            else if (move.Flag == ChessFlag.Checkmate)
            {
#if DEBUG
                this.Log("Found a move to put them in checkMate, adjusting value of move");
#endif
                move.ValueOfMove += 1000;
            }
        }

        // Returns true if this move results in check (used to prune moves from move lists!).
        // False otherwise
        private bool MoveResultsInCheck(ChessBoard board, ChessMove move, ChessColor myColor)
        {
            // this works by making a copy of the chessboard, augmenting it with the move in question, 
            // and then getting all possible moves of the opposing color. If the opposing color can make
            // any move which would take the king, then the move has resulted in check by definition.

            // Create new temp board and make supposed move
            ChessBoard tempBoard = new ChessBoard(board.RawBoard);
            tempBoard.MakeMove(move);
            //use the player in check to see if we will be in check as a result of the move
            return playerInCheck(tempBoard, myColor);
        }

        private ChessLocation GetKingsPosition(ChessBoard board, ChessColor myColor)
        {
            for (int i = 0; i < ROWS; ++i)
            {
                for (int j = 0; j < COLS; ++j)
                {
                    if (myColor == ChessColor.Black && board[i, j] == ChessPiece.BlackKing)
                        return new ChessLocation(i, j);
                    else if (myColor == ChessColor.White && board[i, j] == ChessPiece.WhiteKing)
                        return new ChessLocation(i, j);
                }
            }
            return new ChessLocation(-1, -1);
        }

        //looks through all moves the opponent of a player (given by the color) and checks to see if any of those moves result in the king being killed.
        private bool playerInCheck(ChessBoard board, ChessColor color)
        {
            ChessBoard tempBoard = board.Clone();
            //this.Log("Calling playerInCheck for " + color);
            //get all moves of the opposing color
            List<ChessMove> allEnemiesMoves = new List<ChessMove>();
            for (int i = 0; i < ROWS; ++i)
            {
                for (int j = 0; j < COLS; ++j)
                {
                    ChessLocation pieceLoc = new ChessLocation(i, j);
                    ChessColor theColor = ColorOfPieceAt(pieceLoc, tempBoard);
                    if (theColor != color && tempBoard[i, j] != ChessPiece.Empty)
                    {
                        allEnemiesMoves.AddRange(GetAllValidMovesFor(pieceLoc, tempBoard));
                    }
                }
            }
            //this.Log("playerInCheck found " + allEnemiesMoves.Count + " moves that the other team could make.");
            //check if any of those moves will kill a king.
            foreach (ChessMove nextPossibleMove in allEnemiesMoves)
            {
                //this.Log("Checking for check with move [" + nextPossibleMove.From.X + "," + nextPossibleMove.From.Y + "] to [" + nextPossibleMove.To.X + "," + nextPossibleMove.To.Y + "]. The value of that piece is " + GetValueOfPiece(tempBoard[nextPossibleMove.To]));
                if (ColorOfPieceAt(nextPossibleMove.To, tempBoard) == color && GetValueOfPiece(tempBoard[nextPossibleMove.To]) == 10)
                {
                    //this.Log("returning true Found a move that puts in check");
                    return true;
                }
            }
            return false;
        }

        private bool IsTerminalNode(ChessBoard board, ChessColor myColor)
        {
            // returns true if the board's state is terminal (ie: checkmate)
            if (checkingForCheckOrCheckmate(null, board, myColor) == ChessFlag.Checkmate)
            {
                return true;
            }
            return false;
        }

        private ChessColor GetEnemyColor(ChessColor myColor)
        {
            if (myColor == ChessColor.White) return ChessColor.Black;
            else return ChessColor.White;
        }

        private int MaxMove(ChessBoard board, ChessColor myColor, int depth, int alpha, int beta, ref Dictionary<int, ChessBoard> plys)
        {
            if (depth == 0 || IsTerminalNode(board, myColor) || playerInCheck(board, myColor) || isTimeUp == true)
            {
                int val = ValueOfBoard(board, myColor, MinMaxPlayer.Max);
                //this.Log("Value of Max board: " + val);
                return val;
            }
            else
            {
                List<ChessMove> childMoves = new List<ChessMove>();
                Successors(board, myColor, ref childMoves);
                ChessBoard tempBoard = null;
                if (plys.ContainsKey(depth))
                    tempBoard = plys[depth];
                else
                {
                    tempBoard = board.Clone();
                    plys[depth] = tempBoard;
                }

                for (int i = 0; i < childMoves.Count; ++i)
                {
                    tempBoard = plys[depth];
                    tempBoard.MakeMove(childMoves[i]);
                    alpha = Math.Max(alpha, MinMove(tempBoard, GetEnemyColor(myColor), depth - 1, alpha, beta, ref plys));
                    if (alpha >= beta)
                        return beta;
                }
                return alpha;
            }
        }

        private int MinMove(ChessBoard board, ChessColor myColor, int depth, int alpha, int beta, ref Dictionary<int, ChessBoard> plys)
        {
            if (depth == 0 || IsTerminalNode(board, myColor) || playerInCheck(board, myColor) || isTimeUp == true)
            {
                int val = ValueOfBoard(board, myColor, MinMaxPlayer.Min);
                //this.Log("Value of Min board: " + val);
                return val;
            }
            else
            {
                List<ChessMove> childMoves = new List<ChessMove>();
                Successors(board, myColor, ref childMoves);
                ChessBoard tempBoard = null;
                if (plys.ContainsKey(depth))
                    tempBoard = plys[depth];
                else
                {
                    tempBoard = board.Clone();
                    plys[depth] = tempBoard;
                }

                for (int i = 0; i < childMoves.Count; ++i)
                {
                    tempBoard = plys[depth];
                    tempBoard.MakeMove(childMoves[i]);
                    beta = Math.Min(beta, MaxMove(tempBoard, GetEnemyColor(myColor), depth - 1, alpha, beta, ref plys));
                    if (beta <= alpha)
                        return alpha;
                }
                return beta;
            }
        }


        private ChessMove GreedyMove(ChessBoard board, ChessColor myColor)
        {
            // All Greedy move will do is expand the current board, getting
            // successor moves, and will simply return the move that has the highest move value!
            
            List<ChessMove> possibleMoves = new List<ChessMove>();
            int bestMoveValue = -99;
            int bestMoveValueIndex = 0;

            ChessMove bestMove = null;
            bool moveFound = false;

            // Check to see if opponents move was a valid one.
            //Get previous board, get current board, see what the move made was.


            Successors(board, myColor, ref possibleMoves);

            // This doesn't actually work nicely. The move is just registered as an illegal move,
            // but I still use it here for logging purposes. 
            if (possibleMoves.Count == 0)
            {
                // Checkmate, generally, but just return stalemate?
                ChessMove move = new ChessMove(new ChessLocation(0, 0), new ChessLocation(0, 0));
                this.Log("PossibleMoves was empty, signaling a checkmate");
                move.Flag = ChessFlag.Checkmate;
                return move;
            }
            //else  //Run through all possible moves and determine if any of them will result in us putting the enemy in check or checkmate. Weight these moves higher than others.
            //{
            //    for (int i = 0; i < possibleMoves.Count; ++i)
            //    {
            //        if (MoveResultsInCheck(board, possibleMoves[i], myColor))
            //        {
            //            this.Log("Move resulted in check. Remove from queue!");
            //            possibleMoves.Remove(possibleMoves[i]);
            //        }
            //    }
            //}


            while (bestMove == null)
            {
                Dictionary<int, ChessBoard> plyMap = new Dictionary<int, ChessBoard>();
                for (int i = 0; i < possibleMoves.Count; ++i)
                {
                    if(isTimeUp == true)
                    {
                        break;
                    }
                    ChessBoard tempBoard = board.Clone();
                    tempBoard.MakeMove(possibleMoves[i]);
                    int depth = 4;
                    plyMap.Add(depth, tempBoard);
                    int minimaxVal = -100000;
                    minimaxVal = MaxMove(tempBoard, myColor, depth, -1000000, 1000000, ref plyMap);
                    this.Log("Minimax value for generated move: " + possibleMoves[i].From.X +" , " + possibleMoves[i].From.Y + " " + minimaxVal);
                    plyMap.Clear();

                    if (minimaxVal > bestMoveValue)
                    {
                        bestMoveValueIndex = i;
                        bestMoveValue = minimaxVal;
                    }
                }

                this.Log("Minimax move found " + possibleMoves.Count + " moves. Value of best is: " + bestMoveValue);

                bestMove = possibleMoves[bestMoveValueIndex];
                bestMove.Flag = checkingForCheckOrCheckmate(bestMove, board, myColor);
                moveFound = true;
                //if (!MoveResultsInCheck(board, possibleMoves[bestMoveValueIndex], myColor))
                //{
                //    bestMove = possibleMoves[bestMoveValueIndex];
                //    moveFound = true;
                //}
                //else // remove this move as it results in check for us!
                //{
                //    possibleMoves.Remove(possibleMoves[bestMoveValueIndex]);
                //    bestMoveValue = bestMoveValueIndex = 0;
                //    if (possibleMoves.Count == 0)
                //        break;
                //    this.Log("A move was detected that could result in check. Removed from move queue");
                //    this.Log("The the piece was going to move to: " + possibleMoves[bestMoveValueIndex].To.X + ", " + possibleMoves[bestMoveValueIndex].To.Y);
                //}
            }
            if (moveFound)
                return bestMove;
            else
                return null;
        }

        private void Successors(ChessBoard board, ChessColor myColor, ref List<ChessMove> movesForEachSucc)
        {

            //this.Log("Accumulating successors");

            for (int i = 0; i < ROWS; ++i)
            {
                for (int j = 0; j < COLS; ++j)
                {
                    // Only get potential moves if they are for our color (for now)
                    if (ColorOfPieceAt(new ChessLocation(i, j), board) == myColor && board[i, j] != ChessPiece.Empty)
                    {
                        List<ChessMove> validMovesForPiece = null;
                        // if (board[i,j] == ChessPiece.WhitePawn || board[i, j] == ChessPiece.BlackPawn) // TESTING
                        validMovesForPiece = GetAllValidMovesFor(new ChessLocation(i, j), board);

                        if (validMovesForPiece != null)
                        {
                            // for each piece, we generate all legal moves, and make a new board where that move
                            // is made to cover all possible choices. 
                            for (int k = 0; k < validMovesForPiece.Count; ++k)
                            {
                                if (movesForEachSucc != null)
                                    movesForEachSucc.Add(validMovesForPiece[k]);
                            }
                        }
                    }
                }
            }
        }

        private ChessColor ColorOfPieceAt(ChessLocation loc, ChessBoard board)
        {
            if (board[loc] < ChessPiece.Empty)
                return ChessColor.Black;
            return ChessColor.White;
        }

        // Returns a list of all moves that are valid for the ChessPiece located at loc
        private List<ChessMove> GetAllValidMovesFor(ChessLocation loc, ChessBoard board)
        {
            ChessPiece piece = board[loc];
            List<ChessMove> moves = null;

            // each piece will have its own movement logic based on the piece that it is,
            // and also (in some cases) whether it is black or white.
            switch (piece)
            {
                case ChessPiece.BlackPawn:
                    moves = GetMovesForPawn(loc, board, ChessColor.Black);
                    break;
                case ChessPiece.BlackRook:
                    moves = GetMovesForRook(loc, board, ChessColor.Black);
                    break;
                case ChessPiece.BlackKnight:
                    moves = GetMovesForKnight(loc, board, ChessColor.Black);
                    break;
                case ChessPiece.BlackBishop:
                    moves = GetMovesForBishop(loc, board, ChessColor.Black);
                    break;
                case ChessPiece.BlackQueen:
                    moves = GetMovesForQueen(loc, board, ChessColor.Black);
                    break;
                case ChessPiece.BlackKing:
                    moves = GetMovesForKing(loc, board, ChessColor.Black);
                    break;
                case ChessPiece.WhitePawn:
                    moves = GetMovesForPawn(loc, board, ChessColor.White);
                    break;
                case ChessPiece.WhiteRook:
                    moves = GetMovesForRook(loc, board, ChessColor.White);
                    break;
                case ChessPiece.WhiteKnight:
                    moves = GetMovesForKnight(loc, board, ChessColor.White);
                    break;
                case ChessPiece.WhiteBishop:
                    moves = GetMovesForBishop(loc, board, ChessColor.White);
                    break;
                case ChessPiece.WhiteQueen:
                    moves = GetMovesForQueen(loc, board, ChessColor.White);
                    break;
                case ChessPiece.WhiteKing:
                    moves = GetMovesForKing(loc, board, ChessColor.White);
                    break;
                default:
                    // assume Empty and do nothing
                    break;
            }
            return moves;
        }

        // Evaluation function of the strength of the board state.
        enum MinMaxPlayer { Max, Min };
        private int ValueOfBoard(ChessBoard board, ChessColor color, MinMaxPlayer p)
        {
            // every board has a different state
            // Index 0 is pawns, then 1 is bishop, etc
            List<int> units = NumOfUnits(board, color);
            List<int> unitsEnemy = NumOfUnits(board, GetEnemyColor(color));
            List<ChessMove> possibleMove = new List<ChessMove>();
            List<ChessMove> possibleMoveEnemy = new List<ChessMove>();
            for (int i = 0; i < 8; ++i)
            {
                for (int k = 0; k < 8; ++k)
                {
                    if (board[i, k] != ChessPiece.Empty)
                    {
                        if (ColorOfPieceAt(new ChessLocation(i, k), board) == color)
                        {
                            possibleMove.AddRange(GetAllValidMovesFor(new ChessLocation(i, k), board));
                        }
                        else
                        {
                            possibleMoveEnemy.AddRange(GetAllValidMovesFor(new ChessLocation(i, k), board));
                        }
                    }
                }
            }

            int totalMovesAvailable = possibleMove.Count;
            int totalMovesAvailableEnemy = possibleMoveEnemy.Count;

            int currentValueOfBoard = 0;

            currentValueOfBoard = units.Count - unitsEnemy.Count;

            //If any of the higher value pieces are in danger, lower board value.
            //This will handle lowering the board value if we are in check in this particular board variation
            foreach (var enemyMove in possibleMoveEnemy)
            {
                int result = PieceAtPosInDanger(board, enemyMove.To.X, enemyMove.To.Y);
                //if (result == 100)
                //{
                //    return -100;
                //}
                //else
                //{
                    currentValueOfBoard = currentValueOfBoard - result;
                //}
            }

            foreach (var move in possibleMove)
            {
                //If any of our moves could result in us taking a higher piece increase the board value;
                //This will handle increaseing the board value if we put them in check
                currentValueOfBoard = currentValueOfBoard + PieceAtPosInDanger(board, move.To.X, move.To.Y);
            }

            // old feature (probably still good, but I commented it out for testing
            //int unitWeight = VALUE_KING + VALUE_QUEEN * units[4] + VALUE_ROOK * units[3] + VALUE_KNIGHT * units[2] +
            //  VALUE_BISHOP * units[1] + VALUE_PAWN * units[0];


            // This example feature maximizes the total moves available to Max, while
            // attempting to minimize the total moves available to min. Seems to work fairly well
            // as a feature by itself, but obviously we need more!

            // Good features to add:
            // 1) Check to see if high value peices are in danger. If so, lower board state value
            // 2) Check to see if high mobility peices are local to the center of the board where they will
            //    have most movement ability. 
            // 3) Try to find other good features to add to score
            //this.Log("Value of board will be: " + (totalMovesAvailable - totalMovesAvailableEnemy));
            currentValueOfBoard += totalMovesAvailable - totalMovesAvailableEnemy;
            return currentValueOfBoard;
        }

        //looks at the position given. If a piece is there 
        //then we will return the value that should be taken off of the current value of the board
        private int PieceAtPosInDanger(ChessBoard board, int x, int y)
        {
            int valueOfPieceAtPos = GetValueOfPiece(board[x, y]);
            if (valueOfPieceAtPos == VALUE_KING)
            {
                //this.Log("PieceAtPosInDanger found king in danger. returning 100;");
                return 100; //Make value of king very high
            }
            else
            {
                return valueOfPieceAtPos;
            }
            return 0;
        }

        private List<int> NumOfUnits(ChessBoard board, ChessColor color)
        {
            List<int> unitCounts = new List<int>();
            for (int i = 0; i < 5; ++i)
            {
                unitCounts.Add(0);
            }

            for (int i = 0; i < ROWS; ++i)
            {
                for (int j = 0; j < COLS; ++j)
                {
                    if (ColorOfPieceAt(new ChessLocation(i, j), board) == color)
                    {
                        switch (board[i, j])
                        {

                            case ChessPiece.BlackQueen:
                            case ChessPiece.WhiteQueen:
                                unitCounts[4]++;
                                break;
                            case ChessPiece.BlackRook:
                            case ChessPiece.WhiteRook:
                                unitCounts[3]++;
                                break;
                            case ChessPiece.BlackKnight:
                            case ChessPiece.WhiteKnight:
                                unitCounts[2]++;
                                break;
                            case ChessPiece.BlackBishop:
                            case ChessPiece.WhiteBishop:
                                unitCounts[1]++;
                                break;
                            case ChessPiece.BlackPawn:
                            case ChessPiece.WhitePawn:
                                unitCounts[0]++;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            return unitCounts;
        }

        // Given a chesspiece p returns its value in the game
        private int GetValueOfPiece(ChessPiece p)
        {
            switch (p)
            {
                case ChessPiece.BlackPawn:
                case ChessPiece.WhitePawn:
                    return VALUE_PAWN;
                case ChessPiece.WhiteBishop:
                case ChessPiece.BlackBishop:
                    return VALUE_BISHOP;
                case ChessPiece.WhiteKnight:
                case ChessPiece.BlackKnight:
                    return VALUE_KNIGHT;
                case ChessPiece.WhiteRook:
                case ChessPiece.BlackRook:
                    return VALUE_ROOK;
                case ChessPiece.WhiteQueen:
                case ChessPiece.BlackQueen:
                    return VALUE_QUEEN;
                case ChessPiece.WhiteKing:
                case ChessPiece.BlackKing:
                    return VALUE_KING;
                default:
                    return 0;
            }
        }

        // Returns true if the location specified by x and y does not contain any game unit
        private bool LocationEmpty(ChessBoard board, int x, int y)
        {
            return (ChessPiece.Empty == board[x, y]) ? true : false;
        }


        /// <summary>
        /// Evaluates the chess board and decided which move to make. This is the main method of the AI.
        /// The framework will call this method when it's your turn.
        /// </summary>
        /// <param name="board">Current chess board</param>
        /// <param name="yourColor">Your color</param>
        /// <returns> Returns the best chess move the player has for the given chess board</returns>
        public ChessMove GetNextMove(ChessBoard board, ChessColor myColor)
        {
            //starting the timer for 5 sec
            startTimer();
            ChessMove myNextMove = null;

            while (!IsMyTurnOver() && (isTimeUp != true))
            {
                if (myNextMove == null)
                {
                    // Greedy move, or whatever generates a move, needs to run on a timer eventually
                    // myNextMove = GreedyMove(board, myColor);
                    myNextMove = GreedyMove(board, myColor);
                    if (!IsValidMove(board, myNextMove, myColor))
                        this.Log("Minimax generated an illegal move");

                    this.Log(myColor.ToString() + " (" + this.Name + ") just moved.");
                    this.Log(string.Empty);

                    // Since I have a move, break out of loop
                    break;
                }
            }
            resetTimer();
            return myNextMove;
        }

        /// <summary>
        /// Validates a move. The framework uses this to validate the opponents move.
        /// </summary>
        /// <param name="boardBeforeMove">The board as it currently is _before_ the move.</param>
        /// <param name="moveToCheck">This is the move that needs to be checked to see if it's valid.</param>
        /// <param name="colorOfPlayerMoving">This is the color of the player who's making the move.</param>
        /// <returns>Returns true if the move was valid</returns>
        public bool IsValidMove(ChessBoard boardBeforeMove, ChessMove moveToCheck, ChessColor colorOfPlayerMoving)
        {
            this.Log("Validating Move:" + colorOfPlayerMoving.ToString());
            this.Log("Move to validate: " + moveToCheck.ToString());

            if (playerInCheck(boardBeforeMove, colorOfPlayerMoving))
            {
                if (MoveResultsInCheck(boardBeforeMove, moveToCheck, colorOfPlayerMoving))
                {
                    //write in log that player was in check at start of move and still in check at end of move
                    this.Log("Opponent was in CHECK at start of their move and still in CHECK after move. Invalid Move");
                    return false;
                }
            }

            List<ChessMove> possibleMoves = new List<ChessMove>();
            Successors(boardBeforeMove, colorOfPlayerMoving, ref possibleMoves);

            for (int i = 0; i < possibleMoves.Count; i++)
            {
                this.Log("Checking Successor list: ");

                if (moveToCheck.To == possibleMoves[i].To && moveToCheck.From == possibleMoves[i].From)
                {
                    this.Log("Valid Move");
                    return true;
                }
            }
            this.Log("Move was not found in list our list of successors. Invalid Move");
            return false;

            /*
            List<ChessMove> potentialMoves = GetAllValidMovesFor(moveToCheck.From, boardBeforeMove);
            if (potentialMoves.Count == 0) return false; // no moves
            else
            {
                // if moveToCheck exists in our list, then it is a valid move (assuming GetAllValidMovesFor works!)
                if (potentialMoves.Contains(moveToCheck))
                    return true;
                return false;
            }
             * */
            // return true;
        }

        // Returns int value. Attack Value to assign to a move.
        public int GetAttackMoveValue(int MyPieceValue, ChessLocation EnemyLocation, ChessBoard board)
        {
            //To take the Highest Enemy Piece available, just set value to Enemy Piece Value
            int EnemyValue = GetValueOfPiece(board[EnemyLocation]);
            return EnemyValue;

            // //Prevous Calculation
            // int EnemyValue = GetValueOfPiece(board[EnemyLocation]);
            // return (Math.Abs(MyPieceValue - EnemyValue));
        } //End GetAttackMoveValue - HJW


        //***********************************************************************************//
        //Get Move Functions for Pieces  // All moves that can be done by units in the game
        //**********************************************************************************//

        private List<ChessMove> GetMovesForPawn(ChessLocation loc, ChessBoard board, ChessColor color)
        {
            List<ChessMove> moves = new List<ChessMove>();

            if (ChessColor.Black == color)
            {
                // we are approaching from low values of Y to higher ones
                if (PawnHasNotMoved(loc, ChessColor.Black) && LocationEmpty(board, loc.X, loc.Y + 2) && LocationEmpty(board, loc.X, loc.Y + 1))
                {
                    // we may also move twice towards the enemy
                    moves.Add(new ChessMove(loc, new ChessLocation(loc.X, loc.Y + 2)));
                }

                // move up once if space available -- I should add a move value to this that is suitably high if
                // this results in queening
                if (LocationEmpty(board, loc.X, loc.Y + 1))
                {
                    moves.Add(new ChessMove(loc, new ChessLocation(loc.X, loc.Y + 1)));
                }

                // Finally, if an enemy exists exactly one location "up and over left or right"
                // then we can take that enemy, and the move itself will produce a move value.
                // The move value is just the ABS difference between the two peices, plus 1 so there 
                // is never competition with a 0 move value ChessMove
                ChessMove attackingMoveLeft = null;
                ChessMove attackingMoveRight = null;
                if (loc.X - 1 >= 0 && loc.Y + 1 < ROWS)
                {
                    // we may be able to attack left assuming an enemy is in that spot!
                    ChessLocation enemyLocation = new ChessLocation(loc.X - 1, loc.Y + 1);
                    if (!LocationEmpty(board, enemyLocation.X, enemyLocation.Y) && ChessColor.White == ColorOfPieceAt(enemyLocation, board))
                    {
                        attackingMoveLeft = new ChessMove(loc, enemyLocation);
                        attackingMoveLeft.ValueOfMove = 1 + Math.Abs(VALUE_PAWN - GetValueOfPiece(board[enemyLocation]));
                        moves.Add(attackingMoveLeft);
                    }
                }

                if (loc.X + 1 < COLS && loc.Y + 1 < ROWS)
                {
                    ChessLocation enemyLocation = new ChessLocation(loc.X + 1, loc.Y + 1);
                    if (!LocationEmpty(board, enemyLocation.X, enemyLocation.Y) && ChessColor.White == ColorOfPieceAt(enemyLocation, board))
                    {
                        attackingMoveRight = new ChessMove(loc, enemyLocation);
                        attackingMoveRight.ValueOfMove = 1 + Math.Abs(VALUE_PAWN - GetValueOfPiece(board[enemyLocation]));
                        moves.Add(attackingMoveRight);
                    }
                }
            }
            else // Handle moving White pawns instead. Similar logic with different movement vectors
            {
                if (PawnHasNotMoved(loc, ChessColor.White) && LocationEmpty(board, loc.X, loc.Y - 2) && LocationEmpty(board, loc.X, loc.Y - 1))
                {
                    moves.Add(new ChessMove(loc, new ChessLocation(loc.X, loc.Y - 2)));
                }

                if (LocationEmpty(board, loc.X, loc.Y - 1))
                {
                    moves.Add(new ChessMove(loc, new ChessLocation(loc.X, loc.Y - 1)));
                }

                ChessMove attackingMoveLeft = null;
                ChessMove attackingMoveRight = null;
                if (loc.X - 1 >= 0 && loc.Y - 1 >= 0)
                {
                    ChessLocation enemyLocation = new ChessLocation(loc.X - 1, loc.Y - 1);
                    if (!LocationEmpty(board, enemyLocation.X, enemyLocation.Y) && ChessColor.Black == ColorOfPieceAt(enemyLocation, board))
                    {
                        attackingMoveLeft = new ChessMove(loc, enemyLocation);
                        attackingMoveLeft.ValueOfMove = 1 + Math.Abs(VALUE_PAWN - GetValueOfPiece(board[enemyLocation]));
                        moves.Add(attackingMoveLeft);
                    }
                }

                if (loc.X + 1 < COLS && loc.Y - 1 >= 0)
                {
                    ChessLocation enemyLocation = new ChessLocation(loc.X + 1, loc.Y - 1);
                    if (!LocationEmpty(board, enemyLocation.X, enemyLocation.Y) && ChessColor.Black == ColorOfPieceAt(enemyLocation, board))
                    {
                        attackingMoveRight = new ChessMove(loc, enemyLocation);
                        attackingMoveRight.ValueOfMove = 1 + Math.Abs(VALUE_PAWN - GetValueOfPiece(board[enemyLocation]));
                        moves.Add(attackingMoveRight);
                    }
                }
            }

            return moves;
        }// End GetMovesForPawn

        // Used by GetMovesForPawn. Determines if a pawn, has made its first move. 
        private bool PawnHasNotMoved(ChessLocation loc, ChessColor color)
        {
            const int FIRST_MOV_LOC_BLACK = 1;
            const int FIRST_MOV_LOC_WHITE = 6;
            if (ChessColor.Black == color)
            {
                if (FIRST_MOV_LOC_BLACK == loc.Y)
                    return true;
                return false;
            }
            else
            {
                if (FIRST_MOV_LOC_WHITE == loc.Y)
                    return true;
                return false;
            }
        } // End PawnHasNotMoved

        private List<ChessMove> GetMovesForRook(ChessLocation loc, ChessBoard board, ChessColor color)
        {
            List<ChessMove> moves = new List<ChessMove>();
            ChessMove m = null;

            //*Check Left
            int i = 1;
            while ((loc.Y - i >= 0) && LocationEmpty(board, loc.X, loc.Y - i))
            {
                moves.Add(new ChessMove(loc, new ChessLocation(loc.X, loc.Y - i)));
                ++i;
            }

            if (loc.Y - i >= 0)
            {
                if (color != ColorOfPieceAt(new ChessLocation(loc.X, loc.Y - i), board))
                {
                    m = new ChessMove(loc, new ChessLocation(loc.X, loc.Y - i));
                    m.ValueOfMove = GetAttackMoveValue(VALUE_ROOK, new ChessLocation(loc.X, loc.Y - i), board);
                    moves.Add(m);
                }
            }

            //*Check Right
            i = 1;
            while ((loc.Y + i < COLS) && LocationEmpty(board, loc.X, loc.Y + i))
            {
                moves.Add(new ChessMove(loc, new ChessLocation(loc.X, loc.Y + i)));
                ++i;
            }
            if (loc.Y + i < COLS)
            {
                if (color != ColorOfPieceAt(new ChessLocation(loc.X, loc.Y + i), board))
                {
                    m = new ChessMove(loc, new ChessLocation(loc.X, loc.Y + i));
                    m.ValueOfMove = GetAttackMoveValue(VALUE_ROOK, new ChessLocation(loc.X, loc.Y + i), board);
                    moves.Add(m);
                }
            }

            //*Check Down
            i = 1;
            while ((loc.X - i >= 0) && LocationEmpty(board, loc.X - i, loc.Y))
            {
                moves.Add(new ChessMove(loc, new ChessLocation(loc.X - i, loc.Y)));
                ++i;
            }
            if (loc.X - i >= 0)
            {
                if (color != ColorOfPieceAt(new ChessLocation(loc.X - i, loc.Y), board))
                {
                    m = new ChessMove(loc, new ChessLocation(loc.X - i, loc.Y));
                    m.ValueOfMove = GetAttackMoveValue(VALUE_ROOK, new ChessLocation(loc.X - i, loc.Y), board);
                    moves.Add(m);
                }
            }

            //*Check Up
            i = 1;
            while ((loc.X + i < ROWS) && LocationEmpty(board, loc.X + i, loc.Y))
            {
                moves.Add(new ChessMove(loc, new ChessLocation(loc.X + i, loc.Y)));
                ++i;
            }
            if (loc.X + i < ROWS)
            {
                if (color != ColorOfPieceAt(new ChessLocation(loc.X + i, loc.Y), board))
                {
                    m = new ChessMove(loc, new ChessLocation(loc.X + i, loc.Y));
                    m.ValueOfMove = GetAttackMoveValue(VALUE_ROOK, new ChessLocation(loc.X + i, loc.Y), board);
                    moves.Add(m);
                }
            }

            return moves;
        } // End GetMovesForRook - JDB

        private List<ChessMove> GetMovesForBishop(ChessLocation loc, ChessBoard board, ChessColor color)
        {
            List<ChessMove> moves = new List<ChessMove>();
            ChessMove m = null;
            int i = 1;

            //*Check Up Left
            while ((loc.X - i >= 0) && (loc.Y - i >= 0) && LocationEmpty(board, loc.X - i, loc.Y - i))
            {
                moves.Add(new ChessMove(loc, new ChessLocation(loc.X - i, loc.Y - i)));
                ++i;
            }
            if ((loc.X - i >= 0) && (loc.Y - i >= 0))
            {
                if (color != ColorOfPieceAt(new ChessLocation(loc.X - i, loc.Y - i), board))
                {
                    m = new ChessMove(loc, new ChessLocation(loc.X - i, loc.Y - i));
                    m.ValueOfMove = GetAttackMoveValue(VALUE_BISHOP, new ChessLocation(loc.X - i, loc.Y - i), board);
                    moves.Add(m);
                }
            }

            //*Check Up Right
            i = 1;
            while ((loc.X + i < COLS) && (loc.Y - i >= 0) && LocationEmpty(board, loc.X + i, loc.Y - i))
            {
                moves.Add(new ChessMove(loc, new ChessLocation(loc.X + i, loc.Y - i)));
                ++i;
            }
            if ((loc.X + i < COLS) && (loc.Y - i >= 0))
            {
                if (color != ColorOfPieceAt(new ChessLocation(loc.X + i, loc.Y - i), board))
                {
                    m = new ChessMove(loc, new ChessLocation(loc.X + i, loc.Y - i));
                    m.ValueOfMove = GetAttackMoveValue(VALUE_BISHOP, new ChessLocation(loc.X + i, loc.Y - i), board);
                    moves.Add(m);
                }
            }

            //*Check Down Left
            i = 1;
            while ((loc.X - i >= 0) && (loc.Y + i < COLS) && LocationEmpty(board, loc.X - i, loc.Y + i))
            {
                moves.Add(new ChessMove(loc, new ChessLocation(loc.X - i, loc.Y + i)));
                ++i;
            }
            if ((loc.X - i >= 0) && (loc.Y + i < ROWS))
            {
                if (color != ColorOfPieceAt(new ChessLocation(loc.X - i, loc.Y + i), board))
                {
                    m = new ChessMove(loc, new ChessLocation(loc.X - i, loc.Y + i));
                    m.ValueOfMove = GetAttackMoveValue(VALUE_BISHOP, new ChessLocation(loc.X - i, loc.Y + i), board);
                    moves.Add(m);
                }
            }

            // Check Down Right
            i = 1;
            while ((loc.X + i < ROWS) && (loc.Y + i < COLS) && LocationEmpty(board, loc.X + i, loc.Y + i))
            {
                moves.Add(new ChessMove(loc, new ChessLocation(loc.X + i, loc.Y + i)));
                ++i;
            }
            if ((loc.X + i < COLS) && (loc.Y + i < ROWS))
            {
                if (color != ColorOfPieceAt(new ChessLocation(loc.X + i, loc.Y + i), board))
                {
                    m = new ChessMove(loc, new ChessLocation(loc.X + i, loc.Y + i));
                    m.ValueOfMove = GetAttackMoveValue(VALUE_BISHOP, new ChessLocation(loc.X + i, loc.Y + i), board);
                    moves.Add(m);
                }
            }

            return moves;
        } // GetMovesForBishop - JDB

        private List<ChessMove> GetMovesForQueen(ChessLocation loc, ChessBoard board, ChessColor color)
        {
            List<ChessMove> moves = new List<ChessMove>();
            ChessMove m = null;

            // Check up
            int i = 1;
            while ((loc.Y - i >= 0) && LocationEmpty(board, loc.X, loc.Y - i))
            {
                moves.Add(new ChessMove(loc, new ChessLocation(loc.X, loc.Y - i)));
                ++i;
            }
            if (loc.Y - i >= 0)
            {
                if (color != ColorOfPieceAt(new ChessLocation(loc.X, loc.Y - i), board))
                {
                    m = new ChessMove(loc, new ChessLocation(loc.X, loc.Y - i));
                    m.ValueOfMove = GetAttackMoveValue(VALUE_QUEEN, new ChessLocation(loc.X, loc.Y - i), board);
                    moves.Add(m);
                }
            }
            // Check down
            i = 1;
            while ((loc.Y + i < ROWS) && LocationEmpty(board, loc.X, loc.Y + i))
            {
                moves.Add(new ChessMove(loc, new ChessLocation(loc.X, loc.Y + i)));
                ++i;
            }
            if (loc.Y + i < ROWS)
            {
                if (color != ColorOfPieceAt(new ChessLocation(loc.X, loc.Y + i), board))
                {
                    m = new ChessMove(loc, new ChessLocation(loc.X, loc.Y + i));
                    m.ValueOfMove = GetAttackMoveValue(VALUE_QUEEN, new ChessLocation(loc.X, loc.Y + i), board);
                    moves.Add(m);
                }
            }
            // Check left
            i = 1;
            while ((loc.X - i >= 0) && LocationEmpty(board, loc.X - i, loc.Y))
            {
                moves.Add(new ChessMove(loc, new ChessLocation(loc.X - i, loc.Y)));
                ++i;
            }
            if (loc.X - i >= 0)
            {
                if (color != ColorOfPieceAt(new ChessLocation(loc.X - i, loc.Y), board))
                {
                    m = new ChessMove(loc, new ChessLocation(loc.X - i, loc.Y));
                    m.ValueOfMove = GetAttackMoveValue(VALUE_QUEEN, new ChessLocation(loc.X - i, loc.Y), board);
                    moves.Add(m);
                }
            }
            // Check right
            i = 1;
            while ((loc.X + i < COLS) && LocationEmpty(board, loc.X + i, loc.Y))
            {
                moves.Add(new ChessMove(loc, new ChessLocation(loc.X + i, loc.Y)));
                ++i;
            }
            if (loc.X + i < COLS && loc.X + i >= 0) //just threw in te loc.X + i >= 0 for fun I don't actually expect to need this check because i should always be greater than 0. 
            {
                if (color != ColorOfPieceAt(new ChessLocation(loc.X + i, loc.Y), board))
                {
                    m = new ChessMove(loc, new ChessLocation(loc.X + i, loc.Y));
                    m.ValueOfMove = GetAttackMoveValue(VALUE_QUEEN, new ChessLocation(loc.X + i, loc.Y), board);
                    moves.Add(m);
                }
            }
            // Check up-left
            i = 1;
            while ((loc.Y - i >= 0) && (loc.X - i >= 0) && LocationEmpty(board, loc.X - i, loc.Y - i))
            {
                moves.Add(new ChessMove(loc, new ChessLocation(loc.X - i, loc.Y - i)));
                ++i;
            }
            if ((loc.Y - i >= 0) && (loc.X - i >= 0))
            {
                if (color != ColorOfPieceAt(new ChessLocation(loc.X - i, loc.Y - i), board))
                {
                    m = new ChessMove(loc, new ChessLocation(loc.X - i, loc.Y - i));
                    m.ValueOfMove = GetAttackMoveValue(VALUE_QUEEN, new ChessLocation(loc.X - i, loc.Y - i), board);
                    moves.Add(m);
                }
            }
            // Check up-right
            i = 1;
            while ((loc.Y - i >= 0) && (loc.X + i < COLS) && LocationEmpty(board, loc.X + i, loc.Y - i))
            {
                moves.Add(new ChessMove(loc, new ChessLocation(loc.X + i, loc.Y - i)));
                ++i;
            }
            if ((loc.Y - i >= 0) && (loc.X + i < COLS))
            {
                if (color != ColorOfPieceAt(new ChessLocation(loc.X + i, loc.Y - i), board))
                {
                    m = new ChessMove(loc, new ChessLocation(loc.X + i, loc.Y - i));
                    m.ValueOfMove = GetAttackMoveValue(VALUE_QUEEN, new ChessLocation(loc.X + i, loc.Y - i), board);
                    moves.Add(m);
                }
            }
            // Check down-left
            i = 1;
            while ((loc.Y + i < ROWS) && (loc.X - i >= 0) && LocationEmpty(board, loc.X - i, loc.Y + i))
            {
                moves.Add(new ChessMove(loc, new ChessLocation(loc.X - i, loc.Y + i)));
                ++i;
            }
            if ((loc.Y + i < ROWS) && (loc.X - i >= 0))
            {
                if (color != ColorOfPieceAt(new ChessLocation(loc.X - i, loc.Y + i), board))
                {
                    m = new ChessMove(loc, new ChessLocation(loc.X - i, loc.Y + i));
                    m.ValueOfMove = GetAttackMoveValue(VALUE_QUEEN, new ChessLocation(loc.X - i, loc.Y + i), board);
                    moves.Add(m);
                }
            }
            // Check down-right
            i = 1;
            while ((loc.Y + i < ROWS) && (loc.X + i < COLS) && LocationEmpty(board, loc.X + i, loc.Y + i))
            {
                moves.Add(new ChessMove(loc, new ChessLocation(loc.X + i, loc.Y + i)));
                ++i;
            }
            if ((loc.Y + i < ROWS) && (loc.X + i < COLS))
            {
                if (color != ColorOfPieceAt(new ChessLocation(loc.X + i, loc.Y + i), board))
                {
                    m = new ChessMove(loc, new ChessLocation(loc.X + i, loc.Y + i));
                    m.ValueOfMove = GetAttackMoveValue(VALUE_QUEEN, new ChessLocation(loc.X + i, loc.Y + i), board);
                    moves.Add(m);
                }
            }

            return moves;
        } // End GetMovesForQueen - JDB

        private List<ChessMove> GetMovesForKnight(ChessLocation loc, ChessBoard board, ChessColor color)
        {
            List<ChessMove> moves = new List<ChessMove>();
            List<ChessLocation> moveLocations = new List<ChessLocation>();
            //get all possible locations that a knight can move. And store them in a list.
            moveLocations.Add(new ChessLocation(loc.X + 1, loc.Y + 2));
            moveLocations.Add(new ChessLocation(loc.X - 1, loc.Y + 2));
            moveLocations.Add(new ChessLocation(loc.X + 1, loc.Y - 2));
            moveLocations.Add(new ChessLocation(loc.X - 1, loc.Y - 2));
            moveLocations.Add(new ChessLocation(loc.X + 2, loc.Y + 1));
            moveLocations.Add(new ChessLocation(loc.X + 2, loc.Y - 1));
            moveLocations.Add(new ChessLocation(loc.X - 2, loc.Y + 1));
            moveLocations.Add(new ChessLocation(loc.X - 2, loc.Y - 1));
            foreach (ChessLocation moveLoc in moveLocations)
            {
                if (simpleValidateMove(moveLoc, board, color))
                {
                    ChessMove move = new ChessMove(loc, moveLoc);
                    if (ColorOfPieceAt(moveLoc, board) != color)
                    {
                        move.ValueOfMove = GetAttackMoveValue(VALUE_KNIGHT, moveLoc, board);
                    }
                    moves.Add(move);
                }
            }
            return moves;
        } //End GetMovesForKnight

        private List<ChessMove> GetMovesForKing(ChessLocation loc, ChessBoard board, ChessColor color)
        {
            List<ChessMove> moves = new List<ChessMove>();
            ChessMove m = null;
            //CheckMove LU
            ChessLocation nearbypiece = new ChessLocation(loc.X - 1, loc.Y + 1);
            m = CheckKingMove(loc, nearbypiece, board, color);
            if (!(m.From.X == m.To.X && m.From.Y == m.To.Y)) { moves.Add(m); }
            //CheckMove U
            nearbypiece = new ChessLocation(loc.X, loc.Y + 1);
            m = CheckKingMove(loc, nearbypiece, board, color);
            if (!(m.From.X == m.To.X && m.From.Y == m.To.Y)) { moves.Add(m); }
            //CheckMove RU
            nearbypiece = new ChessLocation(loc.X + 1, loc.Y + 1);
            m = CheckKingMove(loc, nearbypiece, board, color);
            if (!(m.From.X == m.To.X && m.From.Y == m.To.Y)) { moves.Add(m); }
            //CheckMove L
            nearbypiece = new ChessLocation(loc.X - 1, loc.Y);
            m = CheckKingMove(loc, nearbypiece, board, color);
            if (!(m.From.X == m.To.X && m.From.Y == m.To.Y)) { moves.Add(m); }
            //CheckMove R
            nearbypiece = new ChessLocation(loc.X + 1, loc.Y);
            m = CheckKingMove(loc, nearbypiece, board, color);
            if (!(m.From.X == m.To.X && m.From.Y == m.To.Y)) { moves.Add(m); }
            //CheckMove DL
            nearbypiece = new ChessLocation(loc.X - 1, loc.Y - 1);
            m = CheckKingMove(loc, nearbypiece, board, color);
            if (!(m.From.X == m.To.X && m.From.Y == m.To.Y)) { moves.Add(m); }
            //CheckMove D
            nearbypiece = new ChessLocation(loc.X, loc.Y - 1);
            m = CheckKingMove(loc, nearbypiece, board, color);
            if (!(m.From.X == m.To.X && m.From.Y == m.To.Y)) { moves.Add(m); }
            //CheckMove DL
            nearbypiece = new ChessLocation(loc.X + 1, loc.Y - 1);
            m = CheckKingMove(loc, nearbypiece, board, color);
            if (!(m.From.X == m.To.X && m.From.Y == m.To.Y)) { moves.Add(m); }

            return moves;
        }//End GetMovesForKing - HJW

        //Used by GetMovesForKing() if move is valid returns move, or returns move with location same as start
        private ChessMove CheckKingMove(ChessLocation fromloc, ChessLocation toloc, ChessBoard board, ChessColor color)
        {
            //check if move is out of bounds
            if ((toloc.X >= ROWS) || (toloc.X < 0) || (toloc.Y >= COLS) || (toloc.Y < 0))
            {
                ChessMove nomove = new ChessMove(fromloc, fromloc);
                return nomove;
            }
            else
            {
                if (LocationEmpty(board, toloc.X, toloc.Y))
                {
                    ChessMove validmove = new ChessMove(fromloc, new ChessLocation(toloc.X, toloc.Y));
                    //if(!MoveResultsInCheck(board, validmove, color))
                    //{
                    return validmove;
                    //}
                }
                else
                {
                    if (ColorOfPieceAt(new ChessLocation(toloc.X, toloc.Y), board) != color)
                    {
                        ChessMove attackingMoveKing = null;
                        attackingMoveKing = new ChessMove(fromloc, new ChessLocation(toloc.X, toloc.Y));
                        //if (!MoveResultsInCheck(board, attackingMoveKing, color))
                        //{
                        attackingMoveKing.ValueOfMove = GetAttackMoveValue(VALUE_KING, toloc, board);
                        return attackingMoveKing;
                        //}
                    }
                    ChessMove noMove = new ChessMove(fromloc, fromloc);
                    return noMove;
                }
                //return new ChessMove(fromloc, fromloc);
            }
        }//End CheckKingMove - HJW

        #endregion













        #region IChessAI Members that should be implemented as automatic properties and should NEVER be touched by students.
        /// <summary>
        /// This will return false when the framework starts running your AI. When the AI's time has run out,
        /// then this method will return true. Once this method returns true, your AI should return a 
        /// move immediately.
        /// 
        /// You should NEVER EVER set this property!
        /// This property should be defined as an Automatic Property.
        /// This property SHOULD NOT CONTAIN ANY CODE!!!
        /// </summary>
        public AIIsMyTurnOverCallback IsMyTurnOver { get; set; }

        /// <summary>
        /// Call this method to print out debug information. The framework subscribes to this event
        /// and will provide a log window for your debug messages.
        /// 
        /// You should NEVER EVER set this property!
        /// This property should be defined as an Automatic Property.
        /// This property SHOULD NOT CONTAIN ANY CODE!!!
        /// </summary>
        /// <param name="message"></param>
        public AILoggerCallback Log { get; set; }

        /// <summary>
        /// Call this method to catch profiling information. The framework subscribes to this event
        /// and will print out the profiling stats in your log window.
        /// 
        /// You should NEVER EVER set this property!
        /// This property should be defined as an Automatic Property.
        /// This property SHOULD NOT CONTAIN ANY CODE!!!
        /// </summary>
        /// <param name="key"></param>
        public AIProfiler Profiler { get; set; }

        /// <summary>
        /// Call this method to tell the framework what decision print out debug information. The framework subscribes to this event
        /// and will provide a debug window for your decision tree.
        /// 
        /// You should NEVER EVER set this property!
        /// This property should be defined as an Automatic Property.
        /// This property SHOULD NOT CONTAIN ANY CODE!!!
        /// </summary>
        /// <param name="message"></param>
        public AISetDecisionTreeCallback SetDecisionTree { get; set; }
        #endregion
    }
}
