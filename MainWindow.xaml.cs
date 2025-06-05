using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GraphEditor
{
    public partial class MainWindow : Window
    {
        private Dictionary<Ellipse, int> nodes = new();
        private Dictionary<int, Ellipse> nodeById = new();
        private List<Line> edges = new();
        private Dictionary<(int, int), (double weight, Line line)> edgeWeights = new();
        private List<TextBlock> weights = new();
        private bool drawingEdge = false;
        private Ellipse selectedNode = null;
        private int nodeCounter = 0;
        private bool useMatrixInput = false;
        private List<Line> highlightedLines = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (useMatrixInput) return;

            Point click = e.GetPosition(GraphCanvas);
            var hit = VisualTreeHelper.HitTest(GraphCanvas, click);

            if (hit?.VisualHit is Ellipse nodeEllipse && nodes.ContainsKey(nodeEllipse))
            {
                if (!drawingEdge)
                {
                    selectedNode = nodeEllipse;
                    drawingEdge = true;
                    HighlightSelectedNode(nodeEllipse);
                }
                else if (selectedNode != nodeEllipse)
                {
                    DrawEdge(selectedNode, nodeEllipse);
                    ResetNodeHighlight(selectedNode);
                    drawingEdge = false;
                    selectedNode = null;
                }
                else
                {
                    ResetNodeHighlight(selectedNode);
                    drawingEdge = false;
                    selectedNode = null;
                }
                return;
            }
            else if (hit?.VisualHit is TextBlock textBlock && int.TryParse(textBlock.Text, out int nodeId))
            {
                var nodeEllipseFromText = nodeById[nodeId];
                if (!drawingEdge)
                {
                    selectedNode = nodeEllipseFromText;
                    drawingEdge = true;
                    HighlightSelectedNode(nodeEllipseFromText);
                }
                else if (selectedNode != nodeEllipseFromText)
                {
                    DrawEdge(selectedNode, nodeEllipseFromText);
                    ResetNodeHighlight(selectedNode);
                    drawingEdge = false;
                    selectedNode = null;
                }
                else
                {
                    ResetNodeHighlight(selectedNode);
                    drawingEdge = false;
                    selectedNode = null;
                }
            }
            else
            {
                if (!drawingEdge)
                {
                    CreateNode(click);
                }
            }
        }

        private void HighlightSelectedNode(Ellipse nodeEllipse)
        {
            nodeEllipse.Fill = Brushes.Red;
        }

        private void ResetNodeHighlight(Ellipse nodeEllipse)
        {
            nodeEllipse.Fill = Brushes.LightBlue;
        }

        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point click = e.GetPosition(GraphCanvas);
            var hit = VisualTreeHelper.HitTest(GraphCanvas, click);

            if (hit?.VisualHit is Ellipse nodeEllipse && nodes.ContainsKey(nodeEllipse))
            {
                int id = nodes[nodeEllipse];
                var toRemove = edgeWeights.Keys
                    .Where(k => k.Item1 == id || k.Item2 == id)
                    .Distinct()
                    .ToList();

                foreach (var key in toRemove)
                {
                    if (edgeWeights.TryGetValue(key, out var edge))
                    {
                        if (edge.line != null)
                            GraphCanvas.Children.Remove(edge.line);
                        weights.RemoveAll(w =>
                        {
                            if (w.Text == edge.weight.ToString())
                            {
                                GraphCanvas.Children.Remove(w);
                                return true;
                            }
                            return false;
                        });
                    }
                    edgeWeights.Remove(key);
                }

                GraphCanvas.Children.Remove(nodeEllipse);
                var text = GraphCanvas.Children.OfType<TextBlock>().FirstOrDefault(t => t.Text == id.ToString());
                if (text != null)
                    GraphCanvas.Children.Remove(text);

                nodes.Remove(nodeEllipse);
                nodeById.Remove(id);
                UpdateMatrixInputFromGraph();
                return;
            }

            if (selectedNode != null)
            {
                ResetNodeHighlight(selectedNode);
            }
            drawingEdge = false;
            selectedNode = null;
        }

        private void CreateNode(Point pos)
        {
            Ellipse nodeEllipse = new()
            {
                Width = 20,
                Height = 20,
                Fill = Brushes.LightBlue,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            GraphCanvas.Children.Add(nodeEllipse);
            Canvas.SetLeft(nodeEllipse, pos.X - 15);
            Canvas.SetTop(nodeEllipse, pos.Y - 15);

            nodes[nodeEllipse] = nodeCounter;
            nodeById[nodeCounter] = nodeEllipse;

            TextBlock label = new()
            {
                Text = nodeCounter.ToString(),
                IsHitTestVisible = false
            };
            GraphCanvas.Children.Add(label);
            Canvas.SetLeft(label, pos.X + 8);
            Canvas.SetTop(label, pos.Y - 15);

            nodeCounter++;
            UpdateMatrixInputFromGraph();
        }

        private Point GetCenter(Ellipse nodeEllipse)
        {
            return new Point(Canvas.GetLeft(nodeEllipse) + nodeEllipse.Width / 2, Canvas.GetTop(nodeEllipse) + nodeEllipse.Height / 2);
        }

        private void DrawEdge(Ellipse from, Ellipse to, double? weightOverride = null)
        {
            Point p1 = GetCenter(from);
            Point p2 = GetCenter(to);

            Line edge = new()
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            GraphCanvas.Children.Add(edge);
            edges.Add(edge);

            double weight = weightOverride ?? 1;
            if (!weightOverride.HasValue)
            {
                var dialog = new WeightInputWindow { Owner = this };
                if (dialog.ShowDialog() == true && dialog.Weight.HasValue)
                {
                    weight = dialog.Weight.Value;
                }
                else
                {
                    return; // Пользователь отменил ввод — не рисуем ребро
                }

            }

            int id1 = nodes[from];
            int id2 = nodes[to];
            edgeWeights[(id1, id2)] = (weight, edge);
            edgeWeights[(id2, id1)] = (weight, edge);

            TextBlock wt = new()
            {
                Text = weight.ToString(),
                Background = Brushes.White
            };
            weights.Add(wt);
            GraphCanvas.Children.Add(wt);
            Canvas.SetLeft(wt, (p1.X + p2.X) / 2);
            Canvas.SetTop(wt, (p1.Y + p2.Y) / 2);

            UpdateMatrixInputFromGraph();
        }

        private void ClearCanvas_Click(object sender, RoutedEventArgs e)
        {
            GraphCanvas.Children.Clear();
            nodes.Clear();
            nodeById.Clear();
            edges.Clear();
            edgeWeights.Clear();
            weights.Clear();
            nodeCounter = 0;
            ResultBlock.Text = "";
            MatrixInput.Clear();
            highlightedLines.ForEach(line => line.Stroke = Brushes.Black);
            highlightedLines.Clear();
            MatrixWithLabels.Clear();
        }

        private void ToggleInputMode_Click(object sender, RoutedEventArgs e)
        {
            useMatrixInput = !useMatrixInput;
            GraphCanvas.Visibility = useMatrixInput ? Visibility.Collapsed : Visibility.Visible;

            if (useMatrixInput)
            {
                UpdateMatrixInputFromGraph();
            }
        }

        private void UpdateMatrixInputFromGraph()
        {
            var matrix = new string[nodeCounter];
            for (int i = 0; i < nodeCounter; i++)
            {
                var row = new string[nodeCounter];
                for (int j = 0; j < nodeCounter; j++)
                {
                    if (edgeWeights.ContainsKey((i, j)))
                        row[j] = edgeWeights[(i, j)].weight.ToString();
                    else
                        row[j] = "-";
                }
                matrix[i] = string.Join(" ", row);
            }
            MatrixInput.Text = string.Join("\n", matrix);
        }

        private void RunMaxPath_Click(object sender, RoutedEventArgs e)
        {
            MatrixWithLabels.Clear();
            highlightedLines.ForEach(line => line.Stroke = Brushes.Black);
            highlightedLines.Clear();

            if (!int.TryParse(StartVertexBox.Text, out int startVertex))
            {
                MessageBox.Show("Введите корректную начальную вершину.");
                return;
            }
            if (useMatrixInput)
            {
                RebuildGraphFromMatrix();
            }

            var result = FindMaxPathsFrom(startVertex);
            ResultBlock.Text = string.Join("\n", result);
        }

        private List<string> FindMaxPathsFrom(int start)
        {
            var maxDist = new Dictionary<int, double>();
            var paths = new Dictionary<int, List<int>>();

            foreach (var node in nodes.Values)
            {
                maxDist[node] = double.NegativeInfinity;
                paths[node] = new List<int>();
            }

            maxDist[start] = 0;
            paths[start] = new List<int> { start };

            DFS(start, 0, new HashSet<int>(), new List<int> { start }, maxDist, paths);

            var result = new List<string>();

            foreach (var node in nodes.Values)
            {
                if (maxDist[node] == double.NegativeInfinity)
                {
                    result.Add($"Вершина {node}: недостижима");
                }
                else
                {
                    result.Add($"Вершина {node}: макс. длина = {maxDist[node]}, путь: {string.Join(" → ", paths[node])}");

                    var path = paths[node];
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        var key = (path[i], path[i + 1]);
                        if (edgeWeights.ContainsKey(key))
                        {
                            var line = edgeWeights[key].line;
                            if (line != null)
                            {
                                line.Stroke = Brushes.OrangeRed;
                                highlightedLines.Add(line);
                            }
                        }
                    }
                }
            }

            return result;
        }

        private void DFS(int current, double distSoFar, HashSet<int> visited, List<int> pathSoFar,
                         Dictionary<int, double> maxDist, Dictionary<int, List<int>> paths)
        {
            visited.Add(current);

            foreach (var edge in edgeWeights.Where(e => e.Key.Item1 == current))
            {
                int neighbor = edge.Key.Item2;
                double weight = edge.Value.weight;

                if (visited.Contains(neighbor)) continue;

                double newDist = distSoFar + weight;

                if (newDist > maxDist[neighbor])
                {
                    maxDist[neighbor] = newDist;
                    paths[neighbor] = new List<int>(pathSoFar) { neighbor };
                }

                DFS(neighbor, newDist, new HashSet<int>(visited), new List<int>(pathSoFar) { neighbor }, maxDist, paths);
            }
        }

        private void RunDijkstra_Click(object sender, RoutedEventArgs e)
        {
            highlightedLines.ForEach(line => line.Stroke = Brushes.Black);
            highlightedLines.Clear();

            if (!int.TryParse(StartVertexBox.Text, out int startVertex))
            {
                MessageBox.Show("Введите корректную начальную вершину.");
                return;
            }

            if (useMatrixInput)
            {
                RebuildGraphFromMatrix();
            }

            var result = DijkstraAlgorithm(startVertex);
            ResultBlock.Text = string.Join("\n", result);
        }

        private List<string> DijkstraAlgorithm(int start)
        {
            var dist = new Dictionary<int, double>();
            var prev = new Dictionary<int, int?>();
            var unvisited = new HashSet<int>();
            var visitedOrder = new List<int>();
            int size = nodes.Values.Count;

            foreach (var node in nodes.Values)
            {
                dist[node] = double.MaxValue;
                prev[node] = null;
                unvisited.Add(node);
            }
            dist[start] = 0;

            var steps = new List<string>();

            // Добавляем начальное состояние
            string initialLine = "";
            for (int i = 0; i < size; i++)
            {
                initialLine += (i == start) ? "[0] " : "∞ ";
            }
            steps.Add(initialLine.Trim());

            while (unvisited.Count > 0)
            {
                int current = unvisited.OrderBy(n => dist[n]).First();
                unvisited.Remove(current);
                visitedOrder.Add(current);

                foreach (var edge in edgeWeights.Where(e => e.Key.Item1 == current))
                {
                    int neighbor = edge.Key.Item2;
                    if (!unvisited.Contains(neighbor)) continue;

                    double alt = dist[current] + edge.Value.weight;
                    if (alt < dist[neighbor])
                    {
                        dist[neighbor] = alt;
                        prev[neighbor] = current;
                    }
                }

                int? minUnvisited = unvisited.OrderBy(n => dist[n]).FirstOrDefault();
                string line = "";
                for (int i = 0; i < size; i++)
                {
                    if (visitedOrder.Contains(i))
                    {
                        line += "- ";
                    }
                    else if (i == minUnvisited)
                    {
                        line += "[" + (dist[i] == double.MaxValue ? "∞" : dist[i].ToString()) + "] ";
                    }
                    else
                    {
                        line += (dist[i] == double.MaxValue ? "∞" : dist[i].ToString()) + " ";
                    }
                }

                if (line.Any(c => char.IsDigit(c) || c == '∞'))
                {
                    steps.Add(line.Trim());
                }

            }


            MatrixWithLabels.Text = string.Join("\n", steps);

            // Формируем список кратчайших путей
            var results = new List<string>();
            foreach (var node in nodes.Values.OrderBy(v => v))
            {
                if (node == start) continue;

                if (dist[node] == double.MaxValue)
                {
                    results.Add($"Вершина {node}: недостижима");
                }
                else
                {
                    var path = new List<int>();
                    int? current = node;
                    while (current != null)
                    {
                        path.Insert(0, current.Value);
                        current = prev[current.Value];
                    }
                    results.Add($"Вершина {node}: расстояние = {dist[node]}, путь: {string.Join(" → ", path)}");
                }
            }

            return results;
        }




        private void InsertInfinity_Click(object sender, RoutedEventArgs e)
        {
            int caret = MatrixInput.CaretIndex;
            MatrixInput.Text = MatrixInput.Text.Insert(caret, "∞");
            MatrixInput.CaretIndex = caret + 1;
            MatrixInput.Focus();
        }

        private void RebuildGraphFromMatrix()
        {
            edgeWeights.Clear();
            string[] lines = MatrixInput.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int n = lines.Length;

            nodes.Clear();
            nodeById.Clear();
            for (int i = 0; i < n; i++)
            {
                nodes[new Ellipse()] = i;
                nodeById[i] = null;
            }

            for (int i = 0; i < n; i++)
            {
                string[] parts = lines[i].Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                for (int j = 0; j < parts.Length; j++)
                {
                    string val = parts[j].ToLower();
                    if (val != "-" && val != "∞" && val != "inf")
                    {
                        if (double.TryParse(val, out double w))
                            edgeWeights[(i, j)] = (w, null);
                    }
                }
            }
            nodeCounter = n;
        }

    }
}