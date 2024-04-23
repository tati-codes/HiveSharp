using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace Hive
{
    public class Path
    {
        public List<Cell> steps;
        public Cell last
        {
            get
            {
                if (steps.Count < 1) throw new ArgumentOutOfRangeException("path class");
                return steps[steps.Count - 1];
            }
        }
        public Cell penult
        {
            get
            {
                if (steps.Count < 2) throw new ArgumentOutOfRangeException("path class");
                return steps[steps.Count - 2];
            }
        }
        public bool isSingleStep { get => steps.Count == 1; }
        public List<(Cell first, Cell last)> pairs
        {
            get
            {
                if (isSingleStep) { return new List<(Cell first, Cell last)>(); }
                List<(Cell first, Cell last)> result = new List<(Cell first, Cell last)>();
                for (int i = 0; i < steps.Count - 2; i++)
                {
                    //int next = i + 1 > steps.Count - 1 ? 0 : i + 1;
                    result.Add((steps[i], steps[i + 1]));
                }
                return result;
            }
        }
        public Pieces pathType;
        public Path(List<Cell> steps, Pieces type)
        {
            this.steps = steps;
            this.pathType = type;
        }
        public bool isNullPath { get => steps.Count == 0; }
        public Path(Cell step, Pieces type)
        {
            this.steps = new List<Cell>() { step };
            this.pathType = type;
        }
        public Path(params Cell[] step)
        {
            this.steps = step.ToList();
        }
    }
    public class Tile
    {

        public Cell cell;
        public List<Piece> pieces = new List<Piece>();
        public Piece activePiece
        {
            get
            {
                if (pieces.Count > 0) return pieces[0];
                else return null;
            }
        }
        public int zIndex => isOccupied ? pieces.Count : 0;
        public bool hasBlockedPiece => pieces.Count > 1;
        public bool isOccupied => pieces.Count > 0;
        public Tile(Cell cell)
        {
            this.cell = cell;
        }
        public void addPiece(Piece piece) { pieces.Insert(0, piece); }
        public Piece removePiece()
        {
            Piece returnable = pieces[0];
            pieces.RemoveAt(0);
            return returnable;
        }
    }
    public partial class Hive
    {
        public Phases game_status = Phases.PREPARED;
        public Players turn = Players.WHITE;
        public List<Move> moves = new List<Move>();
        public Dictionary<Players, Player> players = new Dictionary<Players, Player>
        {
            [Players.WHITE] = new Player(Players.WHITE),
            [Players.BLACK] = new Player(Players.BLACK),
        };
        public Board board = new Board();
        public Players? winner = null;

        //when the constructor is over
        public delegate void gameIsReadyEventHandler();
        public event gameIsReadyEventHandler gameIsReady;
        //when a move is accepted
        public delegate void onSuccessfulMoveEventHandler(Move move);
        public event onSuccessfulMoveEventHandler onSuccessfulMove;
        //when a move is rejected
        public delegate void onFailedMoveEventHandler(string error);
        public event onFailedMoveEventHandler onFailedMove;
        //when the game is done
        public delegate void onGameOverEventHandler();
        public event onGameOverEventHandler onGameOver;

        public Hive()
        {
        }

        public void send_move(Move move)
        {
            if (!moveIsValid(move) || game_status == Phases.GAME_OVER)
            {
                onFailedMove?.Invoke("Invalid Move");
                return;
            }
            switch (move.type)
            {
                case MoveType.INITIAL_PLACE:
                    this.initialPlace((INITIAL_PLACE)move);
                    break;
                case MoveType.MOVE_PIECE:
                    this.move((MOVE_PIECE)move);
                    break;
                case MoveType.PLACE:
                    this.place((PLACE)move);
                    break;
                case MoveType.AUTOPASS:
                    break;
            }
            moves.Add(move);
            onSuccessfulMove?.Invoke(move);
            if (wincon_check()) game_over();
            else advanceTurn();
        }

        public void advanceTurn()
        {

            turn = ((Players)((int)turn ^ 1));
            autopassCheck();
        }

        private void game_over()
        {
            game_status = Phases.GAME_OVER;
            List<KeyValuePair<Cell, Tile>> bees = board.tiles.Where(kvp => kvp.Value.isOccupied && kvp.Value.pieces.Any(pie => pie.type == Pieces.BEE)).ToList();
            if (bees.All(kvp => board.getOccupiedNeighbors(kvp.Key).Count == 6))
            {
            }
            else
            {
                var color = bees.Where(kvp => board.getOccupiedNeighbors(kvp.Key).Count != 6).ToList();
                winner = color[0].Value.pieces.Find(piece => piece.type == Pieces.BEE)?.owner;
            }
            onGameOver?.Invoke();
        }
        private void place(PLACE move)
        {
            Player pieceOwner = players[move.player];
            var id = pieceOwner.piecePlaced(move.piece);
            Piece newPiece = Piece.create(move.piece, move.player, (move.destination), id);
            board.placePiece(newPiece);
        }
        private void move(MOVE_PIECE move)
        {
            Piece originalPiece = board.tiles[move.origin].activePiece;
            if ((board.tiles[move.origin].isOccupied && !board.tiles[move.destination].isOccupied) ||
                (board.tiles[move.destination].isOccupied && (move.piece == Pieces.BEETLE || move.piece == Pieces.MOSQUITO))
                )
            {
                Piece newPiece = Piece.create(originalPiece.type, originalPiece.owner, move.destination, originalPiece.id);
                board.movePiece(move.origin, newPiece);
            }
            else
            {
                throw new Exception("Move passed checks but failed to play...?");
            }
        }
        private void initialPlace(INITIAL_PLACE move) => place(new PLACE(move.player, move.piece, move.destination));
        #region rule checkers
        //freedom to move is on boardnode
        #endregion
    }
}
