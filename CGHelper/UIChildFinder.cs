using System.Windows;
using System.Windows.Media;

namespace CommonLibrary
{
    public static class UIChildFinder
    {
        public static DependencyObject FindChild<T>(this DependencyObject parent, string childName)
        {
            if (parent == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            int ChildrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < ChildrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T && child is FrameworkElement frameworkElement && frameworkElement.Name.Equals(childName))
                {
                    return child;
                } 
                else
                {
                    DependencyObject dependencyObject = FindChild<T>(child, childName);
                    if (dependencyObject != null)
                    {
                        return dependencyObject;
                    }
                }
            }

            return null;
        }
    }
}
