﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BattleInfoPlugin.ViewModels.Enemies;
using BattleInfoPlugin.Views.Controls;

namespace BattleInfoPlugin.Views.Behaviors
{
    public class ScrollToCellBehavior : Behavior<Cell>
    {
        #region ParentElement


        public DependencyObject ParentElement
        {
            get { return (DependencyObject)this.GetValue(ParentElementProperty); }
            set { this.SetValue(ParentElementProperty, value); }
        }
        
        public static readonly DependencyProperty ParentElementProperty =
            DependencyProperty.Register("ParentElement", typeof(DependencyObject), typeof(ScrollToCellBehavior), new PropertyMetadata(null));


        #endregion

        #region ScrollTargetName


        public string ScrollTargetName
        {
            get { return (string)this.GetValue(ScrollTargetNameProperty); }
            set { this.SetValue(ScrollTargetNameProperty, value); }
        }
        
        public static readonly DependencyProperty ScrollTargetNameProperty =
            DependencyProperty.Register("ScrollTargetName", typeof(string), typeof(ScrollToCellBehavior), new PropertyMetadata(""));


        #endregion

        #region VerticalOffset AttatchedProperty

        public static double GetVerticalOffset(DependencyObject obj)
            => (double)obj.GetValue(VerticalOffsetProperty);

        public static void SetVerticalOffset(DependencyObject obj, double value)
            => obj.SetValue(VerticalOffsetProperty, value);

        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollToCellBehavior), new PropertyMetadata(0.0, OnVerticalOffsetChanged));

        private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as ScrollViewer)?.ScrollToVerticalOffset((double)e.NewValue);

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.MouseUp += this.AssociatedObject_MouseUp;
            this.AssociatedObject.MouseEnter += this.AssociatedObject_MouseEnter;
            this.AssociatedObject.MouseLeave += this.AssociatedObject_MouseLeave;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.MouseUp -= this.AssociatedObject_MouseUp;
            this.AssociatedObject.MouseEnter -= this.AssociatedObject_MouseEnter;
            this.AssociatedObject.MouseLeave -= this.AssociatedObject_MouseLeave;
        }

        private void AssociatedObject_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (this.ParentElement == null) return;

            var scrollTarget = this.GetScrollTarget();
            if (scrollTarget == null) return;

            var cellsGroupItems = GetCellGroups(scrollTarget);
            var targetCellGroup = this.GetTargetCellGroup(cellsGroupItems);
            if (targetCellGroup == null) return;

            var scrollHeight = cellsGroupItems
                .TakeWhile(x => !x.Equals(targetCellGroup))
                .Sum(x => x.ActualHeight);

            var animation = new DoubleAnimation
            {
                From = scrollTarget.VerticalOffset,
                To = scrollHeight,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                AccelerationRatio = 0.1,
                DecelerationRatio = 0.9,
            };
            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, scrollTarget);
            Storyboard.SetTargetProperty(animation, new PropertyPath(VerticalOffsetProperty));
            storyboard.Begin();
        }

        private ScrollViewer GetScrollTarget()
        {
            return this.ParentElement.Descendants<ScrollViewer>().FirstOrDefault(x => x.Name == this.ScrollTargetName);
        }

        private static GroupItem[] GetCellGroups(DependencyObject scrollTarget)
        {
            return scrollTarget.Descendants<GroupItem>()
                .Where(x => ((CollectionViewGroup)x.DataContext).Name is EnemyCellViewModel)
                .ToArray();
        }

        private GroupItem GetTargetCellGroup(IEnumerable<GroupItem> cellsGroupItems)
        {
            var cellNo = this.AssociatedObject.Text;
            return cellsGroupItems
                .SingleOrDefault(x => ((EnemyCellViewModel)((CollectionViewGroup)x.DataContext).Name).Key.ToString() == cellNo);
        }

        void AssociatedObject_MouseEnter(object sender, MouseEventArgs e)
        {
            // 対象データがない場合はカーソルを変えない
            var scrollTarget = this.GetScrollTarget();
            if (scrollTarget == null) return;
            if (this.GetTargetCellGroup(GetCellGroups(scrollTarget)) == null) return;

            this.AssociatedObject.Cursor = Cursors.Hand;
        }

        private void AssociatedObject_MouseLeave(object sender, MouseEventArgs e)
        {
            this.AssociatedObject.Cursor = null;
        }
    }

    static class BehaviorExtensions
    {
        public static IEnumerable<DependencyObject> Children(this DependencyObject obj)
        {
            if (obj == null) throw new ArgumentNullException();

            var count = VisualTreeHelper.GetChildrenCount(obj);
            if (count == 0) yield break;

            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child != null) yield return child;
            }
        }

        public static IEnumerable<DependencyObject> Descendants(this DependencyObject obj)
        {
            if (obj == null) throw new ArgumentNullException();

            foreach (var child in obj.Children())
            {
                yield return child;
                foreach (var grandChild in child.Descendants())
                    yield return grandChild;
            }
        }

        public static IEnumerable<T> Children<T>(this DependencyObject obj)
            where T : DependencyObject
        {
            return obj.Children().OfType<T>();
        }

        public static IEnumerable<T> Descendants<T>(this DependencyObject obj)
            where T : DependencyObject
        {
            return obj.Descendants().OfType<T>();
        }
    }
}
