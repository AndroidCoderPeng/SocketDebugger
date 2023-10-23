using System;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace SocketDebugger.Utils
{
    public class AutoScrollBehavior : Behavior<ListBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectionChanged += AssociatedObjectSelectionChanged;
        }

        private static void AssociatedObjectSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listbox = (ListBox)sender;
            if (listbox.SelectedItem != null)
            {
                listbox.Dispatcher.BeginInvoke((Action)delegate
                {
                    listbox.UpdateLayout();
                    listbox.ScrollIntoView(listbox.SelectedItem);
                });
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.SelectionChanged -= AssociatedObjectSelectionChanged;
        }
    }
}