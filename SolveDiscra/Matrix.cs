using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace SolveDiscra
{
	public class Matrix
	{
		public List<List<int>> mat;
		public int size;

		public int qtyRows {
			get { return mat.Count (); }
		}

		public int qtyCols {
			get { return mat [0].Count (); }
		}

		public Matrix (List<List<int>> init_mat) {
			this.mat = init_mat;
			this.size = init_mat.Count;
		}

		public int this [int i, int j] {
			get { return mat [i] [j]; }
			set { mat [i] [j] = value; }
		}

		private void DropRow (int i) {
			mat = mat.Select (
				(row, index) => index == i ? row.Select (x => int.MaxValue).ToList () : row
			).ToList ();
		}

		private void DropCol (int j) {
			mat = mat.Select (
				row => row.Select ((elem, index) => index == j ? int.MaxValue : elem).ToList ()
			).ToList ();
		}

		// Physically drop row from matrix, this is useful _only_ when printing out final result
		public void PhysicallyDropRow (int i) {
			mat = mat.Where (
				(row, index) => index != i 
			).ToList ();
		}
		// Physically drop column from matrix, this is useful _only_ when printing out final result
		public void PhysicallyDropCol (int j) {
			mat = mat.Select (
				row => row.Where ((elem, index) => index != j).ToList ()
			).ToList ();
		}

		public Matrix DeepCopy () {
			return new Matrix (mat.Select (row => row.Select (elem => elem).ToList ()).ToList ());
		}

		public Matrix () {
			this.mat = new List<List<int>> ();
			this.size = 0;
		}

		public void SubRows (List<int> vector) {
			for (int i = 0; i < vector.Count; i++) {
				var row = mat [i];
				for (int j = 0; j < row.Count; j++) {
					row [j] -= vector [i];
				}
			}
		}

		public void SubsCols (List<int> vector) {
			// iter columns
			for (int i = 0; i < vector.Count; i++) {
				// iter rows
				for (int j = 0; j < vector.Count; j++) {
					mat [i] [j] -= vector [j];
				}
			}
		}

		public List<int> MinCols () {
			var ret = new List<int> ();
			// iter columns
			for (int i = 0; i < qtyCols; i++) {
				bool inf_col = true;
				int min_col = int.MaxValue - 10000;
				// iter rows
				for (int j = 0; j < qtyRows; j++) {
					if (mat [j] [i] < min_col) {
						inf_col = false;
						min_col = mat [j] [i] < 100 ? mat [j] [i] : 0;
					}
				}
				if (inf_col == false) {
					ret.Add (min_col);
				} else {
					ret.Add (0);
				}
			}
			return ret;
		}

		public List<int> MinRows () {
			var ret = new List<int> ();
			for (int i = 0; i < qtyRows; i++) {
				bool inf_row = true;
				int min_row = int.MaxValue - 10000;
				for (int j = 0; j < qtyRows; j++) {
					if (mat [i] [j] < min_row) {
						inf_row = false;
						min_row = mat [i] [j] < 100 ? mat [i] [j] : 0;
					}
				}
				if (inf_row == false) {
					ret.Add (min_row);
				} else {
					ret.Add (0);
				}
			}
			return ret;
		}

		private void SetInf (Point drop_coord) {
			mat [drop_coord.X] [drop_coord.Y] = int.MaxValue;
		}

		public void DropEdgeMarkInf (Point drop_coord) {
			SetInf (drop_coord);
		}

		public void TakeEdgeMarkMinusOne (Point drop_row_col) {
			this.DropRow (drop_row_col.X);
			this.DropCol (drop_row_col.Y);
		}

		private int MeasureEdgePotential (int i, int j) {
			// iter row
			var row = mat [i].eCopyValueElements ();
			row [j] = int.MaxValue;
			int rowmin = row.Min ();
			// iter col
			var col = new List<int> ();
			for (int r_index = 0; r_index < qtyRows; r_index++) {
				col.Add (mat [r_index] [j]);
			}
			col [i] = int.MaxValue;
			int colmin = col.Min ();
			return Math.Max (colmin, rowmin);
		}

		// where the zero elements of the matrix are
		public List<Point> GetZeroCoords () {
			var ret = new List<Point> ();
			for (int i = 0; i < qtyRows; i++) {
				for (int j = 0; j < qtyCols; j++) {
					if (mat [i] [j] == 0) {
						ret.Add (new Point (i, j));
					}
				}
			}
			return ret;
		}

		// Split problem on this edge. Right branch (S1) must have this edge. Left branch (S0) must not
		public Point GetSubproblemSplitEdge () {
			var dict = new Dictionary<int, Point> ();
			List<Point> zeros = GetZeroCoords ();
			foreach (var point in zeros) {
				int w = MeasureEdgePotential (point.X, point.Y);
				if (!dict.ContainsKey (w)) {
					dict [MeasureEdgePotential (point.X, point.Y)] = point;
				}
			}
			int drop_weight = dict.Keys.Max ();
			return dict [drop_weight];
		}

		public override string ToString () {
			return string.Join ("\n", 
			                   mat.Select (row => string.Join (", ", 
			                   row.Select (x => x < 50 ? String.Format ("{0,3}", x) : "***")
			                   )
			                   )
			);
		}
	}
}