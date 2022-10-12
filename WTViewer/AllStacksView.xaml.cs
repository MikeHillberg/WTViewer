using System;
using System.Collections.Generic;
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
    /// Interaction logic for AllStacksView.xaml
    /// </summary>
    public partial class AllStacksView : UserControl
    {
        public AllStacksView()
        {
            InitializeComponent();
            _root.DataContext = this;
        }




        public string MethodName
        {
            get { return (string)GetValue(MethodNameProperty); }
            set { SetValue(MethodNameProperty, value); }
        }
        public static readonly DependencyProperty MethodNameProperty =
            DependencyProperty.Register("MethodName", typeof(string), typeof(AllStacksView), new PropertyMetadata(null, MethodNameChangedStatic));
        static void MethodNameChangedStatic(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as AllStacksView).MethodNameChanged(e.NewValue as string);
        }
        

        void MethodNameChanged(string name )
        {
            SelectedStacks = AggregateStacks(name);
        }

        IEnumerable<Sive> AggregateStacks(string name)
        {
            var matches = new List<Inclusive>();
            var regex = MainWindow.GetRegex(name);
            Sive current = null;

            MainWindow.Find(MainWindow.StaticRoot, ref current, regex, matches, 0, null);

            //var f = from m in matches
            //        select m.CallStack;
            //SelectedStacks = f.ToList();

            foreach (var m in matches)
            {
                foreach (var sive in m.CallStack)
                {
                    yield return sive;
                }
                yield return null;
            }

        }

        public IEnumerable<Sive> SelectedStacks
        {
            get { return (IEnumerable<Sive>)GetValue(SelectedStacksProperty); }
            set { SetValue(SelectedStacksProperty, value); }
        }
        public static readonly DependencyProperty SelectedStacksProperty =
            DependencyProperty.Register("SelectedStacks", typeof(IEnumerable<Sive>), typeof(AllStacksView), new PropertyMetadata(null));

    
    
    
    }
}
