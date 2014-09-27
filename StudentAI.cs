using System;
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
            get { return "Mean Green Chess Machine (DEBUG)"; }
#else
            get { return "Mean Green Chess Machine"; }
#endif
        }

        private const int ROWS = 8;
        private const int COLS = 8;

        private ChessMove GreedyMove(ChessBoard board, ChessColor myColor)
        {
            // All Greedy move will do is expand the current board, getting
            // successor moves, and will simply return the move that has the highest move value!

            List<ChessMove> possibleMoves = new List<ChessMove>();
            int bestMoveValue = 0;
            int bestMoveValueIndex = 0;

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

            for (int i = 0; i < possibleMoves.Count; ++i)
            {
                if (possibleMoves[i].ValueOfMove >= bestMoveValue)
                {
                    bestMoveValueIndex = i;
                    bestMoveValue = possibleMoves[i].ValueOfMove;
                }
            }

            this.Log("GreedyMove found " + possibleMoves.Count + " moves. Value of best is: " + bestMoveValue);

            return possibleMoves[bestMoveValueIndex];
        }

        private List<ChessBoard> Successors(ChessBoard board, ChessColor myColor, ref List<ChessMove> movesForEachSucc)
        {
            List<ChessBoard> succs = new List<ChessBoard>();

            this.Log("Accumulating successors");

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
                                ChessBoard child = new ChessBoard(board.RawBoard);
                                if (movesForEachSucc != null)
                                    movesForEachSucc.Add(validMovesForPiece[k]);
                                child.MakeMove(validMovesForPiece[k]);
                                succs.Add(child);
                            }
                        }
                    }
                }
            }

            // result is all board states from the parent state
            return succs;
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

        private const int VALUE_KING = 10;
        private const int VALUE_QUEEN = 8;
        private const int VALUE_ROOK = 5;
        private const int VALUE_KNIGHT = 4; //?
        private const int VALUE_BISHOP = 3;
        private const int VALUE_PAWN = 1;

        private int ValueOfBoard(ChessBoard board, ChessColor color)
        {
            // every board has a different state
            // Index 0 is pawns, then 1 is bishop, etc
            List<int> units = NumOfUnits(board, color);

            return VALUE_KING + VALUE_QUEEN * units[4] + VALUE_ROOK * units[3] + VALUE_KNIGHT * units[2] +
                VALUE_BISHOP * units[1] + VALUE_PAWN * units[0];
        }

        private List<int> NumOfUnits(ChessBoard board, ChessColor color)
        {
            List<int> unitCounts = new List<int>(5);
            for (int i = 0; i < 5; ++i)
            {
                unitCounts[i] = 0;
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

        // All moves that can be done by units in the game
        private List<ChessMove> GetMovesForPawn(ChessLocation loc, ChessBoard board, ChessColor color)
        {
            List<ChessMove> moves = new List<ChessMove>();

            if (ChessColor.Black == color)
            {
                // we are approaching from low values of Y to higher ones
                if (PawnHasNotMoved(loc, ChessColor.Black))
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
                if (PawnHasNotMoved(loc, ChessColor.White))
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

        // Determines if a pawn at location loc, black or white, has made its first move.
        // Used by GetMovesForPawn.
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
        }

        private List<ChessMove> GetMovesForRook(ChessLocation loc, ChessBoard board, ChessColor color)
        {
            return new List<ChessMove>(0);
        }

        private List<ChessMove> GetMovesForKnight(ChessLocation loc, ChessBoard board, ChessColor color)
        {
            return new List<ChessMove>(0);
        }

        private List<ChessMove> GetMovesForBishop(ChessLocation loc, ChessBoard board, ChessColor color)
        {
            return new List<ChessMove>(0);
        }

        private List<ChessMove> GetMovesForQueen(ChessLocation loc, ChessBoard board, ChessColor color)
        {
            return new List<ChessMove>(0);
        }

        private List<ChessMove> GetMovesForKing(ChessLocation loc, ChessBoard board, ChessColor color)
        {
            return new List<ChessMove>(0);
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
            ChessMove myNextMove = null;

            while (!IsMyTurnOver())
            {
                if (myNextMove == null)
                {
                    // Greedy move, or whatever generates a move, needs to run on a timer eventually
                    myNextMove = GreedyMove(board, myColor);
                    if (!IsValidMove(board, myNextMove, myColor))
                        this.Log("GreedyMove generated an illegal move");

                    this.Log(myColor.ToString() + " (" + this.Name + ") just moved.");
                    this.Log(string.Empty);

                    // Since I have a move, break out of loop
                    break;
                }
            }

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
        {   /*
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
            return true;
        }

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
