using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Diagnostics;

namespace SolveDiscra
{
	class Program
	{
		Dictionary<string, Point> letter2coord = new Dictionary<string, Point> ();
		List<Node> all_nodes = new List<Node> ();
		Dictionary<string, List<string>> graph = new Dictionary<string, List<string>> ();
		List<string> vlist = new List<string> { "a", "b", "c", "d", "e", "f" };
		Dictionary<Tuple<string, string>, int> edges_weight;

		int best_final_path_cost_ever = int.MaxValue;

		public Program () {
			//letter2coord["a"] = new Point(4,10);
			//letter2coord["b"] = new Point(3,8);
			//letter2coord["c"] = new Point(7,4);
			//letter2coord["d"] = new Point(8,6);
			//letter2coord["e"] = new Point(1,2);
			//letter2coord["f"] = new Point(6,7);

			letter2coord ["a"] = new Point (4, 1);
			letter2coord ["b"] = new Point (4, 3);
			letter2coord ["c"] = new Point (2, 7);
			letter2coord ["d"] = new Point (9, 6);
			letter2coord ["e"] = new Point (10, 7);
			letter2coord ["f"] = new Point (6, 10);

			// say that they are all connected
			graph ["a"] = vlist.Where (ch => ch != "a").ToList ();
			graph ["b"] = vlist.Where (ch => ch != "b").ToList ();
			graph ["c"] = vlist.Where (ch => ch != "c").ToList ();
			graph ["d"] = vlist.Where (ch => ch != "d").ToList ();
			graph ["e"] = vlist.Where (ch => ch != "e").ToList ();
			graph ["f"] = vlist.Where (ch => ch != "f").ToList ();
		}

		public Dictionary<Tuple<string,string>,int> GenEdgeWeight (Dictionary<string,List<string>> gr) {
			var l = new List<Tuple<string,string>> ();
			var weights = new Dictionary<Tuple<string, string>, int> ();
			foreach (string vertex_name in graph.Keys) {
				foreach (var other_vertex_name in graph[vertex_name]) {
					l.Add (new Tuple<string, string> (vertex_name,
					                                               other_vertex_name));
				}
			}
			foreach (var tup in l) {
				var coords = new Tuple<Point, Point> (letter2coord [tup.Item1],
				                                                 letter2coord [tup.Item2]);
				weights [tup] = Math.Abs (coords.Item1.X - coords.Item2.X)
				+ Math.Abs (coords.Item1.Y - coords.Item2.Y);
			}
			return weights;
		}

		public Matrix GenMatrix (List<string> vert_list) {
			int lenv = vert_list.Count;
			this.edges_weight = GenEdgeWeight (graph);

			var d0 = new List<List<int>> (lenv);
			for (int i = 0; i < lenv; i++) {
				d0.Add (Enumerable.Repeat (int.MaxValue, 6).ToList ());
			}
			for (int i = 0; i < lenv; i++) {
				for (int j = 0; j < lenv; j++) {
					var edge = new Tuple<string,string> (vlist [i], vlist [j]);
					if (edges_weight.ContainsKey (edge)) {
						d0 [i] [j] = this.edges_weight [edge];
					} else {
						// the only keys it does not contain are cycle keys ("a", "a"), ("b","b")
						d0 [i] [j] = int.MaxValue;
					}
				}
			}
			return new Matrix (d0);
		}

		public Matrix CalcMatrix () {
			return GenMatrix (vlist);
		}

		public void FirstStage () {
			Matrix startmat = CalcMatrix ();
			// This should be called only once on the root matrix
			for (int i = 0; i < startmat.qtyRows; i++) {
				for (int j = 0; j < startmat.qtyCols; j++) {
					if (i == j) {
						startmat [i, j] = int.MaxValue;
					}
				}
			}
			Console.WriteLine (startmat.ToString ());
			var node = new Node (startmat);
			node.name = "s";
			// step 1, calculate low_bound_weight
			node.weight = node.NormaliseGetDelta ();
			node.drop = node.matrix.GetSubproblemSplitEdge ();
			node.path = new List<Point> ();
			node.print_matrix = node.CalcPrintMatrix ();
			node.before_print_matrix = node.print_matrix.DeepCopy ();
			all_nodes.Add (node);
			Console.WriteLine ("\n\n");
			Console.WriteLine (node.matrix.ToString ());
			// left branch
			var s1 = node.BranchRightToS1 (node.drop);
			// right branch
			var s0 = node.BranchLeftToS0 (node.drop);
			// add to dict
			all_nodes.Add (s1);
			all_nodes.Add (s0);
		}

