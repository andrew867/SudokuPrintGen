using System.Text;

namespace SudokuPrintGen.Core.Puzzle;

/// <summary>
/// Represents a Sudoku board with configurable size.
/// </summary>
public class Board
{
    private readonly int[,] _cells;
    private readonly int _size;
    private readonly int _boxRows;
    private readonly int _boxCols;
    
    /// <summary>
    /// Gets the board size (e.g., 9 for a 9x9 board).
    /// </summary>
    public int Size => _size;
    
    /// <summary>
    /// Gets the number of rows in each box.
    /// </summary>
    public int BoxRows => _boxRows;
    
    /// <summary>
    /// Gets the number of columns in each box.
    /// </summary>
    public int BoxCols => _boxCols;
    
    /// <summary>
    /// Gets or sets the value at the specified position.
    /// </summary>
    public int this[int row, int col]
    {
        get => _cells[row, col];
        set => _cells[row, col] = value;
    }
    
    /// <summary>
    /// Initializes a new instance of the Board class.
    /// </summary>
    public Board(int size, int boxRows, int boxCols)
    {
        if (size <= 0)
            throw new ArgumentException("Size must be positive", nameof(size));
        if (boxRows <= 0 || boxCols <= 0)
            throw new ArgumentException("Box dimensions must be positive", nameof(boxRows));
        if (boxRows * boxCols != size)
            throw new ArgumentException("Box rows * box cols must equal size", nameof(size));
        
        _size = size;
        _boxRows = boxRows;
        _boxCols = boxCols;
        _cells = new int[size, size];
    }
    
    /// <summary>
    /// Creates a standard 9x9 board.
    /// </summary>
    public static Board CreateStandard()
    {
        return new Board(9, 3, 3);
    }
    
    /// <summary>
    /// Creates a board from a string representation (81 characters for 9x9, '.' or '0' for empty).
    /// </summary>
    public static Board FromString(string puzzle, int size = 9, int boxRows = 3, int boxCols = 3)
    {
        var board = new Board(size, boxRows, boxCols);
        var index = 0;
        
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (index < puzzle.Length)
                {
                    var ch = puzzle[index];
                    if (ch >= '1' && ch <= '9')
                    {
                        board[row, col] = ch - '0';
                    }
                    else if (ch == '.' || ch == '0')
                    {
                        board[row, col] = 0;
                    }
                }
                index++;
            }
        }
        
        return board;
    }
    
    /// <summary>
    /// Converts the board to a string representation.
    /// </summary>
    public string ToString(bool useDots = true)
    {
        var sb = new StringBuilder(_size * _size);
        
        for (int row = 0; row < _size; row++)
        {
            for (int col = 0; col < _size; col++)
            {
                var value = _cells[row, col];
                if (value == 0)
                {
                    sb.Append(useDots ? '.' : '0');
                }
                else
                {
                    sb.Append(value);
                }
            }
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Creates a copy of this board.
    /// </summary>
    public Board Clone()
    {
        var clone = new Board(_size, _boxRows, _boxCols);
        for (int row = 0; row < _size; row++)
        {
            for (int col = 0; col < _size; col++)
            {
                clone[row, col] = _cells[row, col];
            }
        }
        return clone;
    }
    
    /// <summary>
    /// Gets the box index for a given row and column.
    /// </summary>
    public int GetBoxIndex(int row, int col)
    {
        return (row / _boxRows) * (_size / _boxCols) + (col / _boxCols);
    }
    
    /// <summary>
    /// Gets all cells in a specific box.
    /// </summary>
    public IEnumerable<(int row, int col, int value)> GetBoxCells(int boxIndex)
    {
        var boxesPerRow = _size / _boxCols;
        var boxRow = boxIndex / boxesPerRow;
        var boxCol = boxIndex % boxesPerRow;
        var startRow = boxRow * _boxRows;
        var startCol = boxCol * _boxCols;
        
        for (int r = 0; r < _boxRows; r++)
        {
            for (int c = 0; c < _boxCols; c++)
            {
                var row = startRow + r;
                var col = startCol + c;
                yield return (row, col, _cells[row, col]);
            }
        }
    }
    
    /// <summary>
    /// Checks if the board is completely filled.
    /// </summary>
    public bool IsComplete()
    {
        for (int row = 0; row < _size; row++)
        {
            for (int col = 0; col < _size; col++)
            {
                if (_cells[row, col] == 0)
                    return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Gets the number of filled cells.
    /// </summary>
    public int GetClueCount()
    {
        int count = 0;
        for (int row = 0; row < _size; row++)
        {
            for (int col = 0; col < _size; col++)
            {
                if (_cells[row, col] != 0)
                    count++;
            }
        }
        return count;
    }
}

