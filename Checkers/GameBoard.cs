using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Checkers
{
	// arguments to be passed when the event is raised
	public class BoardArgs : EventArgs
	{
		// from what row
		private int m_FromRow;
		public int FromRow
		{
			get { return m_FromRow; }
		}

		// from what column
		private int m_FromCol;
		public int FromCol
		{
			get { return m_FromCol; }
		}

		// to what row
		private int m_ToRow;
		public int ToRow
		{
			get { return m_ToRow; }
		}

		// to what column
		private int m_ToCol;
		public int ToCol
		{
			get { return m_ToCol; }
		}

		// whose turn was it
		private Player m_CurrPlayerTurn;
		public Player CurrentPlayer
		{
			get { return m_CurrPlayerTurn; }
		}

		public BoardArgs(int from_row, int from_col, int to_row, int to_col, Player p_turn)
		{
			m_FromRow = from_row;
			m_FromCol = from_col;
			m_ToRow = to_row;
			m_ToCol = to_col;
			m_CurrPlayerTurn = p_turn;
		}
	}

	// enumeration for the player who currently has access to the gameboard
	public enum Player
	{
		Red, Black, None,
	};

	public class GameBoard : Panel
	{
		//Event handling for move made
		public delegate void MoveMade(object sender, BoardArgs e);
		public event MoveMade MoveCompleted;

		// struct to store what resembles a valid move for the current selection.
		private struct ValidMove
		{
			// the move the user would be allowed to move to
			private BoardSpace moveTo;
			public BoardSpace MoveTo
			{
				get { return moveTo; }
				set { moveTo = value; }
			}

			// the board space that would be jumped (if any) to arrive at the destination
			private BoardSpace toBeJumped;
			public BoardSpace ToBeJumped
			{
				get { return toBeJumped; }
			}

			public ValidMove(BoardSpace moveto, BoardSpace Jumped)
			{
				moveTo = moveto;
				toBeJumped = Jumped;
			}
		}

		// struct to store the differences in regard to piece movement.  Could use the point struct but the name makes little sense when
		// reading the code
		private struct MoveDiffs
		{
			// difference in row and col
			private int delta_row;
			private int delta_col;

			public int DeltaRow
			{ get { return delta_row; } }

			public int DeltaCol
			{ get { return delta_col; } }

			public MoveDiffs(int delta_row, int delta_col)
			{
				this.delta_row = delta_row;
				this.delta_col = delta_col;
			}
		}

		// whose turn is it?
		private Player m_CurrPlayerTurn;
		public Player CurrentPlayer
		{
			get { return m_CurrPlayerTurn; }
		}

		private List<ValidMove> CurrentValidMoves;  // the valid moves for the current selection
		private BoardSpace m_mouseDownIn = null;  // space mouse was down in

		// the base differences sorted in such a way so that the red piece diffs are in front and the gray/black diffs are at the end
		private static MoveDiffs[] Differences = { new MoveDiffs(1, 1), new MoveDiffs(1, -1), new MoveDiffs(-1, -1), new MoveDiffs(-1, 1) };

		private BoardSpace[,] m_Spaces; // collection of spaces
		private bool m_isValid;       // determines if the board is allowed to be drawn
		private int m_sel_row;        // row of curr selected piece
		private int m_sel_col;        // col of curr selected piece
		private bool m_JumpsExist;    // Do Jumps currently exist?
		private bool m_DblJumpExist;  // Does a double jump exist?
		private int m_RedPieceCount;  // Number of red pieces on the board
		private int m_BlkPieceCount;  // Number of black pieces on the board

		public GameBoard()
			: base()
		{
			// double buffer the game board to prevent the control from flickering on resize
			this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

			// go ahead and initialize an 8 by 8 array of board spaces
			m_Spaces = new BoardSpace[8, 8];

			// at the present time, the board is uninitalized and its not valid, the paint event cant interact with it
			m_isValid = false;

			// set the current selection to nothing
			m_sel_col = -1;
			m_sel_row = -1;
		}

		public void RestartGame(bool isRestart)
		{
			// board is currently invalid
			m_isValid = false;

			// if the game is not a restart but a fresh instance of the game...
			if (!isRestart)
			{
				// calculate the base increment for the position based on the current size
				float base_x = (float)base.Width / 8;
				float base_y = (float)base.Height / 8;

				// for every space on the board, initalize the array of board spaces
				for (int i = 0; i < 8; i++)
				{
					for (int j = 0; j < 8; j++)
					{
						// the decider helps determine what selection to make for each space
						int decider = (i + j) % 2;

						// initialize the board with its row, col, color, owner, and if its selectable
						m_Spaces[i, j] = new BoardSpace(i, j, decider == 1 ? Color.DarkGray : Color.Tan, Player.None, (decider == 0));

						// if the current row is the first row, it can king red pieces
						if (i == 0)
							m_Spaces[i, j].IsAbleToKing(CanKing.YesRed);

						// if the current row is the last row, it can king the black/gray pieces
						if (i == 7)
							m_Spaces[i, j].IsAbleToKing(CanKing.YesBlack);
					}
				}
			}

			// need new list of boardspaces for valid moves
			CurrentValidMoves = new List<ValidMove>();

			// black goes first so the current player will be black, "Smoke kills before fire"
			m_CurrPlayerTurn = Player.Black;

			// theres no way jumps can exist, the board has been reset
			m_JumpsExist = false;
			m_DblJumpExist = false;

			// each player has 12 pieces
			m_BlkPieceCount = 12;
			m_RedPieceCount = 12;

			// since all spaces are now initialized in the array, the board is now valid and no null references exist
			m_isValid = true;

			// for each row on the board
			for (int i = 0; i < 8; i++)
			{
				// assume black owns the space
				Player temp = Player.Black;

				// if we are currently at the two middle rows of the board, then there are no space ownders
				if (i == 3 || i == 4)
					temp = Player.None;

				// of we are now at the 6th row, we need to change the color of the pieces
				if (i >= 5)
					temp = Player.Red;

				// for all slots in the row where pieces are allowed to be, place a marker
				for (int j = 0; j < 8; j = j + 2)
				{
					// offset is based on the row
					int offset = i % 2;
					// place a marker on each of these spots
					m_Spaces[i, (j + offset)].PlaceMarker(temp);
				}
			}

			this.Refresh();
		}

		private bool PiecesCanBeJumped()
		{
			// check the entire board for pieces that can be jumped
			for (int i = 0; i < 8; i++)
			{
				for (int j = 0; j < 8; j++)
				{
					// grab the curently selected board space
					BoardSpace CurrSpace = m_Spaces[i, j];

					// if the piece is not the current player's piece, disregard it
					if (CurrSpace.Owner != m_CurrPlayerTurn || CurrSpace.IsEmpty)
						continue;

					// if there is a jump available for this piece, then return true
					if (JumpAvailable(CurrSpace))
						return true;

				}
			}
			// if we fall through the loop without return true, then there is no way 
			return false;
		}

		private bool JumpAvailable(BoardSpace CurrSpace)
		{
			int start, end; // start and end indices for the array of differences

			// if the space has a king in it
			if (CurrSpace.HoldingKing)
			{
				// go ahead and look at the entire list of differences, they are all valid
				start = 0;
				end = 3;
			}
			// otherwise
			else
			{
				// if its a red piece, look at the last two items in the array
				if (CurrSpace.Owner == Player.Red)
				{
					start = 2;
					end = 3;
				}
				// otherwise
				else
				{
					// look at the first two items of the list because the piece is gray/black
					start = 0;
					end = 1;
				}
			}

			// for each of the determined applicable differences for the space
			for (int k = start; k <= end; k++)
			{
				// calculate where the piece would go
				int new_row = CurrSpace.Row + Differences[k].DeltaRow;
				int new_col = CurrSpace.Col + Differences[k].DeltaCol;

				// if the target space is not off the board
				if ((new_row >= 0 && new_row <= 7) && (new_col >= 0 && new_col <= 7))
				{
					if (m_Spaces[new_row, new_col].Owner == Player.None)
						continue;

					// if the currently selected space and target space dont have the same color pieces, and the target space does not contain a king
					// and the currently selected space doesnt have a regular piece
					if (CurrSpace.Owner != m_Spaces[new_row, new_col].Owner && !(m_Spaces[new_row, new_col].HoldingKing && !CurrSpace.HoldingKing))
					{
						// calculate the new target space
						new_row = new_row + Differences[k].DeltaRow;
						new_col = new_col + Differences[k].DeltaCol;

						// if the new target space is on the board
						if ((new_row >= 0 && new_row <= 7) && (new_col >= 0 && new_col <= 7))
							// and it is empty, we have a valid jump.  Then a jump is possible, return true
							if (m_Spaces[new_row, new_col].Owner == Player.None)
								return true;
					}
				}
			}
			return false;
		}

		private List<ValidMove> GetValidMoveList(BoardSpace CurrSelSpace)
		{
			List<ValidMove> currValidMoves = new List<ValidMove>();
			int start, end; // start and end indices for the array of differences

			// if the space has a king in it
			if (CurrSelSpace.HoldingKing)
			{
				// go ahead and look at the entire list of differences, they are all valid
				start = 0;
				end = 3;
			}
			// otherwise
			else
			{
				// if its a red piece, look at the last two items in the array
				if (CurrSelSpace.Owner == Player.Red)
				{
					start = 2;
					end = 3;
				}
				// otherwise
				else
				{
					// look at the first two items of the list because the piece is gray/black
					start = 0;
					end = 1;
				}
			}

			// for each of the determined applicable differences for the space
			for (int i = start; i <= end; i++)
			{
				// calculate where the piece would go
				int new_row = CurrSelSpace.Row + Differences[i].DeltaRow;
				int new_col = CurrSelSpace.Col + Differences[i].DeltaCol;

				// if the target space is not off the board
				if ((new_row >= 0 && new_row <= 7) && (new_col >= 0 && new_col <= 7))
				{
					// if the space is empty (not owned)
					if (m_Spaces[new_row, new_col].Owner == Player.None)
					{
						// if no jumps exist
						if (!m_JumpsExist)
							// add the current space to the list of valid moves
							currValidMoves.Add(new ValidMove(m_Spaces[new_row, new_col], null));
					}

					// otherwise
					else
					{
						// if the currently selected space and target space dont have the same color pieces, and the target space does not contain a king
						// and the currently selected space doesnt have a regular piece
						if (CurrSelSpace.Owner != m_Spaces[new_row, new_col].Owner && !(m_Spaces[new_row, new_col].HoldingKing && !CurrSelSpace.HoldingKing))
						{
							// the current space can be jumped
							BoardSpace MayBeJumped = m_Spaces[new_row, new_col];

							// calculate the new target space
							new_row = new_row + Differences[i].DeltaRow;
							new_col = new_col + Differences[i].DeltaCol;

							// if the new target space is on the board
							if ((new_row >= 0 && new_row <= 7) && (new_col >= 0 && new_col <= 7))
								// and it is empty, we have a valid jump.  Add it to the list
								if (m_Spaces[new_row, new_col].Owner == Player.None)
									currValidMoves.Add(new ValidMove(m_Spaces[new_row, new_col], MayBeJumped));
						}
					}
				}
			}

			return currValidMoves;
		}

		private void GetValidMoves()
		{
			if (m_sel_col == -1 || m_sel_row == -1)
				return;

			CurrentValidMoves = GetValidMoveList(m_Spaces[m_sel_row, m_sel_col]);
		}

		private bool SpaceSelected()
		{
			// if either of the selected numbers is -1, assume no selection
			// otherwise, there appears to be a selection
			if (m_sel_row == -1 || m_sel_col == -1)
				return false;
			else
				return true;
		}

		private void NextPlayer()
		{
			// if the current player is red, then the next player is black
			// otherwise, the next player is red
			if (m_CurrPlayerTurn == Player.Red)
				m_CurrPlayerTurn = Player.Black;
			else
				m_CurrPlayerTurn = Player.Red;
		}

		private bool TryToMakeMove(int to_row, int to_col)
		{
			// flag to return if a move was made
			bool MoveMade = false;

			// check if the designated move is in our current move list
			foreach (ValidMove b in CurrentValidMoves)
			{
				// if it is...
				if (b.MoveTo.Row == to_row && b.MoveTo.Col == to_col)
				{
					// swap the spaces to effectively move the piece
					m_Spaces[m_sel_row, m_sel_col].SwapPieces(m_Spaces[to_row, to_col]);

					// if the move being made has a jump associated with it...
					if (b.ToBeJumped != null)
					{
						// go ahead and jump the space
						b.ToBeJumped.JumpSpace();

						// if the current player is red, decrement blacks piece count
						if (this.m_CurrPlayerTurn == Player.Red)
							m_BlkPieceCount--;
						else
							// otherwise, decrement red's
							m_RedPieceCount--;

						// check if the move has a double jump available
						m_DblJumpExist = JumpAvailable(m_Spaces[to_row, to_col]);
					}

					// we have officially made a move
					MoveMade = true;

					// break from the loop
					break;
				}
			}

			// if a move was made,
			if (MoveMade)
			{
				// remove the highlight from the current selection
				m_Spaces[m_sel_row, m_sel_col].Highlight(false);

				// remove the highlight from all the valid moves
				foreach (ValidMove b in CurrentValidMoves)
					b.MoveTo.Highlight(false);

				// if no double jump exists
				if (!m_DblJumpExist)
				{
					// we no longer have a selection
					m_sel_col = -1;
					m_sel_row = -1;

					// if the delegate to the MoveCompleted event exists
					if (MoveCompleted != null)
						// call it with the board as the argument and no event args
						MoveCompleted(this, null);

					// get the next players
					this.NextPlayer();

					// check if a jump exists for what will be the next players turn
					m_JumpsExist = PiecesCanBeJumped();
				}
				else
				{
					// otherwise, there was a possible double jump, so our selection is the
					// piece that just performed the jump
					m_sel_col = to_col;
					m_sel_row = to_row;

					// highlight the new selection aka the piece that should do the next jump
					m_Spaces[m_sel_row, m_sel_col].Highlight(true);

					// get all the valid moves for the new selection
					GetValidMoves();

					// highlight the new possible moves
					foreach (ValidMove b in CurrentValidMoves)
						b.MoveTo.Highlight(true);
				}

				// redraw the board
				this.Refresh();
			}

			// return if a move was made
			return MoveMade;
		}

		private void SpaceClick(BoardSpace WasClicked)
		{
			// if the space is a clickable space
			if (WasClicked.Selectable)
			{
				// if there is a double jump
				if (m_DblJumpExist)
				{
					// try and make a move to the space that was clicked
					if (TryToMakeMove(WasClicked.Row, WasClicked.Col))
						return;
				}
				else
				{
					// if there is no piece selected
					if (!SpaceSelected())
					{
						// if the space is not empty and the piece is the color matches the player who is taking their turn
						if (!WasClicked.IsEmpty && WasClicked.Owner == m_CurrPlayerTurn)
						{
							// select that row and col
							m_sel_col = WasClicked.Col;
							m_sel_row = WasClicked.Row;

							// highlight the selection
							WasClicked.Highlight(true);

							// get all the valid moves for this space and highlight them
							GetValidMoves();

							foreach (ValidMove b in CurrentValidMoves)
								b.MoveTo.Highlight(true);
						}
					}
					// otherwise...
					else
					{
						// if the space is empty
						if (WasClicked.IsEmpty)
						{
							// try and make a move
							if (TryToMakeMove(WasClicked.Row, WasClicked.Col))
								return;
						}
						else
						{
							// otherwise, if the row and col match the current selection
							if (WasClicked.Row == m_sel_row && WasClicked.Col == m_sel_col)
							{
								// deselect the piece
								WasClicked.Highlight(false);
								m_sel_col = -1;
								m_sel_row = -1;

								// unhighlight all the valid moves associated
								foreach (ValidMove b in CurrentValidMoves)
									b.MoveTo.Highlight(false);
								CurrentValidMoves.Clear();
							}
							else
							{
								// otherwise, if the piece is that of the current players
								if (WasClicked.Owner == m_CurrPlayerTurn)
								{
									// unhighlight the other selected one and unhighlight all its valid moves
									m_Spaces[m_sel_row, m_sel_col].Highlight(false);
									foreach (ValidMove b in CurrentValidMoves)
										b.MoveTo.Highlight(false);
									CurrentValidMoves.Clear();

									// select and highlight the new selection, find all its valid moves and highlight those
									WasClicked.Highlight(true);
									m_sel_col = WasClicked.Col;
									m_sel_row = WasClicked.Row;
									GetValidMoves();
									foreach (ValidMove b in CurrentValidMoves)
										b.MoveTo.Highlight(true);
								}
							}
						}
					}
				}
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left && m_mouseDownIn != null)
			{
				BoardSpace spaceMouseUp = GetSpaceFromPoint(e.X, e.Y);
				if (spaceMouseUp == m_mouseDownIn)
				{
					SpaceClick(m_mouseDownIn);
					this.Refresh();
				}

				m_mouseDownIn = null;
			}

			base.OnMouseUp(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
				m_mouseDownIn = GetSpaceFromPoint(e.X, e.Y);

			base.OnMouseDown(e);
		}

		private BoardSpace GetSpaceFromPoint(int x, int y)
		{
			int i = y / (base.Width / 8);
			int j = x / (base.Height / 8);

			if (i < 0 || i >= 8)
				return null;

			if (j < 0 || j >= 8)
				return null;

			return m_Spaces[i, j];
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			// if this is not a filled or valid board then dont bother drawing it
			if (!m_isValid)
				return;

			// the graphics smoothing mode should be antialiased so that way all edges
			// show up smooth
			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			// the base x and y increments for the position is 1/8th the board since
			// since a checkers board is an 8 x 8 grid
			float base_x = (float)base.Width / 8;
			float base_y = (float)base.Height / 8;

			// for each row
			for (int i = 0; i < 8; i++)
			{
				// and for each column within that row
				for (int j = 0; j < 8; j++)
				{
					// Set the position position of each piece based on the row and column it belongs
					m_Spaces[i, j].Draw(e, (int)(base_x * j), (int)(base_y * i), (int)base_x, (int)base_y);
				}
			}

			// go ahead and paint the panel
			base.OnPaint(e);
		}
	}
}
