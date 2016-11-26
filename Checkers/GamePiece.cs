using System.Windows.Forms;

namespace Checkers
{
  public abstract class GamePiece
  {
    // base constructor of a game piece
    public GamePiece()
    {
    }

    // all game pieces must be able to draw themselves
    public abstract void Draw(PaintEventArgs e, int height, int width, int x, int y);
  }
}
