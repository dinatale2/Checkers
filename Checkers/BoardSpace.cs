using System.Drawing;
using System.Windows.Forms;

namespace Checkers
{
	public enum CanKing
	{
		No, YesRed, YesBlack,
	};

	public partial class BoardSpace
	{
		// the spaces background color
		private Color m_background;

		// the occupying piece?
		private CheckerMarker m_occupyingpiece;
		public CheckerMarker OccupyingPiece
		{
			get { return m_occupyingpiece; }
		}

		// is there a king here?
		public bool HoldingKing
		{ get { return m_occupyingpiece.IsKing; } }

		// what can it king
		private CanKing m_ableToKing;
		public CanKing AbleToKing
		{ get { return m_ableToKing; } }

		// is it selected?
		private bool m_isSelected;
		public bool IsSelected
		{
			get { return m_isSelected; }
			set { m_isSelected = value; }
		}

		// is this space selectable?
		private bool m_selectable;
		public bool Selectable
		{
			get { return m_selectable; }
		}

		// owner of the space
		public Player Owner
		{
			get { return GetOwner(); }
		}

		// spaces row
		private int m_row;
		public int Row
		{
			get { return m_row; }
		}

		// spaces column
		private int m_col;
		public int Col
		{
			get { return m_col; }
		}

		public bool IsEmpty
		{
			// return if this piece is empty
			get { return Empty(); }
		}

		public BoardSpace(int row, int col, Color c, Player owner, bool Selectable)
		{
			//InitializeComponent();

			// dont erase the form to reduce flicker, have the form dictate when it should be painted, and doublebuffer it for less flicker
			//this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

			// set appropriate fields
			m_row = row;
			m_col = col;
			m_background = c;
			IsSelected = false;
			m_selectable = Selectable;
			m_ableToKing = CanKing.No;

			// Create the piece that belongs here
			PlaceMarker(owner);
		}

		public void IsAbleToKing(CanKing ck)
		{
			if (m_selectable)
				m_ableToKing = ck;
		}

		public void Highlight(bool isSelected)
		{
			// if this piece is selectable then set it as such
			if (m_selectable)
				this.m_isSelected = isSelected;

			// refresh the board because we need to redraw
			//this.Refresh();
		}

		private Player GetOwner()
		{
			// if there is an occupying piece, then get its owner
			// otherwise it has no owner
			if (m_occupyingpiece != null)
			{
				return m_occupyingpiece.Owner;
			}
			else
				return Player.None;
		}

		public void PlaceMarker(Player owner)
		{
			// Create the piece that belongs here
			if (owner == Player.Black)
				// if the owner of this piece is the black player, then put a black piece
				m_occupyingpiece = new CheckerMarker(Player.Black);
			else
				if (owner == Player.Red)
					// or if the owner is the red player, then make a red piece
					m_occupyingpiece = new CheckerMarker(Player.Red);
				else
					// otherwise, there is no valid owner, therefore no piece
					m_occupyingpiece = null;
		}

		private bool Empty()
		{
			// if the occupying piece is null, then this space is empty
			// otherwise its not
			if (m_occupyingpiece == null)
				return true;
			else
				return false;
		}

		public void RemovePiece()
		{
			// set the occupying piece to null so garbage collection
			// takes care of it
			m_occupyingpiece = null;
		}

		public void JumpSpace()
		{
			// Jumping a space is just like removing the piece,
			// so remove it
			RemovePiece();
		}

		public void KingContainedPiece()
		{
			// if there is a piece in this space
			// king it
			if (m_occupyingpiece != null)
				m_occupyingpiece.KingMe();

			// refresh the board so it draws
			//this.Refresh();
		}

		public void Draw(PaintEventArgs e, int x, int y, int width, int height)
		{
			// get the graphics object
			Graphics g = e.Graphics;

			// make it draw smooth edges
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			// dynamically change the thickness of the lines drawn at the edge of each piece
			int thickness = (width + height) / 75;

			Point top_left = new Point(x, y);
			Point top_right = new Point(x + width - thickness, y);
			Point bottom_left = new Point(x, y + height - thickness);
			Point bottom_right = new Point(x + width - thickness, y + height - thickness);

			// if the current space is selected, use brighter colors to make it stand out
			if (m_isSelected)
			{
				Brush b = new SolidBrush(ColorFunctions.AdjustBrightness(m_background, 1.25));
				g.FillRectangle(b, x, y, width, height);
			}
			// otherwise use the normal background color
			else
			{
				Brush b = new SolidBrush(m_background);
				g.FillRectangle(b, x, y, width, height);
			}

			// get a pen in a brighter shade of the background color and 
			// highlight the top and left edges of the boardspace
			Pen p = new Pen(ColorFunctions.AdjustBrightness(m_background, 1.5), thickness);
			e.Graphics.DrawLine(p, bottom_left, top_left);
			e.Graphics.DrawLine(p, top_left, top_right);

			// go ahead and adjust the color to a darker shade of the background color
			// and have the pen draw lowlights on the bottom and right edge of the space
			p.Color = ColorFunctions.AdjustBrightness(m_background, 0.6);
			e.Graphics.DrawLine(p, bottom_left, bottom_right);
			e.Graphics.DrawLine(p, bottom_right, top_right);

			// if there is a piece in this space, tell it to draw
			if (!this.Empty())
			{
				// calculate the pieces location and size
				int p_height = (2 * height) / 3;
				int p_width = (2 * width) / 3;
				int p_x = x + width / 6;
				int p_y = y + height / 6;

				// draw the piece
				m_occupyingpiece.Draw(e, p_height, p_width, p_x, p_y);
			}
		}

		public void SwapPieces(BoardSpace b)
		{
			// store the current occupying piece
			CheckerMarker temp = b.OccupyingPiece;

			// set the other board spaces piece to this ones
			b.SetPiece(this.m_occupyingpiece);

			// set this space's occupying piece to the one
			// the other board space contained
			m_occupyingpiece = temp;

			// if this space can king red pieces and the new piece is a red piece, then king the piece
			if (!this.Empty() && this.m_ableToKing == CanKing.YesRed && this.m_occupyingpiece.Owner == Player.Red)
				this.OccupyingPiece.KingMe();
			else
				// or if this space can king a black piece and it has a black piece, king that piece
				if (!this.Empty() && this.m_ableToKing == CanKing.YesBlack && this.m_occupyingpiece.Owner == Player.Black)
					this.OccupyingPiece.KingMe();
				else
					// of if the other space can king black pieces and it has a black piece, king the other space's piece
					if (!b.Empty() && b.AbleToKing == CanKing.YesBlack && b.OccupyingPiece.Owner == Player.Black)
						b.OccupyingPiece.KingMe();
					else
						// or if the other space has a red piece and it can king red pieces, king the other space's piece
						if (!b.Empty() && b.AbleToKing == CanKing.YesRed && b.OccupyingPiece.Owner == Player.Red)
							b.OccupyingPiece.KingMe();
		}

		public void SetPiece(CheckerMarker c)
		{
			// set this space's occupying piece as
			// the one passed in
			this.m_occupyingpiece = c;
		}
	}
}
