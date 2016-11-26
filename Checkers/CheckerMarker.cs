using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace Checkers
{    
  public class CheckerMarker : GamePiece
  {
    private Player owner;     // who owns this piece?
    public Player Owner
    {
      get { return owner; }
      set { owner = value; }
    }    // accesser for the owner

    private Color m_ColorofPiece; // primary color of piece

    private bool isKing;          // is this piece a king?
    public bool IsKing
    {
      get { return isKing; }
      set { isKing = value; }
    }         // accessor for if this piece is a king

    public CheckerMarker(Player owned_by)
      : base()
    {
      // assign the owner of this piece
      owner = owned_by;

      // assign the primary color for this piece
      if (owned_by == Player.Black)
        m_ColorofPiece = Color.DarkGray;
      else
        m_ColorofPiece = Color.Red;

      // this piece is new, it cant be a king yet
      isKing = false;
    }

    override public void Draw(PaintEventArgs e, int height, int width, int x, int y)
    {
      if (e.ClipRectangle.Contains(x, y))
      {
        Graphics g = e.Graphics;

        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // make a bounding rectangle for the piece
        Rectangle r = new Rectangle(x, y, width, height);
        // make a brush to draw a filled ellipse
        Brush b1 = new SolidBrush(m_ColorofPiece);


        // draw the ellipse that represents the piece
        g.FillEllipse(b1, r);

        // Dynamically calculate a thickness based on the size of the board
        float thickness = ((float)width + (float)height) / 50;

        // Create a pen of that thickness with adjust color and draw the ellipse
        Pen p = new Pen(ColorFunctions.AdjustBrightness(m_ColorofPiece, .5), thickness * 2);
        g.DrawEllipse(p, r);

        p = new Pen(ColorFunctions.AdjustBrightness(m_ColorofPiece, 1.35), thickness);
        g.DrawEllipse(p, r);

        // if the piece is a king
        if (isKing)
        {
          Brush b2 = new SolidBrush(ColorFunctions.AdjustBrightness(m_ColorofPiece, .5));
          // position the crown one fouth the width to the right
          int x_crown = x + (width / 4);

          // position the crown one third the way down the height
          int y_top_crown = y + (height / 3);

          // position the base of the crown one third the way down of the position of the whole crown
          int y_base_crown = y_top_crown + (height / 9);

          // crown width and height
          int crown_width = width / 2;
          int crown_base_height = (2 * height) / 9;
          int crown_top_height = height / 9;

          // draw the base of the crown
          g.FillRectangle(b2, x_crown, y_base_crown, crown_width, crown_base_height);

          // put the spikes on top of the crown
          g.FillRectangle(b2, x_crown, y_top_crown, crown_width / 6, crown_top_height);
          g.FillRectangle(b2, x_crown + ((float)crown_width / 6) * 2, y_top_crown, crown_width / 3, crown_top_height);
          g.FillRectangle(b2, x_crown + ((float)crown_width / 6) * 5, y_top_crown, crown_width / 6, crown_top_height);
        }
      }
    }

    public void KingMe()
    {
      // piece is now a king
      isKing = true;
    }
  }
}
