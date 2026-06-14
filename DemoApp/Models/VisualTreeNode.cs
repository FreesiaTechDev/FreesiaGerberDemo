using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace FreesiaGerberDemo.Models
{
    public sealed class VisualTreeNode
    {
        private static readonly VisualTreeNode Placeholder = new VisualTreeNode();

        private readonly Func<IEnumerable<VisualTreeNode>> ChildrenFactory;

        private readonly Func<string> DetailFactory;

        private bool IsChildrenLoaded;

        private string DetailText;

        public string Header { get; }

        public ObservableCollection<VisualTreeNode> Children { get; }

        public string Detail
        {
            get
            {
                if (DetailText is null)
                    DetailText = DetailFactory?.Invoke() ?? string.Empty;

                return DetailText;
            }
        }

        private VisualTreeNode()
        {
            Header = string.Empty;
            Children = new ObservableCollection<VisualTreeNode>();
            IsChildrenLoaded = true;
        }

        public VisualTreeNode(string Header, Func<string> DetailFactory, Func<IEnumerable<VisualTreeNode>> ChildrenFactory)
        {
            this.Header = Header;
            this.DetailFactory = DetailFactory;
            this.ChildrenFactory = ChildrenFactory;
            Children = new ObservableCollection<VisualTreeNode>();

            if (ChildrenFactory != null)
                Children.Add(Placeholder);
        }

        public void EnsureChildrenLoaded()
        {
            if (IsChildrenLoaded)
                return;

            IsChildrenLoaded = true;
            Children.Clear();

            foreach (VisualTreeNode Child in ChildrenFactory?.Invoke() ?? Enumerable.Empty<VisualTreeNode>())
                Children.Add(Child);
        }

    }
}
