using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MobileShell.Extensions;

public class ListViewExt : DependencyObject
{
    public static readonly DependencyProperty ItemClickCommandProperty = DependencyProperty.RegisterAttached("ItemClickCommand", typeof(ICommand), typeof(ListViewExt), new PropertyMetadata(null));

    public static void SetItemClickCommand(ListView listView, ICommand command)
    {
        listView.SetValue(ItemClickCommandProperty, command);
        listView.ItemClick += (sender, e) =>
        {
            if (command.CanExecute(e.ClickedItem))
                command.Execute(e.ClickedItem);
        };
    }

    public static ICommand GetItemClickCommand(ListView listView) => (ICommand)listView.GetValue(ItemClickCommandProperty);
}
