using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace SocketDebugger.Utils
{
    public class AutoScrollBehavior : Behavior<ListBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            ((ICollectionView)AssociatedObject.Items).CollectionChanged += AssociatedObjectCollectionChanged;
        }

        private void AssociatedObjectCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (AssociatedObject.HasItems)
            {
                AssociatedObject.ScrollIntoView(AssociatedObject.Items[AssociatedObject.Items.Count - 1]);
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            ((ICollectionView)AssociatedObject.Items).CollectionChanged -= AssociatedObjectCollectionChanged;
        }
    }
}