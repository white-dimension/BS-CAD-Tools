using System;
using System.Collections.Generic;
using System.Linq;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BS.CAD.Tools.Views
{
    public static class InputDialog
    {
        private static System.Windows.Controls.ControlTemplate CreateRoundedButtonTemplate(double radius)
        {
            var template = new System.Windows.Controls.ControlTemplate(typeof(System.Windows.Controls.Button));
            var border = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.Border));
            border.Name = "border";
            border.SetValue(System.Windows.Controls.Border.BackgroundProperty, new System.Windows.TemplateBindingExtension(System.Windows.Controls.Button.BackgroundProperty));
            border.SetValue(System.Windows.Controls.Border.BorderBrushProperty, new System.Windows.TemplateBindingExtension(System.Windows.Controls.Button.BorderBrushProperty));
            border.SetValue(System.Windows.Controls.Border.BorderThicknessProperty, new System.Windows.TemplateBindingExtension(System.Windows.Controls.Button.BorderThicknessProperty));
            border.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new System.Windows.CornerRadius(radius));
            var content = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.ContentPresenter));
            content.SetValue(System.Windows.Controls.ContentPresenter.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            content.SetValue(System.Windows.Controls.ContentPresenter.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            border.AppendChild(content);
            template.VisualTree = border;
            return template;
        }

        private static void ApplyCommonStyles(System.Windows.Window dlg)
        {
            // ScrollBar 样式 (Chrome 4px)
            string scrollXaml = @"
            <Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ScrollBar'>
                <Setter Property='Width' Value='4'/><Setter Property='Height' Value='4'/>
                <Setter Property='Background' Value='Transparent'/>
                <Setter Property='Template'>
                    <Setter.Value>
                        <ControlTemplate TargetType='ScrollBar'>
                            <Grid Background='Transparent'>
                                <Track Name='PART_Track' IsDirectionReversed='true'>
                                    <Track.Thumb>
                                        <Thumb><Thumb.Template><ControlTemplate TargetType='Thumb'><Border Background='#666666' CornerRadius='2'/></ControlTemplate></Thumb.Template></Thumb>
                                    </Track.Thumb>
                                </Track>
                            </Grid>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>";
            var scrollStyle = (System.Windows.Style)System.Windows.Markup.XamlReader.Parse(scrollXaml);
            dlg.Resources.Add(typeof(System.Windows.Controls.Primitives.ScrollBar), scrollStyle);

            // ComboBox 样式
            var cbStyle = new System.Windows.Style(typeof(System.Windows.Controls.ComboBox));
            cbStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.ComboBox.BackgroundProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 35, 38))));
            cbStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.ComboBox.ForegroundProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 237, 242))));
            cbStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.ComboBox.BorderBrushProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 58, 63))));
            cbStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.ComboBox.BorderThicknessProperty, new System.Windows.Thickness(1)));
            cbStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.ComboBox.PaddingProperty, new System.Windows.Thickness(8, 4, 8, 4)));

            // 下拉项样式
            var cbiStyle = new System.Windows.Style(typeof(System.Windows.Controls.ComboBoxItem));
            cbiStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.ComboBoxItem.BackgroundProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(36, 39, 41))));
            cbiStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.ComboBoxItem.ForegroundProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 237, 242))));
            cbiStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.ComboBoxItem.PaddingProperty, new System.Windows.Thickness(10, 6, 10, 6)));

            dlg.Resources.Add(typeof(System.Windows.Controls.ComboBox), cbStyle);
            dlg.Resources.Add(typeof(System.Windows.Controls.ComboBoxItem), cbiStyle);
        }

        public static string? Show(string title, string prompt, string defaultValue = "")
        {
            var dlg = new System.Windows.Window
            {
                Width = 400, Height = 200,
                WindowStyle = System.Windows.WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                ResizeMode = System.Windows.ResizeMode.NoResize,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false
            };

            try { new System.Windows.Interop.WindowInteropHelper(dlg).Owner = AcadApp.MainWindow.Handle; }
            catch { dlg.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen; }

            var mainBorder = new System.Windows.Controls.Border { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(36, 39, 41)), CornerRadius = new System.Windows.CornerRadius(12), BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(61, 69, 75)), BorderThickness = new System.Windows.Thickness(1) };
            var root = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(24) };

            var titleTxt = new System.Windows.Controls.TextBlock { Text = title, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 237, 242)), FontSize = 18, FontWeight = System.Windows.FontWeights.SemiBold, Margin = new System.Windows.Thickness(0, 0, 0, 8) };
            var promptTxt = new System.Windows.Controls.TextBlock { Text = prompt, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(143, 154, 163)), FontSize = 12, Margin = new System.Windows.Thickness(0, 0, 0, 16) };

            var txtBorder = new System.Windows.Controls.Border { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 35, 38)), CornerRadius = new System.Windows.CornerRadius(7), Padding = new System.Windows.Thickness(10, 8, 10, 8), BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 58, 63)), BorderThickness = new System.Windows.Thickness(1) };
            var txt = new System.Windows.Controls.TextBox { Text = defaultValue, Background = System.Windows.Media.Brushes.Transparent, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 237, 242)), BorderThickness = new System.Windows.Thickness(0), CaretBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 237, 242)) };
            txtBorder.Child = txt;

            var btnStack = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new System.Windows.Thickness(0, 20, 0, 0) };
            var cancelBtn = new System.Windows.Controls.Button { Content = "取消", Width = 80, Height = 34, Background = System.Windows.Media.Brushes.Transparent, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(78, 91, 100)), BorderThickness = new System.Windows.Thickness(1), Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(143, 154, 163)), Margin = new System.Windows.Thickness(0, 0, 10, 0) };
            cancelBtn.Template = CreateRoundedButtonTemplate(6);
            cancelBtn.Click += (s, e) => dlg.Close();

            var okBtn = new System.Windows.Controls.Button { Content = "确定", Width = 80, Height = 34, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 132, 214)), BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 132, 214)), BorderThickness = new System.Windows.Thickness(1), Foreground = System.Windows.Media.Brushes.White };
            okBtn.Template = CreateRoundedButtonTemplate(6);
            okBtn.Click += (s, e) => { dlg.DialogResult = true; dlg.Close(); };
            okBtn.IsDefault = true;

            btnStack.Children.Add(cancelBtn);
            btnStack.Children.Add(okBtn);

            root.Children.Add(titleTxt);
            root.Children.Add(promptTxt);
            root.Children.Add(txtBorder);
            root.Children.Add(btnStack);
            mainBorder.Child = root;
            dlg.Content = mainBorder;

            dlg.MouseLeftButtonDown += (s, e) => { try { dlg.DragMove(); } catch { } };
            txt.Focus();
            txt.SelectAll();

            return dlg.ShowDialog() == true ? txt.Text : null;
        }

        public static string? Select(string title, string prompt, List<string> options, string defaultOption = "")
        {
            var dlg = new System.Windows.Window
            {
                Width = 400, Height = 320,
                WindowStyle = System.Windows.WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                ResizeMode = System.Windows.ResizeMode.NoResize,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false
            };
            ApplyCommonStyles(dlg);

            try { new System.Windows.Interop.WindowInteropHelper(dlg).Owner = AcadApp.MainWindow.Handle; }
            catch { dlg.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen; }

            var mainBorder = new System.Windows.Controls.Border { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(36, 39, 41)), CornerRadius = new System.Windows.CornerRadius(12), BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(61, 69, 75)), BorderThickness = new System.Windows.Thickness(1) };
            var root = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(24) };

            var titleTxt = new System.Windows.Controls.TextBlock { Text = title, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 237, 242)), FontSize = 18, FontWeight = System.Windows.FontWeights.SemiBold, Margin = new System.Windows.Thickness(0, 0, 0, 8) };
            var promptTxt = new System.Windows.Controls.TextBlock { Text = prompt, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(143, 154, 163)), FontSize = 12, Margin = new System.Windows.Thickness(0, 0, 0, 16) };

            string selectedValue = options.Contains(defaultOption) ? defaultOption : (options.Count > 0 ? options[0] : "");

            var listBorder = new System.Windows.Controls.Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 35, 38)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 58, 63)),
                BorderThickness = new System.Windows.Thickness(1),
                CornerRadius = new System.Windows.CornerRadius(8),
                Height = 150
            };
            var optionStack = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(4) };
            var optionButtons = new List<System.Windows.Controls.Button>();
            void RefreshOptionButtons()
            {
                foreach (var b in optionButtons)
                {
                    bool active = string.Equals(b.Tag?.ToString(), selectedValue, StringComparison.Ordinal);
                    b.Background = new System.Windows.Media.SolidColorBrush(active
                        ? System.Windows.Media.Color.FromRgb(38, 56, 71)
                        : System.Windows.Media.Color.FromRgb(31, 35, 38));
                    b.BorderBrush = new System.Windows.Media.SolidColorBrush(active
                        ? System.Windows.Media.Color.FromRgb(10, 132, 214)
                        : System.Windows.Media.Color.FromRgb(31, 35, 38));
                }
            }

            foreach (var option in options)
            {
                var optionButton = new System.Windows.Controls.Button
                {
                    Content = option,
                    Tag = option,
                    Height = 34,
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left,
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 35, 38)),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 35, 38)),
                    BorderThickness = new System.Windows.Thickness(1),
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 237, 242)),
                    Margin = new System.Windows.Thickness(0, 0, 0, 3),
                    Padding = new System.Windows.Thickness(12, 0, 12, 0)
                };
                optionButton.Template = CreateRoundedButtonTemplate(6);
                optionButton.Click += (s, e) =>
                {
                    selectedValue = option;
                    RefreshOptionButtons();
                };
                optionButton.MouseDoubleClick += (s, e) =>
                {
                    selectedValue = option;
                    dlg.DialogResult = true;
                    dlg.Close();
                };
                optionButtons.Add(optionButton);
                optionStack.Children.Add(optionButton);
            }
            RefreshOptionButtons();

            var scroll = new System.Windows.Controls.ScrollViewer
            {
                Content = optionStack,
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                Background = System.Windows.Media.Brushes.Transparent
            };
            listBorder.Child = scroll;

            var btnStack = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new System.Windows.Thickness(0, 20, 0, 0) };
            var cancelBtn = new System.Windows.Controls.Button { Content = "取消", Width = 80, Height = 34, Background = System.Windows.Media.Brushes.Transparent, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(78, 91, 100)), BorderThickness = new System.Windows.Thickness(1), Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(143, 154, 163)), Margin = new System.Windows.Thickness(0, 0, 10, 0) };
            cancelBtn.Template = CreateRoundedButtonTemplate(6);
            cancelBtn.Click += (s, e) => dlg.Close();

            var okBtn = new System.Windows.Controls.Button { Content = "确定", Width = 80, Height = 34, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 132, 214)), BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 132, 214)), BorderThickness = new System.Windows.Thickness(1), Foreground = System.Windows.Media.Brushes.White };
            okBtn.Template = CreateRoundedButtonTemplate(6);
            okBtn.Click += (s, e) => { dlg.DialogResult = true; dlg.Close(); };
            okBtn.IsDefault = true;

            btnStack.Children.Add(cancelBtn);
            btnStack.Children.Add(okBtn);

            root.Children.Add(titleTxt);
            root.Children.Add(promptTxt);
            root.Children.Add(listBorder);
            root.Children.Add(btnStack);
            mainBorder.Child = root;
            dlg.Content = mainBorder;

            dlg.MouseLeftButtonDown += (s, e) => { try { dlg.DragMove(); } catch { } };

            return dlg.ShowDialog() == true ? selectedValue : null;
        }

        public static (string Name, string Description)? ShowLayerFields(string title, string name, string description)
        {
            var dlg = new System.Windows.Window
            {
                Width = 460, Height = 300,
                WindowStyle = System.Windows.WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                ResizeMode = System.Windows.ResizeMode.NoResize,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false
            };

            try { new System.Windows.Interop.WindowInteropHelper(dlg).Owner = AcadApp.MainWindow.Handle; }
            catch { dlg.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen; }

            var mainBorder = new System.Windows.Controls.Border { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(36, 39, 41)), CornerRadius = new System.Windows.CornerRadius(12), BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(61, 69, 75)), BorderThickness = new System.Windows.Thickness(1) };
            var root = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(24) };
            var titleTxt = new System.Windows.Controls.TextBlock { Text = title, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 237, 242)), FontSize = 18, FontWeight = System.Windows.FontWeights.SemiBold, Margin = new System.Windows.Thickness(0, 0, 0, 12) };

            System.Windows.Controls.TextBox BuildTextBox(string value)
            {
                return new System.Windows.Controls.TextBox { Text = value, Background = System.Windows.Media.Brushes.Transparent, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 237, 242)), BorderThickness = new System.Windows.Thickness(0), CaretBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 237, 242)) };
            }

            System.Windows.Controls.Border WrapInput(System.Windows.Controls.TextBox box)
            {
                return new System.Windows.Controls.Border { Child = box, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 35, 38)), CornerRadius = new System.Windows.CornerRadius(7), Padding = new System.Windows.Thickness(10, 8, 10, 8), BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 58, 63)), BorderThickness = new System.Windows.Thickness(1), Margin = new System.Windows.Thickness(0, 0, 0, 14) };
            }

            var nameLabel = new System.Windows.Controls.TextBlock { Text = "图层名称", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(143, 154, 163)), FontSize = 12, Margin = new System.Windows.Thickness(0, 0, 0, 6) };
            var nameBox = BuildTextBox(name);
            var descLabel = new System.Windows.Controls.TextBlock { Text = "说明", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(143, 154, 163)), FontSize = 12, Margin = new System.Windows.Thickness(0, 0, 0, 6) };
            var descBox = BuildTextBox(description);

            var btnStack = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new System.Windows.Thickness(0, 8, 0, 0) };
            var cancelBtn = new System.Windows.Controls.Button { Content = "取消", Width = 88, Height = 36, Background = System.Windows.Media.Brushes.Transparent, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(78, 91, 100)), BorderThickness = new System.Windows.Thickness(1), Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(143, 154, 163)), Margin = new System.Windows.Thickness(0, 0, 12, 0) };
            cancelBtn.Template = CreateRoundedButtonTemplate(7);
            cancelBtn.Click += (s, e) => dlg.Close();
            var okBtn = new System.Windows.Controls.Button { Content = "保存", Width = 96, Height = 36, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 132, 214)), BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 132, 214)), BorderThickness = new System.Windows.Thickness(1), Foreground = System.Windows.Media.Brushes.White };
            okBtn.Template = CreateRoundedButtonTemplate(7);
            okBtn.Click += (s, e) => { dlg.DialogResult = true; dlg.Close(); };
            okBtn.IsDefault = true;

            btnStack.Children.Add(cancelBtn);
            btnStack.Children.Add(okBtn);
            root.Children.Add(titleTxt);
            root.Children.Add(nameLabel);
            root.Children.Add(WrapInput(nameBox));
            root.Children.Add(descLabel);
            root.Children.Add(WrapInput(descBox));
            root.Children.Add(btnStack);
            mainBorder.Child = root;
            dlg.Content = mainBorder;
            dlg.MouseLeftButtonDown += (s, e) => { try { dlg.DragMove(); } catch { } };
            nameBox.Focus();
            nameBox.SelectAll();

            return dlg.ShowDialog() == true ? (nameBox.Text, descBox.Text) : null;
        }
    }
}
