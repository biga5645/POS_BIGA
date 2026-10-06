using System.Windows;
using System.Windows.Controls;
using POS_BIGA.Models;

namespace POS_BIGA.Views.Selectors
{
    public class LeftItemTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null) return base.SelectTemplate(item, container);
            var app = Application.Current;
            if (item is Product)
            {
                var t = app.TryFindResource(typeof(Product)) as DataTemplate;
                if (t != null) return t;
            }
            if (item is Category)
            {
                var t = app.TryFindResource(typeof(Category)) as DataTemplate;
                if (t != null) return t;
            }
            return base.SelectTemplate(item, container);
        }
    }
}
