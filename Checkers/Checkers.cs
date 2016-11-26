using System;
using System.Windows.Forms;

namespace Checkers
{
  public partial class Checkers : Form
  {
    public Checkers()
    {
      InitializeComponent();
      // once we start, call a restart with false to say initialize the board
      gameBoard1.RestartGame(false);
    }

    private void restartToolStripMenuItem_Click(object sender, EventArgs e)
    {
      gameBoard1.RestartGame(true);
    }

    private void exitToolStripMenuItem_Click(object sender, EventArgs e)
    {
      this.Close();
    }
  }
}
