using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WTViewer
{


    abstract public class Sive : INotifyPropertyChanged
    {
        public string Name { get; set; }

        string _friendlyName = null;

        bool _isSelected = false;
        public bool IsSelected
        {
            get { return _isSelected;  }
            set
            {
                _isSelected = value;
                RaiseChildChanged(this);

                var parent = Parent;
                while(parent != null)
                {
                    parent.IsSelectionAncestor = value;
                    parent.RaisePropertyChanged();
                    parent = parent.Parent;
                }
            }
        }

        public bool IsSelectionAncestor
        {
            get;
            set;
        }

        void RaiseChildChanged( Sive sive)
        {
            sive.RaisePropertyChanged();
            var inc = sive as Inclusive;
            if( inc == null )
                return;

            foreach( var child in inc.Nodes)
            {
                RaiseChildChanged(child);
            }
        }

        public bool InSelectionScope
        {
            get
            {
                var parent = this;
                while( parent != null)
                {
                    if (parent.IsSelected)
                        return true;
                    parent = parent.Parent;
                }

                return false;
            }
        }

        public Line Line { get; set; }
        public string FriendlyName
        {
            get
            {
                if (_friendlyName == null)
                {
                    _friendlyName = Name;
                    var rootDll = MainWindow.StaticRoot.Name.Split('!')[0];
                    if (Name.StartsWith(rootDll))
                    {
                        _friendlyName = Name.Substring(rootDll.Length + 1);
                    }

                    _friendlyName = CleanParameterizedName(_friendlyName);
                    Debug.Assert(!string.IsNullOrEmpty(_friendlyName));
                }
                return _friendlyName;
            }
        }

        public IEnumerable<Sive> CallStack
        {
            get
            {
                return GetCallStack().ToList();
            }
        }
        IEnumerable<Sive> GetCallStack()
        {
            var parent = this;
            while (parent != null)
            {
                yield return parent;
                parent = parent.Parent;
            }
        }

        string CleanParameterizedName(string name)
        {
            var start = name.IndexOf('<');
            if (start == -1)
                return name;

            var end = name.LastIndexOf('>');
            if (end == -1)
                return name;

            return name.Substring(0, start) + "<..." + name.Substring(end);
        }

        Inclusive _parent = null;
        public Inclusive Parent 
        { 
            get
            {
                if (IsFakeRoot)
                    return null;
                else
                    return _parent;
            }
            set
            {
                _parent = value;
            }
        }
        public int Count { get; set; }
        public string CountString
        {
            get
            {
                return String.Format("{0:N0}", Count);
            }
        }
        public int RunningTotal { get; set; }
        public string RunningTotalString
        {
            get
            {
                return String.Format("{0:N0}", RunningTotal);
            }
        }


        public Thickness Margin
        {
            get
            {
                int deltaDepth = Level - Inclusive.Root.Level;
                return new Thickness(deltaDepth * 30, 0, 0, 0);
            }
        }

        bool _isFakeRoot = false;
        public bool IsFakeRoot 
        {
            get { return _isFakeRoot;  }
            set { _isFakeRoot = value; RaisePropertyChanged(); }
        }

        public int Level { get; set; }

        int _count = 0;
        string _countString = null;
        int _calls = 0;
        public string TotalCount
        {
            get
            {
                if (_count == 0)
                {
                    var s = "^" + Name + "$";
                    if (this is Inclusive)
                    {
                        int callsIncludingNested;
                        MainWindow.CountInclusive(MainWindow.StaticRoot, MainWindow.GetRegex(s), 0, null,
                                out _count,
                                out _calls,
                                out callsIncludingNested);

                        if (callsIncludingNested > _calls)
                        {
                            _countString = String.Format("{0:N0} inclusive instructions across {1} calls, not including recursive calls ({2} calls total)",
                                        _count, _calls, callsIncludingNested);
                        }
                        else
                        {
                            _countString = String.Format("{0:N0} inclusive instructions across {1} calls (no recursive calls)",
                                        _count, _calls);
                        }
                    }
                    else
                    {
                        _count = MainWindow.CountExclusive(MainWindow.StaticRoot, MainWindow.GetRegex(s), ref _calls);
                        _countString = String.Format("{0:N0} exclusive instructions across {1} chunks",
                                    _count, _calls);
                    }

                }

                return _countString;
            }


        }


        public void ExpandParent()
        {
            if( _parent != null )
                _parent.IsExpanded = true;
        }

        static PropertyChangedEventArgs _emptyArgs = new PropertyChangedEventArgs("");
        protected void RaisePropertyChanged()
        {
            if (PropertyChanged != null)
                PropertyChanged(this, _emptyArgs);
        }

        //private void RaisePropertyChanged([CallerMemberName] string caller = "")
        //{
        //    if (PropertyChanged != null)
        //    {
        //        PropertyChanged(this, new PropertyChangedEventArgs(caller));
        //    }
        //}

        public event PropertyChangedEventHandler PropertyChanged;
    }
    public class Exclusive : Sive
    {
        public bool IsInlined { get; set; }
    }


    public class Inclusive : Sive
    {
        static public Inclusive Root { get; set; }
        public Inclusive()
        {
            Nodes = new List<Sive>();
        }
        public IList<Sive> Nodes { get; private set; }

        static public event EventHandler Expanded;
        static public event EventHandler Collapsed;

        public void CollapseDeep()
        {
            _isExpanded = false;
            foreach (var node in Nodes)
            {
                var inc = node as Inclusive;
                if (inc != null)
                    inc.CollapseDeep();
            }

            RaisePropertyChanged();
        }

        public string ExpandString
        {
            get
            {
                if (IsExpanded)
                    return "-";
                else
                    return "+";
            }
        }


        bool _isExpanded = false;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded == value)
                    return;

                _isExpanded = value;
                RaisePropertyChanged();

                if (_isExpanded && Expanded != null)
                    Expanded(this, null);
                else if (!_isExpanded && Collapsed != null)
                    Collapsed(this, null);

            }
        }

        //public string TotalCount
        //{
        //    get
        //    {
        //        if (_count == 0)
        //        {
        //            _count = MainWindow.CountInclusive(MainWindow.StaticRoot, Name, ref _calls);
        //            _countString = String.Format("{0:N0}", _count);
        //        }

        //        return _countString;
        //    }
        //}

    }

    public struct Line
    {
        //public Line() { }
        public int Inner { get; set; }
        public int Outer { get; set; }
        public int Level { get; set; }
        public string Name { get; set; }
        public bool Skip { get; set; }
        public int SourceLineNumber { get; set; }
    }


    public class Line2
    {
        public Line2() { }
        public int Inner { get; set; }
        public int Outer { get; set; }
        public int Level { get; set; }
        public string Name { get; set; }
    }
}
