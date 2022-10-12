using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for FindResults.xaml
    /// </summary>
    public partial class FindResults : UserControl
    {
        public FindResults()
        {
            InitializeComponent();
            DataContext = this;
        }




        public FindResult SelectedItem
        {
            get { return (FindResult)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(FindResult), typeof(FindResults), new PropertyMetadata(null, SelectedItemChangedStatic));
        static void SelectedItemChangedStatic( DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as FindResults).SelectedItemChanged( e.NewValue as FindResult);
        }
        void SelectedItemChanged( FindResult findResult)
        {
            //var matches = new List<Inclusive>();
            //var regex = MainWindow.GetRegex(findResult.Name);
            //Sive current = null;

            //MainWindow.Find(MainWindow.StaticRoot, ref current, regex, matches, 0, null);

            //var f = from m in matches
            //        select m.CallStack;
            //SelectedStacks = f.ToList();
        }



        //public IEnumerable<IEnumerable<Sive>> SelectedStacks
        //{
        //    get { return (IEnumerable<IEnumerable<Sive>>)GetValue(SelectedStacksProperty); }
        //    set { SetValue(SelectedStacksProperty, value); }
        //}
        //public static readonly DependencyProperty SelectedStacksProperty =
        //    DependencyProperty.Register("SelectedStacks", typeof(IEnumerable<IEnumerable<Sive>>), typeof(FindResults), new PropertyMetadata(null));

        
        

        public int TotalCount
        {
            get { return (int)GetValue(TotalCountProperty); }
            set { SetValue(TotalCountProperty, value); }
        }
        public static readonly DependencyProperty TotalCountProperty =
            DependencyProperty.Register("TotalCount", typeof(int), typeof(FindResults), new PropertyMetadata(0));

        

        public IEnumerable<FindResult> Results
        {
            get { return (IEnumerable<FindResult>)GetValue(ResultsProperty); }
            set { SetValue(ResultsProperty, value); }
        }
        public static readonly DependencyProperty ResultsProperty =
            DependencyProperty.Register("Results", typeof(IEnumerable<FindResult>), typeof(FindResults), new PropertyMetadata(null));

        
    }
}
