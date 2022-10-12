using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    /// Interaction logic for FastTreeView.xaml
    /// </summary>
    public partial class FastTreeView : UserControl
    {
        public FastTreeView()
        {
            InitializeComponent();
            _root.DataContext = this;


            Inclusive.Expanded += Inclusive_Expanded;
            Inclusive.Collapsed += Inclusive_Collapsed;
            CurrentView = new ObservableCollection<Sive>();

            (CurrentView as INotifyCollectionChanged).CollectionChanged += FastTreeView_CollectionChanged;

            _ic.SelectionChanged += _ic_SelectionChanged;


        }

        void ItemDoubleClick( object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            var lbi = sender as ListBoxItem;
            var inc = lbi.Content as Inclusive;
            if (inc != null)
                inc.IsExpanded = !inc.IsExpanded;
        }

        public event SelectionChangedEventHandler SelectionChanged;
        void _ic_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectionChanged != null)
                SelectionChanged(sender, e);
        }

        void Inclusive_Collapsed(object sender, EventArgs e)
        {
            Collapse(sender as Inclusive);
        }

        void Inclusive_Expanded(object sender, EventArgs e)
        {
            var inc = sender as Inclusive;
            Expand(inc);
        }
        void Expand( Inclusive inc )
        {
            var lbi = _ic.ItemContainerGenerator.ContainerFromItem(inc) as ListBoxItem;
            //if (lbi == null)
            {
                //(inc.Parent as Inclusive).IsExpanded = true;
                inc.ExpandParent();
            }

            var index = CurrentView.IndexOf(inc);
            //Debug.Assert(index != -1);
            if (index != -1)
            {
                foreach (var c in inc.Nodes)
                {
                    CurrentView.Insert(++index, c);
                }
            }
        }

        public void SelectAndFocus( Sive inc )
        {
            _ic.SelectedItem = inc;

            _ic.ScrollIntoView(inc);

            var scrollViewer = (VisualTreeHelper.GetChild(_ic, 0) as Border).Child as ScrollViewer;
            scrollViewer.ScrollToRightEnd();


            var container = _ic.ItemContainerGenerator.ContainerFromItem(inc) as ListBoxItem;
            if (container != null)
            {
                container.IsSelected = true;
                container.Focus();
            }

            //Dispatcher.BeginInvoke(new Action(()
            //    =>
            //{
            //    var container = _ic.ItemContainerGenerator.ContainerFromItem(inc) as ListBoxItem;
            //    if (container != null)
            //    {
            //        container.IsSelected = true;
            //        container.Focus();
            //    }
            //}), System.Windows.Threading.DispatcherPriority.Background);

            //var container = _ic.ItemContainerGenerator.ContainerFromItem(inc) as ListBoxItem;
            //if (container == null)
            //{
            //    UpdateLayout();
            //    container = _ic.ItemContainerGenerator.ContainerFromItem(inc) as ListBoxItem;
            //}

            //if( container != null)
            //{
            //    container.IsSelected = true;
            //    container.Focus();
            //}
        }

        void Collapse(Inclusive inc)
        {
            //var lbi = _ic.ItemContainerGenerator.ContainerFromItem(inc) as ListBoxItem;
            //if (lbi == null)
            //    Collapse(inc.Parent as Inclusive);
            //lbi = _ic.ItemContainerGenerator.ContainerFromItem(inc) as ListBoxItem;
            //var parentContainer = _ic.ItemContainerGenerator.ContainerFromItem(inc.Parent as Inclusive) as ListBoxItem;

            //var index = CurrentView.IndexOf(inc);
            //foreach (var c in inc.Nodes)
            //{
            //    CurrentView.Insert(++index, c);
            //}

            var depth = inc.Level;// Depth;
            inc.IsExpanded = false;
            var index = CurrentView.IndexOf(inc);
            var current = CurrentView[++index];

            while (index <= CurrentView.Count - 1 && CurrentView[index].Level > depth)
            {
                CurrentView.RemoveAt(index);
            }
        }

        void FastTreeView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

        }


        public Inclusive Root
        {
            get { return (Inclusive)GetValue(RootProperty); }
            set { SetValue(RootProperty, value); }
        }
        public static readonly DependencyProperty RootProperty =
            DependencyProperty.Register("Root", typeof(Inclusive), typeof(FastTreeView), new PropertyMetadata(null, RootChangedStatic));

        static void RootChangedStatic( DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as FastTreeView).RootChanged(e.NewValue as Inclusive);
        }
        void RootChanged( Inclusive inc)
        {
            CurrentView.Clear();

            Inclusive.Root = inc;

            if (inc == null)
                return;

            inc.CollapseDeep();

            foreach( var i in inc.Nodes )
            {
                //if( i is Inclusive)
                {
                    CurrentView.Add(i);
                }
            }
        }


        public ObservableCollection<Sive> CurrentView
        {
            get { return (ObservableCollection<Sive>)GetValue(CurrentViewProperty); }
            private set { SetValue(CurrentViewProperty, value); }
        }
        public static readonly DependencyProperty CurrentViewProperty =
            DependencyProperty.Register("CurrentView", typeof(ObservableCollection<Sive>), typeof(FastTreeView), new PropertyMetadata(null));

        private void _ic_KeyUp(object sender, KeyEventArgs e)
        {
            var lbi = e.OriginalSource as ListBoxItem;
            if (lbi == null) return;

            var inc = lbi.Content as Inclusive;
            var exc = lbi.Content as Exclusive;
            //if (inc == null) return;

            if (e.Key != Key.Right && e.Key != Key.Left || Keyboard.Modifiers != ModifierKeys.None)
                return;

            e.Handled = true;

            if (e.Key == Key.Right && inc != null)
            {
                inc.IsExpanded = true;
            }
            else if (e.Key == Key.Left)
            {
                if (inc != null && inc.IsExpanded)
                {
                    inc.IsExpanded = false;
                }
                else if( inc != null || exc != null )
                {
                    var sive = lbi.Content as Sive;
                    (sive.Parent as Inclusive).IsExpanded = false;
                    SelectAndFocus(sive.Parent as Inclusive);
                }
            }

            //var lbi = e.OriginalSource as ListBoxItem;
            //if (lbi == null) return;

            //var inc = lbi.Content as Inclusive;
            //if (inc == null) return;

            //if (e.Key == Key.Right && !inc.IsExpanded)
            //{
            //    inc.IsExpanded = true;
            //    var index = CurrentView.IndexOf(inc);
            //    foreach( var c in inc.Nodes)
            //    {
            //        CurrentView.Insert(++index, c);
            //    }
            //}
            //else if( e.Key == Key.Left && inc.IsExpanded)
            //{
            //    var depth = inc.Depth;
            //    inc.IsExpanded = false;
            //    var index = CurrentView.IndexOf(inc);
            //    var current = CurrentView[++index];

            //    while(index <= CurrentView.Count-1 && CurrentView[index].Depth > depth)
            //    {
            //        CurrentView.RemoveAt(index);
            //    }
            //}
        }

        

        
    }
}
