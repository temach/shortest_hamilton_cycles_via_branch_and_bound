using System;
using System.Collections.Generic;

namespace SolveDiscra
{
	static class Bootstrap
	{
		public static void Main (string[] args) {
			var app = new Program ();
			app.Run ();
		}

		public static List<T> eCopyValueElements<T> (this IList<T> input) {
			var output = new List<T> ();
			foreach (T elem in input) {
				T another = elem;
				output.Add (another);
			}
			return output;
		}
	}
}

