namespace Hive
{
    public class Board
    {
        public Dictionary<Cell, Tile> tiles = new Dictionary<Cell, Tile>();
        public List<Cell> filteredPiecesInPlay { get { return tiles.Where(kvp => kvp.Value.isOccupied).ToList().Select(KeyValuePair => KeyValuePair.Key).ToList(); } }
        public Board()
        {
            foreach (Cell cell in HexUtils.HexGen(36, 24, HexOrientation.PointyTopped))
            {
                tiles.Add(cell, new Tile(cell));
            }
        }
        public void movePiece(Cell origin, Piece piece)
        {
            tiles[origin].removePiece();
            tiles[piece.location].addPiece(piece);
        }
        public void placePiece(Piece pieceToPlace) => tiles[pieceToPlace.location].addPiece(pieceToPlace);
     
        #region checkers
        public bool AreCellsAdjacent(Cell a, Cell B) => HiveUtils.getNeighbors(a).Contains(B);
        public bool CanMoveAboveHive(Cell a, Cell b)
        {
            List<Cell> adjacents = connectingAdjacents(a, b);
            return !adjacents.All(cell => tileIsOccupied(cell) && tiles[cell].hasBlockedPiece);
        }
        public bool CanMoveBetween(Cell a, Cell b) => !connectingAdjacents(a, b).All(cell => tileIsOccupied(cell));
        public bool tileIsOccupied(Cell cell) => tiles.ContainsKey(cell) && tiles[cell].isOccupied;
        #endregion
        
        #region distillation
        public List<Cell> getEmptyNeighbors(Cell cell) => getNeighbors(cell).Where(x => !tileIsOccupied(x)).ToList();
        public List<Cell> getOccupiedNeighbors(Cell cell) => getNeighbors(cell).Where(x => tileIsOccupied(x)).ToList();
        public List<Cell> getNeighbors(Cell origin) => HiveUtils.getNeighbors(origin).ToList();
        public List<Cell> adjacentLegalCells(Cell cell)
        {
            List<Cell> empty = getEmptyNeighbors(cell);
            List<Cell> neighbors = getOccupiedNeighbors(cell);
            HashSet<Cell> neighbor_adjacent = new HashSet<Cell>();
            foreach (Cell neighbor in neighbors)
            {
                List<Cell> neighborAdjacentEmpties = getEmptyNeighbors(neighbor);
                neighborAdjacentEmpties.ForEach(temp_tile => neighbor_adjacent.Add(temp_tile));
            }
            var prelim = empty.Intersect(neighbor_adjacent).ToList();
            return prelim.ToList();
        }
        public List<Cell> connectingAdjacents(Cell a, Cell b)
        {
            //this is for the freedom to move rule
            /*
             *returns {xy}

               /  \
              |  y |
             / \  /  \
            | a ||  b |
             \ /  \  /
               | x |     
             `  \ /
             */
            List<Cell> aNeighbors = HiveUtils.getNeighbors(a);
            List<Cell> bNeighbors = HiveUtils.getNeighbors(b);
            List<Cell> union = aNeighbors.Intersect(bNeighbors).ToList();
            if (union.Count > 0 && union.Count == 2) return union;
            else throw new Exception("connectingAdjacents fucked up somewhere!");
        }
        #endregion

        //SEND TO SPIDER/ANT RESPECTIVELY
        public List<Cell> hypotheticalAdjacentLegalCells(Cell cell, Cell exclude)
        {
            List<Cell> empty = getEmptyNeighbors(cell);
            List<Cell> neighbors = getOccupiedNeighbors(cell);
            neighbors.Remove(exclude);
            HashSet<Cell> neighbor_adjacent = new HashSet<Cell>();
            foreach (Cell neighbor in neighbors)
            {
                List<Cell> neighborAdjacentEmpties = getEmptyNeighbors(neighbor);
                neighborAdjacentEmpties.ForEach(temp_tile => neighbor_adjacent.Add(temp_tile));
            }
            var prelim = empty.Intersect(neighbor_adjacent).Where(next => hypotheticallCanMoveBetween(cell, next, exclude)).ToList();
            return prelim.ToList();
        }
        //FIXME WONTFIX this is awful
        public List<Cell> hypotheticalAdjacentLegalCellsForAnts(Cell cell, List<Cell> exclude)
        {
            List<Cell> empty = getEmptyNeighbors(cell);
            List<Cell> neighbors = getOccupiedNeighbors(cell);
            foreach (Cell toExclude in exclude)
            {
                neighbors.Remove(toExclude);
            }
            HashSet<Cell> neighbor_adjacent = new HashSet<Cell>();
            foreach (Cell neighbor in neighbors)
            {
                List<Cell> neighborAdjacentEmpties = getEmptyNeighbors(neighbor);
                neighborAdjacentEmpties.ForEach(temp_tile => neighbor_adjacent.Add(temp_tile));
            }
            var prelim = empty.Intersect(neighbor_adjacent).Where(next => hypotheticallCanMoveBetweenForAnts(cell, next, exclude)).ToList();
            return prelim.ToList();
        }

        public bool hypotheticallCanMoveBetween(Cell a, Cell b, Cell exclude)
        {
            List<Cell> adjacents = connectingAdjacents(a, b);
            if (adjacents.Contains(exclude)) return true;
            else return !adjacents.All(cell => tileIsOccupied(cell));
        }
        public bool hypotheticallCanMoveBetweenForAnts(Cell a, Cell b, List<Cell> exclude)
        {
            List<Cell> adjacents = connectingAdjacents(a, b);
            if (adjacents.Intersect(exclude).Count() > 0) return true;
            else return !adjacents.All(cell => tileIsOccupied(cell));
        }

    }
}
