using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WTViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();


            Stack = new ObservableCollection<object>();
            _stackHeader.Visibility = Visibility.Collapsed;
            Stack.CollectionChanged += (s, e) =>
                {
                    if (Stack.Count > 0)
                        _stackHeader.Visibility = Visibility.Visible;
                    else
                        _stackHeader.Visibility = Visibility.Collapsed;
                };


            LoadRegistry();
            string file = null;
            if (RecentFiles.Count > 0)
                file = RecentFiles[0];

            if (!string.IsNullOrEmpty(file) && File.Exists(file))
            {
                LoadFile(file); // Update Root
            }
            DataContext = this;

            //EventManager.RegisterClassHandler(typeof(TreeViewItem), TreeViewItem.SelectedEvent,
            //    new RoutedEventHandler(SelectionChanged));

            _ftc.SelectionChanged += _ftc_SelectionChanged;

            EventManager.RegisterClassHandler(typeof(TreeViewItem), TreeViewItem.MouseRightButtonUpEvent,
                new RoutedEventHandler(RightClick));

            this.KeyDown += (s, e) =>
                {
                    if ((e.Key == Key.F || e.Key == Key.E) && Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        _search.Focus();
                        _search.Text = "";
                        _searchCost.Text = "";
                        _searchAncestor.Text = "";
                    }

                    else if (e.Key == Key.F3 && Keyboard.Modifiers == 0)
                        FindNext(null, null);

                    else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
                        DoCopy();

                    else if (e.Key == Key.F10 && Keyboard.Modifiers == ModifierKeys.None)
                        DoStep();
                    else if (e.Key == Key.R && Keyboard.Modifiers == ModifierKeys.Control)
                        SetRoot(null, null);
                    else if (e.Key == Key.R && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                        ResetRoot(null, null);
                };

            Loaded += (s, e) =>
                {
                    // Delay to ensure this window goes on top of the main Window
                    Dispatcher.BeginInvoke(new Action(()
                    =>
                    {
                        Help(null,null);
                    }), System.Windows.Threading.DispatcherPriority.Background);
                };

        }

        void _ftc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged(sender, e);
        }



        public int TotalAllocations
        {
            get { return (int)GetValue(TotalAllocationsProperty); }
            set { SetValue(TotalAllocationsProperty, value); }
        }
        public static readonly DependencyProperty TotalAllocationsProperty =
            DependencyProperty.Register("TotalAllocations", typeof(int), typeof(MainWindow), new PropertyMetadata(0));


        public int NetAllocations
        {
            get { return (int)GetValue(NetAllocationsProperty); }
            set { SetValue(NetAllocationsProperty, value); }
        }
        public static readonly DependencyProperty NetAllocationsProperty =
            DependencyProperty.Register("NetAllocations", typeof(int), typeof(MainWindow), new PropertyMetadata(0));



        public string WindowTitle
        {
            get { return (string)GetValue(WindowTitleProperty); }
            set { SetValue(WindowTitleProperty, value); }
        }
        public static readonly DependencyProperty WindowTitleProperty =
            DependencyProperty.Register("WindowTitle", typeof(string), typeof(MainWindow), new PropertyMetadata("WT Viewer"));




        const string _regKeyName = @"Software\ToolboxWTViewer";
        const string _mruKeyValueName = "MRU";

        void LoadRegistry()
        {
            var key = Registry.CurrentUser.OpenSubKey(_regKeyName);
            if (key != null)
            {
                var mruFileNames = key.GetValue(_mruKeyValueName, null) as string[];
                foreach (var f in mruFileNames)
                    RecentFiles.Add(f);
            }

        }

        void DoStep()
        {

        }

        ListBoxItem _selectedTVI = null;
        Sive _selectedItem = null;

        void SelectionChanged(object sender, RoutedEventArgs e)
        {
            //var tvi = sender as ListBoxItem;

            var sive = (sender as ListBox).SelectedItem as Sive;
            if (sive == null) return;

            if (_selectedItem != null)
                _selectedItem.IsSelected = false;

            _selectedItem = sive;
            _selectedItem.IsSelected = true;

            //if (_selectedTVI != null && _selectedTVI.Content != null)
            //{
            //    (_selectedTVI.Content as Sive).IsSelected = false;
            //}

            var listBox = sender as ListBox;
            _selectedTVI = listBox.ItemContainerGenerator.ContainerFromItem(sive) as ListBoxItem;
            if (_selectedTVI != null)
                (_selectedTVI.Content as Sive).IsSelected = true;

            //if (tvi.Content is Sive && tvi.IsSelected)
            {
                Stack.Clear();
                //_selectedTVI = tvi;
                SelectedItem = sive; // tvi.Content as Sive;
                //var sive = tvi.Content as Sive;

                while (sive != null)
                {
                    Stack.Add(sive);
                    sive = sive.Parent;
                }

            }

            int count = 0;
            foreach (var item in (sender as ListBox).SelectedItems)
            {
                sive = item as Sive;
                count += sive.Count;
            }

            SelectedTotalCount = count;

        }



        public int SelectedTotalCount
        {
            get { return (int)GetValue(SelectedTotalCountProperty); }
            set { SetValue(SelectedTotalCountProperty, value); }
        }
        public static readonly DependencyProperty SelectedTotalCountProperty =
            DependencyProperty.Register("SelectedTotalCount", typeof(int), typeof(MainWindow), new PropertyMetadata(0));



        public Sive SelectedItem
        {
            get { return (Sive)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(Sive), typeof(MainWindow), new PropertyMetadata(null, SelectedItemChangedStatic));
        static void SelectedItemChangedStatic(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MainWindow).SelectedItemVisibility = e.NewValue == null ? Visibility.Collapsed : Visibility.Visible;
        }


        public ObservableCollection<object> Stack
        {
            get { return (ObservableCollection<object>)GetValue(StackProperty); }
            set { SetValue(StackProperty, value); }
        }

        public static readonly DependencyProperty StackProperty =
            DependencyProperty.Register("Stack", typeof(ObservableCollection<object>), typeof(MainWindow), new PropertyMetadata(null));

        void RightClick(object sender, RoutedEventArgs e)
        {
            //var tvi = sender as TreeViewItem;
            //var inc = tvi.Header as Inclusive;

            //e.Handled = true;


            //int hits = 0;
            //var count = CountInclusive(Root, inc.Name, ref hits);
            //MessageBox.Show(hits.ToString() + " hits, " + count.ToString() + " instructions");

        }

        static public Regex GetRegex(string s)
        {
            return new Regex(s, RegexOptions.IgnoreCase);
        }

        void CustomCount(object sender, RoutedEventArgs e)
        {
            int hits = 0;

            var matches = new Dictionary<string, FindResult>();

            int minCost = 0;
            int.TryParse(_searchCost.Text, out minCost);


            int count = 0;
            var hitsIncludingNesting = 0;
            CountInclusive(Root, GetRegex(_search.Text), minCost, _searchAncestor.Text.ToUpper(),
                out count,
                out hits,
                out hitsIncludingNesting,
                matches);

            var findResults = new FindResults();
            findResults.Results = from m in matches
                                  orderby m.Value.Count descending
                                  select m.Value;
            findResults.TotalCount = count;

            _popupBorderContent.Child = findResults;
            _popupBorder.Visibility = Visibility.Visible;
            _popupBorderTitle.Text = "Stacks for \"" + _search.Text;

            //var dialog = new Window();
            //dialog.Title = "Stacks for \"" + _search.Text + "\" [" + _filename + "]";
            //dialog.Content = findResults;
            //dialog.Show();
        }



        static public void CountInclusive(Inclusive root, Regex regex, int minCost, string ancestor,
            out int count,
            out int rootHits,
            out int totalHits,
            Dictionary<string, FindResult> matches = null)
        {
            count = rootHits = totalHits = 0;

            CountInclusivePrivate(root, regex, minCost, ancestor,
                false, // nesting
                ref count, ref rootHits, ref totalHits,
                matches);

            return;
        }


        static private void CountInclusivePrivate(Inclusive root, Regex regex, int minCost, string ancestor,
            bool nesting,
            ref int count,
            ref int rootHits,
            ref int totalHits,
            Dictionary<string, FindResult> matches)
        {
            for (int i = 0; i < root.Nodes.Count; i++)
            {
                bool alreadyCounted = false;
                var node = root.Nodes[i] as Inclusive;
                if (node != null)
                {
                    var name = node.Name.ToUpper();
                    if (!string.IsNullOrEmpty(ancestor))
                    {
                        if (name.Contains(ancestor))
                            ancestor = null;
                    }
                    else if (regex.Match(name) != Match.Empty && node.Count >= minCost)
                    {
                        totalHits++;
                        if (!nesting)
                        {
                            count += node.Count;
                            rootHits++;

                            CountInclusivePrivate(node, regex, minCost, ancestor,
                                true, // nesting
                                ref count,
                                ref rootHits,
                                ref totalHits,
                                null); // matches
                            alreadyCounted = true;
                        }

                        if (matches != null)
                        {
                            FindResult findResult = null;
                            if (matches.TryGetValue(node.Name, out findResult))
                            {
                                findResult.Count += node.Count;
                                findResult.Hits++;
                            }
                            else
                            {
                                findResult = new FindResult() { Count = node.Count, Hits = 1, Name = node.Name };
                                matches[node.Name] = findResult;
                            }
                        }

                        //continue;
                    }

                    if (!alreadyCounted)
                    {
                        CountInclusivePrivate(node, regex, minCost, ancestor,
                            nesting, // nesting
                            ref count,
                            ref rootHits,
                            ref totalHits,
                            matches);
                    }
                }
            }

            //return count;
        }

        static public int CountExclusive(Inclusive root, Regex regex, ref int hits)
        {
            var count = 0;
            //s = s.ToUpper();

            for (int i = 0; i < root.Nodes.Count; i++)
            {
                var inc = root.Nodes[i] as Inclusive;
                var exc = root.Nodes[i] as Exclusive;

                if (inc != null)
                {
                    count += CountExclusive(inc, regex, ref hits);
                }
                else if (exc != null)
                {
                    //if (exc.Name.ToUpper().Contains(s))
                    if (regex.IsMatch(exc.Name))
                    {
                        count += exc.Count;
                        hits++;
                        continue;
                    }

                }
            }

            return count;
        }

        Inclusive _originalRoot = null;
        public Inclusive Root
        {
            get { return (Inclusive)GetValue(RootProperty); }
            set { SetValue(RootProperty, value); }
        }
        public static readonly DependencyProperty RootProperty =
            DependencyProperty.Register("Root", typeof(Inclusive), typeof(MainWindow), new PropertyMetadata(null, RootChanged));
        static int _rootChangedFork = 0;
        static void RootChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StaticRoot = e.NewValue as Inclusive;

            var This = d as MainWindow;

            int count = 0;
            int nestedTotal = 0;

            var totalAllocations = 0;
            var totalFrees = 0;

            var bw = new BackgroundWorker();
            var fork = ++_rootChangedFork;
            bw.DoWork += (s, e2) =>
                {
                    MainWindow.CountInclusive(MainWindow.StaticRoot, MainWindow.GetRegex("ntdll.*!RtlAllocateHeap"), 0, null,
                        out count,
                        out totalAllocations,
                        out nestedTotal);

                    MainWindow.CountInclusive(MainWindow.StaticRoot, MainWindow.GetRegex("ntdll.*!RtlFreeHeap"), 0, null,
                        out count,
                        out totalFrees,
                        out nestedTotal);
                };
            bw.RunWorkerCompleted += (s, e2) =>
                {
                    if (fork == _rootChangedFork)
                    {
                        This.TotalAllocations = totalAllocations;
                        This.NetAllocations = totalAllocations - totalFrees;
                    }
                };

            bw.RunWorkerAsync();
        }

        static public Inclusive StaticRoot { get; private set; }



        public string CurrentFile
        {
            get { return (string)GetValue(CurrentFileProperty); }
            set { SetValue(CurrentFileProperty, value); }
        }
        public static readonly DependencyProperty CurrentFileProperty =
            DependencyProperty.Register("CurrentFile", typeof(string), typeof(MainWindow), new PropertyMetadata(""));

        static char[] _emptyChar = new char[] { ' ' };

        string _filename;
        private void LoadFile(string filename)
        {
            _filename = filename;
            Line line = new Line();
            var reader = File.OpenText(filename);
            //CurrentFile = filename;
            WindowTitle = "WT Viewer - " + filename;
            var lineNumber = 0;

            _lines.Clear();
            _selectedTVI = null;
            SelectedItem = null;

            try
            {
                bool skip = false;
                bool inHeader = true;
                while (!reader.EndOfStream)
                {
                    line = new Line();
                    var original = reader.ReadLine();
                    var str = original.Trim();
                    line.SourceLineNumber = ++lineNumber;

                    if (string.IsNullOrEmpty(str))
                        continue;

                    if (inHeader)
                    {
                        if (original.Contains(" 0]"))
                        {
                            inHeader = false;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    // Look and skip for non-trace messages
                    if (original[0] != ' ')
                    {
                        if (str.StartsWith(">> More than one level popped"))
                        {
                            skip = true;
                        }

                        if (str.StartsWith(">> No match on ret")
                            ||
                            str.StartsWith("ModLoad:")
                            ||
                            str.StartsWith("Tracing ")
                            ||
                            str.StartsWith("Opened log")
                            ||
                            str.StartsWith("0:")
                            ||
                            str.StartsWith("Matched: ")
                            ||
                            str.StartsWith(">> More than one level popped"))
                        {
                            continue;
                        }

                        // 1710419 instructions were executed in 1710418 events (0 from other threads)
                        if (str.Contains("instructions were executed in"))
                            break;
                    }

                    var mainParts = str.Split(new string[] { " [", "] " }, StringSplitOptions.RemoveEmptyEntries);

                    var counts = mainParts[0].Trim().Split(_emptyChar, StringSplitOptions.RemoveEmptyEntries);
                    line.Outer = int.Parse(counts[0]);
                    line.Inner = int.Parse(counts[1]);

                    line.Level = int.Parse(mainParts[1].Trim());
                    line.Name = mainParts[2].Trim();

                    line.Skip = skip;
                    skip = false;

                    _lines.Add(line);

                }
            }
            catch (Exception)
            {
                var sb = new StringBuilder();
                sb.Append("Failed parsing ");
                //if( line != null )
                sb.AppendFormat("(line {0})", line.SourceLineNumber);

                MessageBox.Show(sb.ToString());
                return;
            }

            int index = 0;
            var root = Parse(_lines, 0, ref index);
            root.RunningTotal = 0;

            Root = _originalRoot = root;
            _lines.Clear();

        }

        int _depth = 0;
        Inclusive Parse(IList<Line> lines, int runningTotal, ref int index)
        {
            var level = lines[index].Level;
            var inclusive = new Inclusive();
            inclusive.Name = lines[index].Name;
            inclusive.Line = lines[index];
            //inclusive.Depth = _depth;
            inclusive.Level = level;

            int runningCount = 0;

            while (true)
            {
                if (index >= lines.Count)
                    break;

                var line = lines[index];

                if (lines[index].Level == level)
                {
                    var exclusive = new Exclusive()
                    {
                        Count = line.Outer - runningCount,
                        Parent = inclusive,
                        Level = line.Level,
                        Line = lines[index]
                    };

                    exclusive.Name = lines[index].Name;

                    if (exclusive.Name != inclusive.Name)
                    {
                        exclusive.IsInlined = true;
                        break;
                    }

                    inclusive.Nodes.Add(exclusive);

                    runningCount = line.Outer;

                    runningTotal += exclusive.Count;
                    exclusive.RunningTotal = runningTotal - exclusive.Count;

                    inclusive.Count += exclusive.Count;

                    index++;
                }
                else if (lines[index].Level > level)
                {

                    _depth++;
                    var sub = Parse(lines, runningTotal, ref index);
                    --_depth;
                    //sub.Depth = _depth;
                    sub.Level = level;
                    inclusive.Nodes.Add(sub);
                    sub.Parent = inclusive;

                    inclusive.Count += sub.Count;
                    runningTotal += sub.Count;
                    sub.RunningTotal = runningTotal - sub.Count;
                }
                else
                    break;

            }


            inclusive.RunningTotal = runningTotal;
            //inclusive.Count = runningCount;
            return inclusive;
        }

        List<Line> _lines = new List<Line>();

        //string _search = "Windows_UI_Xaml";
        private void FindNext(object sender, RoutedEventArgs e)
        {
            var header = SelectedItem;
            var list = new List<Inclusive>();
            int minCost = 0;
            int.TryParse(_searchCost.Text, out minCost);

            if (Find(Root, ref header, GetRegex(_search.Text), list, minCost, _searchAncestor.Text.ToUpper()))
            {
                SelectItem(list[0]);

                //list.Reverse();

                //Expand(_ic, list, 0);
            }
        }
        private void Find(object sender, RoutedEventArgs e)
        {
            Sive sive = null;
            var list = new List<Inclusive>();
            int minCost = 0;
            int.TryParse(_searchCost.Text, out minCost);

            if (Find(Root, ref sive, GetRegex(_search.Text), list, minCost, _searchAncestor.Text.ToUpper()))
            {
                SelectItem(list[0]);
                //list.Reverse();
                //Expand(_ic, list, 0);
            }
            else
                MessageBox.Show("Not found");


        }

        delegate void Foo();

        //void Expand(ItemsControl itemsControl, List<int> list, int index)
        //{
        //    var g = itemsControl.ItemContainerGenerator;

        //    var tvi = itemsControl as TreeViewItem;
        //    if (tvi != null)
        //    {
        //        if (!tvi.IsExpanded)
        //        {
        //            tvi.IsExpanded = true;
        //            tvi.Focus();

        //        }
        //    }





        //    //////
        //    var ii = itemsControl.Items[list[index]];
        //    tvi = g.ContainerFromItem(ii) as TreeViewItem;
        //    ///////

        //    if (index == list.Count - 1)
        //    {
        //        tvi.IsSelected = true;
        //        tvi.Focus();
        //    }
        //    else
        //        Expand(tvi, list, index + 1);

        //    return;
        //}


        ObservableCollection<string> _recentFiles = new ObservableCollection<string>();
        public ObservableCollection<string> RecentFiles
        {
            get { return _recentFiles; }
        }

        public static bool Find(Inclusive root, ref Sive current, Regex regex, List<Inclusive> list, int minCost, string ancestor)
        {
            for (int i = 0; i < root.Nodes.Count; i++)
            {
                var node = root.Nodes[i];
                if (node is Exclusive)
                {
                    if (current != null)
                    {
                        if (current == node as Exclusive)
                            current = null;
                    }
                }

                else if (node is Inclusive)
                {
                    var inc = node as Inclusive;
                    var ancestorT = ancestor;

                    if (current != null)
                    {
                        if (current == inc)
                            current = null;
                    }
                    else if (!string.IsNullOrEmpty(ancestorT))
                    {
                        if (inc.Name.ToUpper().Contains(ancestorT))
                        {
                            ancestorT = null;
                        }
                    }
                    else if (regex.IsMatch(inc.Name) && inc.Count >= minCost)
                    {
                        if (list != null)
                            list.Add(inc);

                        //SelectItem(inc);

                        //return true;
                    }

                    if (Find(inc, ref current, regex, list, minCost, ancestorT))
                    {
                        //if( list != null )
                        //    list.Add(inc);

                        //return true;
                    }
                }
            }

            return list.Count != 0;
        }

        void SelectItem(Inclusive inc)
        {
            inc.IsExpanded = true;

            Dispatcher.BeginInvoke(new Action(()
                =>
            {
                _ftc.SelectAndFocus(inc);
            }), System.Windows.Threading.DispatcherPriority.Background);


        }

        private void _ic_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void FileOpen(object sender, RoutedEventArgs e)
        {
            var ofn = new OpenFileDialog();
            ofn.Filter = "Text files|*.txt|WT trace files|*.wt|All files|*";
            if (true == ofn.ShowDialog())
            {
                LoadFile(ofn.FileName); // Root

                _recentFiles.Insert(0, ofn.FileName);
                while (_recentFiles.Count > 5)
                {
                    _recentFiles.RemoveAt(_recentFiles.Count - 1);
                }

                UpdateRegistry();
            }
        }

        void UpdateRegistry()
        {
            var key = Registry.CurrentUser.CreateSubKey(_regKeyName);
            key.SetValue(_mruKeyValueName, _recentFiles.ToArray());
        }

        private void _search_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers == ModifierKeys.None)
                    Find(null, null);
                else if (Keyboard.Modifiers == ModifierKeys.Shift)
                    CustomCount(null, null);

            }
        }

        void DoCopy()
        {
            var listBox = _stack;

            var text = new StringBuilder();

            foreach (var selectedItem in listBox.SelectedItems)
            {
                dynamic item = selectedItem;
                text.AppendLine(item.Name);
            }

            Clipboard.SetText(text.ToString());

        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void RecentOpen(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource != sender)
            {
                var filename = (e.OriginalSource as MenuItem).Header as string;
                if (string.IsNullOrEmpty(filename))
                    return;

                var loaded = false;
                if (File.Exists(filename))
                {
                    LoadFile(filename); // Root
                    loaded = true;
                }
                else
                {
                    MessageBox.Show("File not found");
                }


                var index = _recentFiles.IndexOf(filename);
                _recentFiles.RemoveAt(index);

                if (loaded)
                    _recentFiles.Insert(0, filename);

                UpdateRegistry();
            }
        }

        private void SetRoot(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null)
                return;

            Root.IsFakeRoot = false;

            var sive = _selectedItem;// _selectedTVI.Content as Sive;
            var inc = sive as Inclusive;
            if (inc == null)
            {
                var exc = sive as Exclusive;
                inc = exc.Parent as Inclusive;
            }

            inc.IsFakeRoot = true;
            Root = inc;

            SelectItem(Root);
        }

        private void ResetRoot(object sender, RoutedEventArgs e)
        {
            Root.IsFakeRoot = false;
            Root = _originalRoot;
            SelectItem(Root);
        }

        private void ShowAllCallers(object sender, RoutedEventArgs e)
        {
            var a = new AllStacksView();
            a.MethodName = SelectedItem.Name;

            _popupBorderContent.Child = a;
            _popupBorder.Visibility = Visibility.Visible;
            _popupBorderTitle.Text = "Stacks for " + a.MethodName;

            //var dialog = new Window();
            //dialog.Title = "Stacks for " + a.MethodName + " [" + _filename + "]";
            //dialog.Content = a;
            //dialog.Show();
        }



        public Visibility SelectedItemVisibility
        {
            get { return (Visibility)GetValue(SelectedItemVisibilityProperty); }
            set { SetValue(SelectedItemVisibilityProperty, value); }
        }
        public static readonly DependencyProperty SelectedItemVisibilityProperty =
            DependencyProperty.Register("SelectedItemVisibility", typeof(Visibility), typeof(MainWindow), new PropertyMetadata(Visibility.Collapsed));

        private void ShowAllocationStacks(object sender, RoutedEventArgs e)
        {
            var a = new AllStacksView();
            a.MethodName = "ntdll.*!RtlAllocateHeap";

            _popupBorderContent.Child = a;
            _popupBorder.Visibility = Visibility.Visible;
            _popupBorderTitle.Text = "All allocations";

            //var dialog = new Window();
            //dialog.Title = "All allocations [" + _filename + "]";
            //dialog.Content = a;
            //dialog.Show();
        }

        private void ClosePopup(object sender, RoutedEventArgs e)
        {
            _popupBorderContent.Child = null;
            _popupBorder.Visibility = Visibility.Collapsed;
        }

        private void Help(object sender, RoutedEventArgs e)
        {
            //new HelpDisplay().ShowDialog();
            var help = new HelpDisplay();
            help.Owner = this;
            help.ShowDialog();
        }
    }


    public class FindResult
    {
        public int Count { get; set; }
        public int Hits { get; set; }
        public string Name { get; set; }
    }



}
