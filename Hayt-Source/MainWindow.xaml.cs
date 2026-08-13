using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Hayt
{

    

    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _popupOpenTimer;
        private readonly DispatcherTimer _treeHoverExpandTimer;
        private TreeViewItem? _pendingHoverItem;
        private bool _popupIsOpening;
        private bool _lessonWasSelected;

        
        // ====================================================
        //  منوی اصلی
        // ====================================================
        private void MainMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainMenuPanel == null) return;
            MainMenuPanel.Visibility = (MainMenuPanel.Visibility == Visibility.Visible)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }public MainWindow()
        {
            InitializeComponent();
            _popupOpenTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
            _popupOpenTimer.Tick += PopupOpenTimer_Tick;

            _treeHoverExpandTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(220)
            };
            _treeHoverExpandTimer.Tick += TreeHoverExpandTimer_Tick;
        }

        private void BookComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            OpenCategoryPopup();
        }

        private void BookComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space || e.Key == Key.Down || e.Key == Key.F4)
            {
                e.Handled = true;
                OpenCategoryPopup();
            }
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                CloseCategoryPopup();
            }
        }

        private void BookComboBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus == BookComboBox)
            {
                _popupOpenTimer.Stop();
                _popupOpenTimer.Start();
            }
        }

        private void PopupOpenTimer_Tick(object? sender, EventArgs e)
        {
            _popupOpenTimer.Stop();
            OpenCategoryPopup();
        }

        private void OpenCategoryPopup()
        {
            if (_popupIsOpening) return;
            _popupIsOpening = true;
            try
            {
                BookComboBox.IsDropDownOpen = false;
                if (!CategoryPopup.IsOpen)
                    CategoryPopup.IsOpen = true;
            }
            finally
            {
                _popupIsOpening = false;
            }
        }

        private void CloseCategoryPopup()
        {
            _popupOpenTimer.Stop();
            if (CategoryPopup.IsOpen)
                CategoryPopup.IsOpen = false;
        }

        private void CategoryPopup_Opened(object sender, EventArgs e)
        {
            _lessonWasSelected = false;
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => EducationTreeView.Focus()));
        }

        private void CategoryPopup_Closed(object sender, EventArgs e)
        {
            _popupOpenTimer.Stop();
            if (_lessonWasSelected)
            {
                _lessonWasSelected = false;
                return;
            }
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                if (BookComboBox.IsKeyboardFocusWithin)
                    Keyboard.ClearFocus();
            }));
        }

        private void CloseCategoryPopup_Click(object sender, RoutedEventArgs e)
            => CloseCategoryPopup();

        private void EducationTreeItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is not TreeViewItem item)
                return;

            object? data = item.DataContext;
            if (data == null || DetectItemLevel(data) == "Lesson")
                return;

            if (!item.HasItems || item.IsExpanded)
                return;

            _pendingHoverItem = item;
            _treeHoverExpandTimer.Stop();
            _treeHoverExpandTimer.Start();
        }

        private void TreeHoverExpandTimer_Tick(object? sender, EventArgs e)
        {
            _treeHoverExpandTimer.Stop();

            TreeViewItem? item = _pendingHoverItem;
            _pendingHoverItem = null;

            if (item == null || !item.IsMouseOver || !item.HasItems)
                return;

            object? data = item.DataContext;
            if (data == null || DetectItemLevel(data) == "Lesson")
                return;

            item.IsExpanded = true;
        }
        private void EducationTreeItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TreeViewItem item) return;

            TreeViewItem? clickedItem = FindVisualParent<TreeViewItem>(e.OriginalSource as DependencyObject);
            if (clickedItem == null || !ReferenceEquals(clickedItem, item)) return;

            item.IsSelected = true;
            item.Focus();

            object? selectedData = item.DataContext;
            if (selectedData == null) return;

            string level = DetectItemLevel(selectedData);

            switch (level)
            {
                case "Category":
                    ExecuteCategoryCommand(selectedData);
                    if (item.HasItems) item.IsExpanded = true;
                    e.Handled = true;
                    return;

                case "Book":
                    SetViewModelProperty("SelectedBook", selectedData);
                    CollapseSiblingItems(item);
                    if (item.HasItems) item.IsExpanded = true;
                    e.Handled = true;
                    return;

                case "Section":
                    SetViewModelProperty("SelectedSection", selectedData);
                    CollapseSiblingItems(item);
                    if (item.HasItems) item.IsExpanded = true;
                    e.Handled = true;
                    return;

                case "Chapter":
                    SetViewModelProperty("SelectedChapter", selectedData);
                    CollapseSiblingItems(item);
                    if (item.HasItems) item.IsExpanded = true;
                    e.Handled = true;
                    return;

                case "Lesson":
                    SetViewModelProperty("SelectedLesson", selectedData);
                    SelectParentsFromTree(item);
                    _lessonWasSelected = true;
                    e.Handled = true;
                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(CloseCategoryPopup));
                    return;
            }
        }

        private void EducationTreeItem_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is not TreeViewItem item) return;
            if (!ReferenceEquals(e.OriginalSource, item)) return;

            object? selectedData = item.DataContext;
            if (selectedData == null) return;

            string level = DetectItemLevel(selectedData);
            switch (level)
            {
                case "Book": SetViewModelProperty("SelectedBook", selectedData); break;
                case "Section": SetViewModelProperty("SelectedSection", selectedData); break;
                case "Chapter": SetViewModelProperty("SelectedChapter", selectedData); break;
                case "Lesson": SetViewModelProperty("SelectedLesson", selectedData); break;
            }
            e.Handled = true;
        }

        private void SelectParentsFromTree(TreeViewItem lessonItem)
        {
            TreeViewItem? chapterItem = ItemsControl.ItemsControlFromItemContainer(lessonItem) as TreeViewItem;
            TreeViewItem? sectionItem = chapterItem == null ? null : ItemsControl.ItemsControlFromItemContainer(chapterItem) as TreeViewItem;
            TreeViewItem? bookItem = sectionItem == null ? null : ItemsControl.ItemsControlFromItemContainer(sectionItem) as TreeViewItem;

            if (bookItem?.DataContext != null) SetViewModelProperty("SelectedBook", bookItem.DataContext);
            if (sectionItem?.DataContext != null) SetViewModelProperty("SelectedSection", sectionItem.DataContext);
            if (chapterItem?.DataContext != null) SetViewModelProperty("SelectedChapter", chapterItem.DataContext);
            if (lessonItem.DataContext != null) SetViewModelProperty("SelectedLesson", lessonItem.DataContext);
        }

        private void ExecuteCategoryCommand(object category)
        {
            object? viewModel = DataContext;
            if (viewModel == null) return;

            PropertyInfo? commandProperty = viewModel.GetType().GetProperty("SelectCategoryCommand",
                BindingFlags.Instance | BindingFlags.Public);
            if (commandProperty?.GetValue(viewModel) is not ICommand command) return;

            object? categoryId = GetPropertyValue(category, "Id");
            if (command.CanExecute(categoryId))
                command.Execute(categoryId);
        }

        private void SetViewModelProperty(string propertyName, object value)
        {
            object? viewModel = DataContext;
            if (viewModel == null) return;

            PropertyInfo? property = viewModel.GetType().GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (property == null || !property.CanWrite) return;

            Type targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (!targetType.IsInstanceOfType(value)) return;

            try { property.SetValue(viewModel, value); }
            catch { }
        }

        private static string DetectItemLevel(object data)
        {
            Type type = data.GetType();
            string typeName = type.Name;

            if (typeName.Contains("Category", StringComparison.OrdinalIgnoreCase)) return "Category";
            if (typeName.Contains("Book", StringComparison.OrdinalIgnoreCase)) return "Book";
            if (typeName.Contains("Section", StringComparison.OrdinalIgnoreCase)) return "Section";
            if (typeName.Contains("Chapter", StringComparison.OrdinalIgnoreCase)) return "Chapter";
            if (typeName.Contains("Lesson", StringComparison.OrdinalIgnoreCase)) return "Lesson";

            if (HasProperty(data, "Books") || HasProperty(data, "SubCategoriesJson")) return "Category";
            if (HasProperty(data, "Sections")) return "Book";
            if (HasProperty(data, "Chapters")) return "Section";
            if (HasProperty(data, "Lessons")) return "Chapter";
            if (HasProperty(data, "Questions") || HasProperty(data, "Content")) return "Lesson";

            return string.Empty;
        }

        private static bool HasProperty(object source, string propertyName)
            => source.GetType().GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase) != null;

        private static object? GetPropertyValue(object source, string propertyName)
            => source.GetType().GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)?.GetValue(source);

        private static void CollapseSiblingItems(TreeViewItem selectedItem)
        {
            ItemsControl? parent = ItemsControl.ItemsControlFromItemContainer(selectedItem);
            if (parent == null) return;

            foreach (object siblingData in parent.Items)
            {
                if (parent.ItemContainerGenerator.ContainerFromItem(siblingData) is TreeViewItem sibling &&
                    !ReferenceEquals(sibling, selectedItem))
                {
                    sibling.IsExpanded = false;
                }
            }
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            DependencyObject? current = child;
            while (current != null)
            {
                if (current is T requestedParent) return requestedParent;
                current = (current is Visual || current is System.Windows.Media.Media3D.Visual3D)
                    ? VisualTreeHelper.GetParent(current)
                    : (current as FrameworkElement)?.Parent ?? (current as FrameworkContentElement)?.Parent;
            }
            return null;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (CategoryPopup.IsOpen && e.Key == Key.Escape)
            {
                e.Handled = true;
                CloseCategoryPopup();
                BookComboBox.Focus();
                return;
            }
            base.OnPreviewKeyDown(e);
        }

        private void DashboardButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                if (Application.Current.Properties["DashboardService"]
                    is not Hayt.Services.IDashboardService dashboardService)
                {
                    MessageBox.Show(
                        this,
                        "سرویس داشبورد در دسترس نیست.",
                        "داشبورد",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                var notificationService =
                    new Hayt.Services.NotificationService();

                var notificationViewModel =
                    new Hayt.ViewModels.NotificationViewModel(
                        notificationService);

                var achievementService =
                    new Hayt.Services.AchievementService(
                        notificationService);

                var achievementViewModel =
                    new Hayt.ViewModels.AchievementViewModel(
                        achievementService);

                var streakService =
                    new Hayt.Services.StreakService();

                var streakViewModel =
                    new Hayt.ViewModels.StreakViewModel(
                        streakService);

                var dashboardViewModel =
                    new Hayt.ViewModels.DashboardViewModel(
                        dashboardService,
                        notificationViewModel,
                        achievementViewModel,
                        streakViewModel);

                var dashboardWindow =
                    new Hayt.Views.DashboardWindow(
                        dashboardViewModel)
                    {
                        Owner = this
                    };

                dashboardWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "بازکردن داشبورد با خطا مواجه شد." +
                    Environment.NewLine +
                    Environment.NewLine +
                    ex.Message,
                    "خطای داشبورد",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                object? viewModel = DataContext;

                if (viewModel != null)
                {
                    PropertyInfo? commandProperty =
                        viewModel.GetType().GetProperty(
                            "ToggleThemeCommand",
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.IgnoreCase);

                    if (commandProperty?.GetValue(viewModel) is ICommand command &&
                        command.CanExecute(null))
                    {
                        command.Execute(null);
                        return;
                    }
                }

                MessageBox.Show(
                    this,
                    "فرمان تغییر پوسته هنوز در ViewModel تعریف نشده است.",
                    "تغییر پوسته",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "تغییر پوسته با خطا مواجه شد." +
                    Environment.NewLine +
                    ex.Message,
                    "خطا",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            _popupOpenTimer.Stop();
            _popupOpenTimer.Tick -= PopupOpenTimer_Tick;

            _treeHoverExpandTimer.Stop();
            _treeHoverExpandTimer.Tick -= TreeHoverExpandTimer_Tick;
            _pendingHoverItem = null;
            CategoryPopup.IsOpen = false;
            base.OnClosed(e);
        }

        // ====================================================
        //  باز/بسته شدن منوهای هاوری سایدبار
        // ====================================================
        private void SidebarHoverExpander_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Expander expander)
            {
                expander.IsExpanded = true;
            }
        }

        private void SidebarHoverExpander_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Expander expander)
            {
                expander.IsExpanded = false;
            }
        }
    }
}


