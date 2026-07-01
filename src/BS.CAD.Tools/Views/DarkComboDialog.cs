using System;
using System.Collections.Generic;
using System.Linq;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BS.CAD.Tools.Views
{
    public static class DarkComboDialog
    {
        private static System.Windows.Style CreateRoundedButtonStyle(double radius)
        {
            var style = new System.Windows.Style(typeof(System.Windows.Controls.Button));
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Button.BackgroundProperty,
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 35, 38))));
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Button.ForegroundProperty,
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 237, 242))));
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Button.BorderBrushProperty,
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 58, 63))));
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Button.BorderThicknessProperty,
                new System.Windows.Thickness(1)));
            var templateFactory = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.Border));
            templateFactory.SetValue(System.Windows.Controls.Border.BackgroundProperty,
                new System.Windows.TemplateBindingExtension(System.Windows.Controls.Button.BackgroundProperty));
            templateFactory.SetValue(System.Windows.Controls.Border.BorderBrushProperty,
                new System.Windows.TemplateBindingExtension(System.Windows.Controls.Button.BorderBrushProperty));
            templateFactory.SetValue(System.Windows.Controls.Border.BorderThicknessProperty,
                new System.Windows.TemplateBindingExtension(System.Windows.Controls.Button.BorderThicknessProperty));
            templateFactory.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new System.Windows.CornerRadius(radius));
            var content = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.ContentPresenter));
            content.SetValue(System.Windows.Controls.ContentPresenter.HorizontalAlignmentProperty,
                System.Windows.HorizontalAlignment.Center);
            content.SetValue(System.Windows.Controls.ContentPresenter.VerticalAlignmentProperty,
                System.Windows.VerticalAlignment.Center);
            templateFactory.AppendChild(content);
            var template = new System.Windows.Controls.ControlTemplate(typeof(System.Windows.Controls.Button));
            template.VisualTree = templateFactory;
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Button.TemplateProperty, template));
            return style;
        }

        /// <summary>
        /// Show a dark-themed dropdown selection dialog with optional delete button.
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="prompt">Prompt text</param>
        /// <param name="items">List of selectable items</param>
        /// <param name="defaultItem">Default selected item</param>
        /// <param name="showDelete">Show a "删除" button (for template management)</param>
        /// <returns>
        /// (Action, SelectedItem):
        ///   Action = "ok" / "delete" / "cancel"
        ///   SelectedItem = the chosen item (null if cancel/delete)
        /// </returns>
        public static (string Action, string? SelectedItem) Show(
            string title, string prompt, List<string> items,
            string defaultItem = "", bool showDelete = false)
        {
            var dlg = new System.Windows.Window
            {
                Width = 460, Height = 420,
                WindowStyle = System.Windows.WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                ResizeMode = System.Windows.ResizeMode.NoResize,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false
            };
            dlg.PreviewKeyDown += (s, ke) =>
            {
                if (ke.Key == System.Windows.Input.Key.Escape) { dlg.Close(); }
            };

            try { new System.Windows.Interop.WindowInteropHelper(dlg).Owner = AcadApp.MainWindow.Handle; }
            catch { dlg.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen; }

            var border = new System.Windows.Controls.Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(29, 34, 40)),
                CornerRadius = new System.Windows.CornerRadius(12),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(61, 69, 75)),
                BorderThickness = new System.Windows.Thickness(1)
            };

            var root = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(24, 18, 24, 16) };

            // Title
            root.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = title,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 246, 250)),
                FontSize = 18, FontWeight = System.Windows.FontWeights.SemiBold,
                Margin = new System.Windows.Thickness(0, 0, 0, 6)
            });

            // Prompt
            root.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = prompt,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 180, 192)),
                FontSize = 12, Margin = new System.Windows.Thickness(0, 0, 0, 10)
            });

            // Search box
            var searchBorder = new System.Windows.Controls.Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 25, 31)),
                CornerRadius = new System.Windows.CornerRadius(7),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 58, 63)),
                BorderThickness = new System.Windows.Thickness(1),
                Padding = new System.Windows.Thickness(10, 6, 10, 6),
                Margin = new System.Windows.Thickness(0, 0, 0, 8)
            };
            var searchBox = new System.Windows.Controls.TextBox
            {
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 237, 242)),
                BorderThickness = new System.Windows.Thickness(0),
                CaretBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 237, 242))
            };
            searchBorder.Child = searchBox;

            // Scrollable item list
            var listBorder = new System.Windows.Controls.Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 25, 31)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(56, 66, 77)),
                BorderThickness = new System.Windows.Thickness(1),
                CornerRadius = new System.Windows.CornerRadius(8),
                Height = 180
            };

            var itemStack = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(4) };
            string selectedValue = items.Contains(defaultItem) ? defaultItem : (items.Count > 0 ? items[0] : "");

            void RefreshItemHighlights()
            {
                foreach (System.Windows.Controls.Border? b in itemStack.Children)
                {
                    if (b?.Child is System.Windows.Controls.TextBlock tb)
                    {
                        bool active = string.Equals(tb.Text, selectedValue, StringComparison.Ordinal);
                        b.Background = new System.Windows.Media.SolidColorBrush(active
                            ? System.Windows.Media.Color.FromRgb(29, 47, 63)
                            : System.Windows.Media.Color.FromRgb(20, 25, 31));
                        b.BorderBrush = new System.Windows.Media.SolidColorBrush(active
                            ? System.Windows.Media.Color.FromRgb(53, 120, 246)
                            : System.Windows.Media.Color.FromRgb(20, 25, 31));
                    }
                }
            }

            void RebuildList(string filter)
            {
                itemStack.Children.Clear();
                var filtered = string.IsNullOrWhiteSpace(filter)
                    ? items
                    : items.Where(x => x.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

                foreach (var item in filtered)
                {
                    var tb = new System.Windows.Controls.TextBlock
                    {
                        Text = item,
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 246, 250)),
                        FontSize = 13,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        Margin = new System.Windows.Thickness(10, 0, 10, 0)
                    };
                    var optionBorder = new System.Windows.Controls.Border
                    {
                        Child = tb,
                        Height = 32,
                        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 25, 31)),
                        BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 25, 31)),
                        BorderThickness = new System.Windows.Thickness(1),
                        CornerRadius = new System.Windows.CornerRadius(6),
                        Margin = new System.Windows.Thickness(0, 0, 0, 2),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };
                    string captured = item;
                    optionBorder.MouseLeftButtonDown += (s, e) =>
                    {
                        if (e.ClickCount >= 2)
                        {
                            selectedValue = captured;
                            dlg.DialogResult = true;
                            dlg.Close();
                        }
                        else
                        {
                            selectedValue = captured;
                            RefreshItemHighlights();
                        }
                    };
                    itemStack.Children.Add(optionBorder);
                }
                RefreshItemHighlights();
            }

            RebuildList("");

            searchBox.TextChanged += (s, e) => RebuildList(searchBox.Text);

            var scroll = new System.Windows.Controls.ScrollViewer
            {
                Content = itemStack,
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                Background = System.Windows.Media.Brushes.Transparent
            };
            listBorder.Child = scroll;

            root.Children.Add(searchBorder);
            root.Children.Add(listBorder);

            // Buttons
            var btnStyle = CreateRoundedButtonStyle(8);
            var btnStack = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new System.Windows.Thickness(0, 12, 0, 0)
            };

            string? resultAction = null;
            string? resultItem = null;

            if (showDelete)
            {
                var deleteBtn = new System.Windows.Controls.Button
                {
                    Content = "删除模板",
                    Width = 88, Height = 34,
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 50, 50)),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 50, 50)),
                    Foreground = System.Windows.Media.Brushes.White,
                    FontWeight = System.Windows.FontWeights.SemiBold,
                    Margin = new System.Windows.Thickness(0, 0, 8, 0),
                    Style = btnStyle
                };
                deleteBtn.Click += (s, e) =>
                {
                    resultAction = "delete";
                    resultItem = selectedValue;
                    dlg.DialogResult = true;
                    dlg.Close();
                };
                btnStack.Children.Add(deleteBtn);
            }

            var cancelBtn = new System.Windows.Controls.Button
            {
                Content = "取消",
                Width = 72, Height = 34,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(78, 91, 100)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 180, 192)),
                Margin = new System.Windows.Thickness(0, 0, 10, 0),
                Style = btnStyle
            };
            cancelBtn.Click += (s, e) => dlg.Close();
            btnStack.Children.Add(cancelBtn);

            var okBtn = new System.Windows.Controls.Button
            {
                Content = "读取",
                Width = 80, Height = 34,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(53, 120, 246)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 145, 255)),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = System.Windows.FontWeights.SemiBold,
                IsDefault = true,
                Style = btnStyle
            };
            okBtn.Click += (s, e) =>
            {
                resultAction = "ok";
                resultItem = selectedValue;
                dlg.DialogResult = true;
                dlg.Close();
            };
            btnStack.Children.Add(okBtn);

            root.Children.Add(btnStack);
            border.Child = root;
            dlg.Content = border;
            dlg.MouseLeftButtonDown += (s, e) => { try { dlg.DragMove(); } catch { } };
            searchBox.Focus();

            return dlg.ShowDialog() == true
                ? (resultAction ?? "ok", resultItem ?? selectedValue)
                : ("cancel", null);
        }

        /// <summary>
        /// Simple selection: returns selected item or null.
        /// </summary>
        public static string? Select(string title, string prompt, List<string> items, string defaultItem = "")
        {
            var (action, item) = Show(title, prompt, items, defaultItem, showDelete: false);
            return action == "ok" ? item : null;
        }
    }
}