		public void Run () {
			FirstStage ();

			while (true) {
				// calculate new best ever final path cost
				var tmp = all_nodes.Where (nd => nd.IsTerminal);
				if (tmp.Count () > 0) {
					best_final_path_cost_ever = tmp.Select (nd => nd.weight).Min ();
				}
				var cur_leaves = all_nodes.Where (nd => nd.IsLeaf && (!nd.IsTerminal)).ToList ();
				if (cur_leaves.Count == 0) {
					// all leaves are also terminal nodes, so we have analysed everything
					break;
				}
				int min_leaf_weight = cur_leaves.Select (nd => nd.weight).Min ();
				Node curnode = cur_leaves.First (nd => nd.weight == min_leaf_weight);
				if (curnode.weight > best_final_path_cost_ever) {
					curnode.Terminate ("Node stopped: it costs more than the found shortest path.");
					continue;
				}
				if (curnode.print_matrix.qtyCols <= 2) {
					// then we can stop, if this matrix is clear what the last elements should be
					List<Point> zeros = curnode.matrix.GetZeroCoords ();
					Debug.Assert (zeros.Count == 2,
					                            "Some element is non zero: matrix is not finalized");
					curnode.AddMustTakeEdge (zeros.First ());
					curnode.AddFinalMustTakeEdge (zeros.Last ());
					curnode.Terminate ("Finished and found path.");
					continue;
				}
				curnode.drop = curnode.matrix.GetSubproblemSplitEdge ();
				Node s1 = curnode.BranchRightToS1 (curnode.drop);
				all_nodes.Add (s1);
				Node s0 = curnode.BranchLeftToS0 (curnode.drop);
				all_nodes.Add (s0);
			}
			// after the while(true) loop
			WriteOutput ();
			WriteDotFile ();
			WriteLatex ();
		}

		public void WriteOutput () {
			// after the while loop
			var output = new List<string> ();
			foreach (var node in all_nodes) {
				output.Add (node.ToString ());
			}
			File.WriteAllLines ("result.txt", output);
		}

		public void WriteDotFile () {
			List<string> print = new List<string> ();
			print.Add ("digraph G {\n");
			foreach (Node nd in all_nodes) {
				print.Add (nd.GetDotCommand ());
			}
			print.Add ("\n}");
			File.WriteAllLines ("dot_commands.txt", print);
		}

		public void WriteLatex () {
			List<string> print = new List<string> ();
			print.Add (LatexData.header);
			foreach (Node nd in all_nodes) {
				print.Add (nd.LatexCommand ());
			}
			print.Add (LatexData.footer);
			File.WriteAllLines ("latex_commands.tex", print);
		}
	}



	public static class LatexData
	{
		public static string header = @"\documentclass[a4paper,10pt]{report} % формат бумаги А4, шрифт по умолчанию - 12pt

% заметь, что в квадратных скобках вводятся необязательные аргументы пакетов.
% а в фигурных - обязательные

\usepackage[T2A]{fontenc} % поддержка кириллицы в Латехе
\usepackage[utf8]{inputenc} % включаю кодировку ютф8
\usepackage[english,russian]{babel} % использую русский и английский языки с переносами

\usepackage{indentfirst} % делать отступ в начале параграфа
\usepackage{amsmath} % математические штуковины
\usepackage{mathtools} % еще математические штуковины
\usepackage{mathtext}
\usepackage{multicol} % подключаю мультиколоночность в тексте
\usepackage{graphicx} % пакет для вставки графики, я хз нахуя он нужен в этом документе
\usepackage{listings} % пакет для вставки кода
\usepackage[table]{xcolor}% http://ctan.org/pkg/xcolor for coloring the inside of a cell
\usepackage[lofdepth,lotdepth]{subfig} % so we can place figures side by side

\usepackage{geometry} % меняю поля страницы
\usepackage{caption}

%из параметров ниже понятно, какие части полей страницы меняются:
\geometry{left=1cm}
\geometry{right=1cm}
\geometry{top=1cm}
\geometry{bottom=1cm}

\renewcommand{\baselinestretch}{1} % меняю ширину между строками на 1.5
\righthyphenmin=2
\begin{document}

% make the captions stick to the LEFT of the page
\captionsetup{justification=raggedright,
singlelinecheck=false
}

\captionsetup[subfloat]{labelformat=empty}


\begin{titlepage}
\newpage

\begin{center}
{\large НАЦИОНАЛЬНЫЙ ИССЛЕДОВАТЕЛЬСКИЙ УНИВЕРСИТЕТ \\
«ВЫСШАЯ ШКОЛА ЭКОНОМИКИ» 							\\
Дисциплина: «Дискретная математика»}

\vfill % заполняет длину страницы вертикально

{\large Домашнее задание 1}

\bigskip

\underline{Исследование комбинационных схем}\\
Вариант 002

\vfill

\begin{flushright}
Выполнил: Абрамов Артем,\\
студент группы БПИ1511\medskip \\
Преподаватель: Авдошин С.М., \\
профессор департамента \\
программной инженерии \\
факультета компьютерных наук
\end{flushright}

\vfill

Москва \number\year

\end{center}
\end{titlepage}
";

		public static string footer = @"\end{document}";

	}
}
