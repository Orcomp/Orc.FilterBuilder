﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TreeViewItemExtensions.cs" company="WildGums">
//   Copyright (c) 2008 - 2015 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FilterBuilder.Extensions
{
    using System.Windows.Controls;
    using System.Windows.Media;

    public static class TreeViewItemExtensions
    {
        public static int GetDepth(this TreeViewItem item)
        {
            TreeViewItem parent;

            while ((parent = GetParent(item)) != null)
            {
                return GetDepth(parent) + 1;
            }

            return 0;
        }

        private static TreeViewItem GetParent(TreeViewItem item)
        {
            var parent = VisualTreeHelper.GetParent(item);

            while (!(parent is TreeViewItem || parent is TreeView))
            {
                if (parent == null) return null;
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as TreeViewItem;
        }
    }
}