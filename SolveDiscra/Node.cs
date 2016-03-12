using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SolveDiscra
{
	public class Node
	{
		public int weight;
		public Node child_s0;
		public Node child_s1;
		public string name;
		public Matrix matrix;
		public Matrix print_matrix;
		public Node parent;
		public Point drop;
		public string why_no_children;
		public List<Point> path;
		public List<Point> never_take_edges = new List<Point> ();
		public List<int> debug_rows = new List<int> ();
		public List<int> debug_cols = new List<int> ();
		public Matrix before_print_matrix;
		public Point? color_cell = null;

		public List<int> minfromrows;
		public int alpha;
		public List<int> minfromcols;
		public int beta;

		public bool IsLeaf {
			get { return (this.child_s0 == null) && (this.child_s1 == null); }
		}

		public bool IsTerminal {
			get { return this.why_no_children.Length > 0; }
		}

		public bool IsS0 {
			get { return this.name.Last () == '0'; }
		}

		public bool IsS1 {
			get { return this.name.Last () == '1'; }
		}

		public Node (Matrix mat) {
			this.matrix = mat.DeepCopy ();
			this.why_no_children = "";
			this.weight = -1;
			this.path = null;
		}

		public void Terminate (string reason) {
			this.why_no_children = reason;
		}

		// normalise the matrix and return the delta to the original weight
		public int NormaliseGetDelta () {
			// step 1
			minfromrows = matrix.MinRows ();
			alpha = minfromrows.Sum ();
			this.matrix.SubRows (minfromrows);
			// step 2
			minfromcols = matrix.MinCols ();
			beta = minfromcols.Sum ();
			this.matrix.SubsCols (minfromcols);
			return alpha + beta;
		}

		// clean up matrix of short-circuits after deciding to take an edge
		public void AddMustTakeEdge (Point must_take_edge) {
			// operation on matrix should be first
			this.matrix.TakeEdgeMarkMinusOne (must_take_edge);
			this.path.Add (must_take_edge);
			// this may insert infinities into the node matrix
			this.color_cell = this.RemoveMatrixBadCycles (this.matrix);
		}

		public void AddFinalMustTakeEdge (Point final_must_take) {
			// operation on matrix should be first
			this.matrix.TakeEdgeMarkMinusOne (final_must_take);
			this.path.Add (final_must_take);
			// do not try to remove cycles
		}

		public void DropEdgeAndNeverTakeIt (Point must_never_take_edge) {
			this.matrix.DropEdgeMarkInf (must_never_take_edge);
			this.color_cell = must_never_take_edge;
			this.never_take_edges.Add (must_never_take_edge);
		}

		public Node BranchRightToS1 (Point must_take_edge) {
			var s1 = new Node (this.matrix);
			// copy parent stuff
			s1.parent = this;
			s1.name = s1.parent.name + "1";
			// calculate self stuff
			if (s1.parent.path.Count () > 0) {
				s1.path = s1.parent.path.eCopyValueElements ();
				s1.never_take_edges = s1.parent.never_take_edges.eCopyValueElements ();
			} else {
				s1.path = new List<Point> ();
			}
			s1.AddMustTakeEdge (must_take_edge);
			s1.before_print_matrix = s1.CalcPrintMatrix ();
			s1.weight = s1.parent.weight + s1.NormaliseGetDelta ();
			s1.print_matrix = s1.CalcPrintMatrix ();
			this.child_s1 = s1;
			return s1;
		}

		public Node BranchLeftToS0 (Point never_take_edge) {
			// this matrix gets infinities set in it one by one until it is all 
			// infinities at which point it should be terminated due to overweight
			var s0 = new Node (this.matrix);
			// assign parent stuff
			s0.parent = this;
			s0.name = s0.parent.name + "0";
			if (s0.parent.path.Count () > 0) {
				s0.path = s0.parent.path.eCopyValueElements ();
				s0.never_take_edges = s0.parent.never_take_edges.eCopyValueElements ();
			} else {
				s0.path = new List<Point> ();
			}
			// calculate self stuff
			s0.DropEdgeAndNeverTakeIt (never_take_edge);
			s0.before_print_matrix = s0.CalcPrintMatrix ();
			s0.weight = s0.parent.weight + s0.NormaliseGetDelta ();
			s0.print_matrix = s0.CalcPrintMatrix ();
			this.child_s0 = s0;
			return s0;
		}

		// this should be _not_ needed
		private List<Point> GetParentTakes () {
			var takes = new List<Point> ();
			Node papa;
			Node cur = this;
			while ((papa = cur.parent) != null) {
				takes.Add (papa.drop);
				cur = papa;
			}
			return takes;
		}

		public Point? RemoveMatrixBadCycles (Matrix cur_matrix) {
			Point? killed_edge = null;
			// List<Point> path = GetParentTakes();
			foreach (Point edge in path) {
				var cur = edge;
				// the node index at which we started
				int totalstart = cur.X;
				int totalend = -1;
				while (true) {
					var following = path.Where (e => cur.Y == e.X);
					int count = following.Count ();
					if (count > 1) {
						throw new Exception ("more than one outbound edge, "
						+ "this is result of some earlier error.");
					} else if (count == 1) {
						cur = following.First ();
					} else if (count == 0) {
						totalend = cur.Y;
						// if the currently edge that you are killing has not already been killed
						if (cur_matrix.mat [totalend] [totalstart] < 100) {
							killed_edge = new Point (totalend, totalstart);
						}
						//if ( 
						//(! path.Select(p => p.X).Contains(totalstart)) 
						//    && (! path.Select(p => p.Y).Contains(totalend))
						//)
						cur_matrix.DropEdgeMarkInf (new Point (totalend,
						                                       totalstart));
						break;
					}
				}
			}
			return killed_edge;
		}

		public List<Point> GetNodePath () {
			var ordered_path = new List<Point> ();
			// add start
			var start = this.path.First ();
			ordered_path.Add (start);
			// set cur to one node after start
			Point cur = path.Where (e => start.Y == e.X).First ();
			;
			do {
				ordered_path.Add (cur);
				// where the end of current is start of new one
				cur = path.Where (e => cur.Y == e.X).First ();
			} while (cur != start);  // follow the cur node until we reach start again
			return ordered_path;
		}

		public Matrix CalcPrintMatrix () {
			var to_print_matrix = this.matrix.DeepCopy ();
			// we must delete starting with largest indexes first so as not to mess up the print matrix
			var by_row = path.OrderByDescending (p => p.X);
			var by_col = path.OrderByDescending (p => p.Y);
			foreach (var edge_taken in by_col) {
				to_print_matrix.PhysicallyDropCol (edge_taken.Y);
			}
			foreach (var edge_taken in by_row) {
				to_print_matrix.PhysicallyDropRow (edge_taken.X);
			}
			return to_print_matrix.DeepCopy ();
		}

		public string PrintOneChild (Node ch) {
			return string.Format ("child {0} matrix:\n"
			+ "debug underlying matrix:\n{1}\n"
			+ "output martix before:\n{2}\n"
			+ "output matrix AFTER:\n{3}\n"
			+ "low bound b:{4}\n"
			                      , ch.name
			                      , ch.matrix
			                      , ch.parent.print_matrix
			                      , ch.print_matrix
			                      , ch.weight
			);
		}

		public string[] PrintChildren () {
			string chs0 = "";
			string chs1 = "";
			if (child_s0 != null && child_s1 != null) {
				string childsep = new string ('-', 40) + "\n";
				chs0 = childsep + PrintOneChild (child_s0);
				chs1 = childsep + PrintOneChild (child_s1);
			}
			return new string[] { chs0, chs1 };
		}

		public string PrintTitle () {
			string newpage = new string ('=', 100) + "\n";
			string endpage = new string ('\n', 4);
			string[] children = PrintChildren ();
			string title = string.Format ("wroking with node:{0}\n"
			               + "node matrix:\n{1}\n{2}\n{3}\n" 
			                              , name
			                              , this.print_matrix, this.DotEdge (), this.DotPath ()
			               );
			string why_stop = "\n" + this.why_no_children + "\n";
			return newpage + title + children [0] + children [1] + why_stop + endpage;
		}

		public override string ToString () {
			return PrintTitle ();
		}

		public string GetDotInnerTitle () {
			string node_inner = string.Format ("\"{0}\\n{1};{2}\\n{3}\""
			                                   , name
			                                   , DotWeight ()
			                                   , DotEdge ()
			                                   , DotPath ()
			                    );
			return node_inner;
		}

		public string DotEdge () {
			string edge = "";
			if (!IsTerminal) {
				edge = string.Format ("edge=({0},{1})",
				                      this.drop.X + 1,
				                      this.drop.Y + 1);
			}
			return edge;
		}

		public string DotWeight () {
			return string.Format ("weight={0}", weight);
		}

		public string DotPath () {
			string finpath = "";
			if (this.path.Count == 6) {
				finpath = "path=" + string.Join (" ",
				                                 GetNodePath ().Select (p => p.X + 1)).Replace (" ",
				                                                                                "");
			}
			return finpath;
		}

		public string[] DotChildren () {
			string chs0 = "";
			string chs1 = "";
			if (child_s0 != null && child_s1 != null) {
				chs0 = string.Format ("->{0}", child_s1.GetDotInnerTitle ());
				chs1 = string.Format ("->{0}", child_s0.GetDotInnerTitle ());
			}
			return new string[] { chs0, chs1 };
		}

		public string GetDotCommand () {
			string title = GetDotInnerTitle ();
			var children = DotChildren ();
			return (title + children [0] + ";\n") + (title + children [1] + ";\n");
		}

		// LATEX STUFF
		public string tex_newpage = @"\newpage" + "\n";
		public string flushleft = @"\begin{{flushleft}}{0}" + @"\end{{flushleft}}" + "\n\n";
		public string flushright = @"\begin{{flushright}}{0}" + @"\end{{flushright}}" + "\n\n";
		public string tex_subfloat = @"\subfloat[][]{{" + "{0}" + "}}";
		public string tex_hfill = @"\hfill" + "\n";
		public string tex_one_row_table = @"\begin{{table}}[ht]" + "\n{0}\n" + @"\end{{table}}" + "\n\n";
		public string tex_cline = @"\cline{0-";
		public string tex_hline = @"\hline";
		public string tex_tabular = @"\begin{{tabular}}[]{{" + "{0}" + "}}\n";
		public string tex_table_end = "\n" + @"\end{tabular}" + "\n";
		public string tex_page_head = @"Определим дугу ветвления для разбиения множества {0} \\" + "\n";
		public string tex_nl = @"\\" + "\n";
		public string cellcolor = @"\cellcolor{yellow}";
		public string tex_caption = @"\captionof*{table}{";

		public Tuple<List<string>,List<string>> TexGetCurrentRowsCols (List<Point> new_path) {
			List<int> row_names = Enumerable.Range (0, 6).ToList ();
			List<int> col_names = Enumerable.Range (0, 6).ToList ();
			foreach (var edge_taken in new_path) {
				row_names [edge_taken.X] = int.MaxValue;
				col_names [edge_taken.Y] = int.MaxValue;
			}
			var rows = row_names.Where (n => n < 100).Select (n => (n + 1).ToString ()).ToList ();
			var cols = col_names.Where (n => n < 100).Select (n => (n + 1).ToString ()).ToList ();
			return new Tuple<List<string>,List<string>> (rows, cols);
		}

		public Matrix TexCalcPrintMatrix (Matrix mat, List<Point> new_path) {
			Matrix cur_mat = mat.DeepCopy ();
			var by_row = new_path.OrderByDescending (p => p.X);
			var by_col = new_path.OrderByDescending (p => p.Y);
			foreach (var edge_taken in by_col) {
				cur_mat.PhysicallyDropCol (edge_taken.Y);
			}
			foreach (var edge_taken in by_row) {
				cur_mat.PhysicallyDropRow (edge_taken.X);
			}
			return cur_mat;
		}

		public string Row2Tex (IEnumerable<int> r, int? color_column = null) {
			var tmp = r.Select (elem => (elem < 100) ? string.Format ("{0,6}",
			                                                          elem) : @"$\infty$");
			if (color_column != null) {
				// add cell color to column
				tmp = tmp.Select ((elem, index) => (index == color_column) ? (cellcolor + elem.Trim ()) : elem);
			}
			return string.Join (" & ", tmp);
		}

		// for s0: print s0.print_matrix
		// for s1: print s1.print_matrix
		public string TexTableFromMatrix (Matrix some_mat, Node nd) {
			var cur_matrix = some_mat.DeepCopy ();
			// Note: only add +1 for vertical line at table end + 1 for row names
			var column_lines = Enumerable.Repeat ("|", cur_matrix.qtyCols + 2);
			string table_layout = string.Join ("c", column_lines);
			table_layout = table_layout.Substring (1, table_layout.Length - 1);
			string table_begin = string.Format (tex_tabular, table_layout);
			// when we have the path is of the final node it has 6 elements, so
			// when we try to get the col_names we get nothing as result. Hence we must cut out the last two
			// edges that we took when the path is final and complete
			var path_before_last = nd.path.eCopyValueElements ();
			if (nd.path.Count () == 6) {
				path_before_last = path_before_last.Take (4).ToList ();
			}
			var row_col_names = TexGetCurrentRowsCols (path_before_last);
			var row_names = row_col_names.Item1;
			var col_names = row_col_names.Item2;
			col_names.Insert (0, nd.name);
			List<string> table_inner = cur_matrix.mat.Select ((row, index) => row_names [index] + " & " + Row2Tex (row) + tex_nl + string.Format ("\\cline{{2-{0}}}",
			                                                                                                                                   row.Count + 1)).ToList ();
			// add first row of names
			string col_names_string = string.Join (" & ", col_names) + " ";
			Console.WriteLine (col_names_string);
			foreach (Match m in Regex.Matches (col_names_string, @" \d{1,} ")) {
				string s = m.ToString ();
				col_names_string = col_names_string.Replace (m.ToString (), "\\multicolumn{1}{c}{" + s.Substring (1, s.Length - 2) + "}");
			}
			col_names_string = col_names_string.Replace (" &", " } &").Insert(0,"\\multicolumn{1}{c}{ ");
			table_inner.Insert (0,
			                    col_names_string + tex_nl + string.Format ("\\cline{{2-{0}}}",
			                                                               cur_matrix.mat.Count + 1));
			return table_begin + string.Join ("\n", table_inner) + tex_table_end;
		}

		// when you want a table with "min" extra column and row
		// AND you want to highlight a certain cell in it
		public string TexMakeColorCellTableWithMinColumn (Matrix cur_matrix, Node nd, Point color_cell) {
			List<int> print_min_rows = cur_matrix.MinRows ();
			List<int> print_min_cols = cur_matrix.MinCols ();
			// columns +3 becasue +1 (min column) and +1 for vertical line and +1 for row names
			var column_lines = Enumerable.Repeat ("|", cur_matrix.qtyCols + 3);
			string table_layout = string.Join ("c", column_lines);
			// remove last vertical bar in for min apha value column
			table_layout = table_layout.Substring (1, table_layout.Length - 2);
			// Get column and row names
			var row_col_names = TexGetCurrentRowsCols (nd.path);
			var row_names = row_col_names.Item1;
			var col_names = row_col_names.Item2;
			col_names.Insert (0, nd.name);
			col_names.Add ("min");
			string table_begin = string.Format (tex_tabular, table_layout);
			// process the inner table. The "a & " is for extra column with names
			List<string> table_inner = cur_matrix.mat.Select (
				                           (row, index) => row_names [index] + " & "
				// when we are on the correct row, highlight the given cell
				                           + ((index == color_cell.X) ? Row2Tex (row,
				                                                                 color_cell.Y) : Row2Tex (row))
				                           + " & " + print_min_rows [index]
				                           + tex_nl + string.Format ("\\cline{{2-{0}}}",
				                                                     row.Count + 1)
			                           ).ToList ();
			// add last row of minimum values
			string min_row = "\\multicolumn{1}{c}{min} & " + Row2Tex (print_min_cols) + @" \\";
			foreach (Match m in Regex.Matches (min_row, @" \d{1,} ")) {
				string s = m.ToString ();
				min_row = min_row.Replace (m.ToString (),
				                           "\\multicolumn{1}{c}{" + s.Substring (1,
				                                                                 s.Length - 2) + "}");
			}
			table_inner.Add (min_row);
			// add first row of names
			string col_names_string = string.Join (" & ", col_names);
			foreach (Match m in Regex.Matches (col_names_string, @" \d{1,} ")) {
				string s = m.ToString ();
				col_names_string = col_names_string.Replace (m.ToString (), "\\multicolumn{1}{c}{" + s.Substring (1, s.Length - 2) + "}");
			}
			col_names_string = col_names_string.Replace (" &", " } &").Insert(0,"\\multicolumn{1}{c}{ ");
			table_inner.Insert (0,
			                    col_names_string + tex_nl + string.Format ("\\cline{{2-{0}}}",
			                                                               cur_matrix.mat.Count + 1));
			return table_begin + string.Join ("\n", table_inner) + tex_table_end;
		}

		// when you want at table with extra row and column "min"
		public string TexMakeTableWithMinColumn (Matrix cur_matrix, Node nd) {
			List<int> print_min_rows = cur_matrix.MinRows ();
			List<int> print_min_cols = cur_matrix.MinCols ();
			// columns +3 becasue +1 (min column) and +1 for vertical line and +1 for row names
			var column_lines = Enumerable.Repeat ("|", cur_matrix.qtyCols + 3);
			string table_layout = string.Join ("c", column_lines);
			// remove last vertical bar in for min apha value column
			table_layout = table_layout.Substring (1, table_layout.Length - 2);
			// Get column and row names
			var row_col_names = TexGetCurrentRowsCols (nd.path);
			var row_names = row_col_names.Item1;
			var col_names = row_col_names.Item2;
			col_names.Insert (0, nd.name);
			col_names.Add ("min");
			string table_begin = string.Format (tex_tabular, table_layout);
			// process the inner table. The "a & " is for extra column with names
			List<string> table_inner = cur_matrix.mat.Select (
				                           (row, index) => row_names [index] + " & "
				                           + Row2Tex (row) + " & "
				                           + print_min_rows [index] + tex_nl + string.Format ("\\cline{{2-{0}}}",
				                                                                              row.Count + 1)
			                           ).ToList ();
			// add last row of minimum values
			string min_row = "\\multicolumn{1}{c}{min} & " + Row2Tex (print_min_cols) + @" \\";
			foreach (Match m in Regex.Matches (min_row, @" \d{1,} ")) {
				string s = m.ToString ();
				min_row = min_row.Replace (m.ToString (),
				                           "\\multicolumn{1}{c}{" + s.Substring (1,
				                                                                 s.Length - 2) + "}");
			}
			table_inner.Add (min_row);
			// add first row of names
			string col_names_string = string.Join (" & ", col_names);
			foreach (Match m in Regex.Matches (col_names_string, @" \d{1,} ")) {
				string s = m.ToString ();
				col_names_string = col_names_string.Replace (m.ToString (), "\\multicolumn{1}{c}{" + s.Substring (1, s.Length - 2) + "}");
			}
			col_names_string = col_names_string.Replace (" &", " } &").Insert(0,"\\multicolumn{1}{c}{ ");
			table_inner.Insert (0,
			                    col_names_string + tex_nl + string.Format ("\\cline{{2-{0}}}",
			                                                               cur_matrix.mat.Count + 1));
			return table_begin + string.Join ("\n", table_inner) + tex_table_end;
		}

		// for s0 child: get parent.matrix (current print_matrix) + set to ininity where parent.drop
		public Matrix TexS0_MatrixBefore () {
			var cur_matrix = this.matrix.DeepCopy ();
			var cur_path = this.child_s0.path.eCopyValueElements ();
			// This are LIST type, we are safe.
			cur_matrix.DropEdgeMarkInf (this.drop);
			return TexCalcPrintMatrix (cur_matrix, cur_path);
		}

		// for s1 child: get parent.matrix (current print_matrix) + drop the edges/columns
		public Matrix TexS1_MatrixBefore () {
			return child_s1.before_print_matrix;
		}

		// to calculate child's lower bounds
		public string TexLowerBound (Node ch) {
			string this_node_low_bound_str = name.Replace ('S', 'b');
			string child_low_bound_str = ch.name.Replace ('S', 'b');
			string low_b_calculation = string.Format ("{0} = {1} + {2} + {3} = {4}"
			                                          , child_low_bound_str
			                                          , this_node_low_bound_str
			                                          , ch.alpha
			                                          , ch.beta
			                                          , ch.weight);
			return tex_caption + low_b_calculation + "}";
		}

		public Point GoFromGlobalIntoToPrintCoords (Point global_edge, List<Point> cur_drop_path) {
			// we must delete starting with largest indexes first so as not to mess up the print matrix
			var by_row = cur_drop_path.OrderByDescending (p => p.X);
			var by_col = cur_drop_path.OrderByDescending (p => p.Y);
			int decrement = 0;
			foreach (var drop_point in by_col) {
				if (drop_point.Y < global_edge.Y) {
					decrement++;
				}
			}
			global_edge.Y -= decrement;
			decrement = 0;
			foreach (var drop_point in by_row) {
				if (drop_point.X < global_edge.X) {
					decrement++;
				}
			}
			global_edge.X -= decrement;
			// returning a struct makes a copy
			return global_edge;
		}

		public string TexMakeThirdOfPageWithChild (Node ch, Matrix before) {
			string print_before;
			if (ch.color_cell != null) {
				// because we record the color_cells values for full matrices
				// , but we need to apply them to changed matrices (changed for printing)
				// so we need to modify the X and Y value accordingly. 
				// HOW TO DO THIS???
				var tmp = new Point (ch.color_cell.Value.X,
				                     ch.color_cell.Value.Y);
				Point highlight = GoFromGlobalIntoToPrintCoords (tmp,
				                                                 ch.path.eCopyValueElements ());
				print_before = TexMakeColorCellTableWithMinColumn (before.DeepCopy (),
				                                                   ch,
				                                                   highlight);
			} else {
				print_before = TexMakeTableWithMinColumn (before.DeepCopy (), ch);
			}
			string lower_bound = TexLowerBound (ch);
			string left = string.Format (tex_subfloat, print_before);
			left += tex_hfill;
			// after matrix = child.print_matrix
			string print_after = TexTableFromMatrix (ch.print_matrix.DeepCopy (),
			                                         ch);
			string right = string.Format (tex_subfloat, print_after);
			right += lower_bound;
			// put under "table"
			string one_row = string.Format (tex_one_row_table, left + right);
			return one_row;
		}

		public string TexEdge () {
			var str = DotEdge ();
			return str.Length > 0 ? str.Substring (5) : "";
		}

		public string TexOneNode () {
			// oh. my. dog.
			if (this.why_no_children.Contains ("costs more")) {
				// we terminated this node becasue it was overweight. We don't need 
				// to generate a whole page without children for it.
				return "";
			}
			string print_cur_node = TexTableFromMatrix (this.print_matrix.DeepCopy (),
			                                            this);
			// change this code to also remove the "edge="
			string print_drop_edge = tex_caption + TexEdge () + "}\n";
			string flushleft_current_print = string.Format (flushleft,
			                                                print_cur_node + print_drop_edge);
			// get rows of data
			string s0_data_row = "";
			string s1_data_row = "";
			if (this.child_s1 != null && child_s0 != null) {
				Matrix s0_before = TexS0_MatrixBefore ();
				s0_data_row = TexMakeThirdOfPageWithChild (child_s0, s0_before);
				// if this is a finishing matrix for s1, do something slightly different
				Matrix s1_before = TexS1_MatrixBefore ();
				if (s1_before.qtyRows == 2) {
					s1_data_row = TexTableFromMatrix (s1_before.DeepCopy (),
					                                  child_s1);
					s1_data_row += TexLowerBound (child_s1);
				} else {
					s1_data_row = TexMakeThirdOfPageWithChild (child_s1,
					                                           s1_before);
				}
			}
			// now final combined page
			string node_full_data = string.Format ("{0}\n{1}\n{2}"
			                                       , flushleft_current_print
			                                       , s0_data_row
			                                       , s1_data_row
			                        );
			return string.Format (tex_page_head, this.name) + node_full_data + tex_newpage;
		}

		public string LatexCommand () {
			return TexOneNode ();
		}
	}
}

