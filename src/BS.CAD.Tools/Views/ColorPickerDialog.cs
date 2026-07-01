using System;
using System.Collections.Generic;
using System.Linq;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcColor = Autodesk.AutoCAD.Colors.Color;
using AcColorMethod = Autodesk.AutoCAD.Colors.ColorMethod;

namespace BS.CAD.Tools.Views
{
    public static class ColorPickerDialog
    {
        private class ColorState { public short SelectedIndex; }

        public static AcColor? Show(AcColor currentColor)
        {
            short initialIndex = 7;
            try { initialIndex = currentColor.ColorIndex; if (initialIndex < 1 || initialIndex > 255) initialIndex = 7; } catch { }

            var state = new ColorState { SelectedIndex = initialIndex };
            var dlg = new System.Windows.Window
            {
                Width = 460, Height = 580, WindowStyle = System.Windows.WindowStyle.None, AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent, ResizeMode = System.Windows.ResizeMode.NoResize,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner, ShowInTaskbar = false
            };
            try { new System.Windows.Interop.WindowInteropHelper(dlg).Owner = AcadApp.MainWindow.Handle; }
            catch { dlg.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen; }
            dlg.PreviewKeyDown += (s, ke) => { if (ke.Key == System.Windows.Input.Key.Escape) { dlg.DialogResult = false; dlg.Close(); } };

            var allCells = new Dictionary<short, System.Windows.Controls.Border>();
            var mainBorder = new System.Windows.Controls.Border { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(36, 39, 41)), BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(61, 69, 75)), BorderThickness = new System.Windows.Thickness(1) };
            mainBorder.CornerRadius = new System.Windows.CornerRadius(12);

            var root = new System.Windows.Controls.Grid();
            root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

            // Title
            var titleBar = new System.Windows.Controls.Border { Height = 40, Padding = new System.Windows.Thickness(20, 0, 10, 0) };
            var titleGrid = new System.Windows.Controls.Grid();
            titleGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition());
            titleGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = System.Windows.GridLength.Auto });
            titleGrid.Children.Add(new System.Windows.Controls.TextBlock { Text = "选择颜色 (ACI)", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 237, 242)), FontSize = 14, FontWeight = System.Windows.FontWeights.SemiBold, VerticalAlignment = System.Windows.VerticalAlignment.Center });
            var closeBtn = new System.Windows.Controls.Button { Content = "✕", Width = 30, Height = 30, Background = System.Windows.Media.Brushes.Transparent, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(143, 154, 163)), BorderThickness = new System.Windows.Thickness(0), FontSize = 14, Cursor = System.Windows.Input.Cursors.Hand };
            closeBtn.Click += (s, e) => dlg.Close();
            System.Windows.Controls.Grid.SetColumn(closeBtn, 1); titleGrid.Children.Add(closeBtn);
            titleBar.Child = titleGrid;
            titleBar.MouseLeftButtonDown += (s, e) => { try { dlg.DragMove(); } catch { } };
            root.Children.Add(titleBar);

            // Preview (Enlarged text)
            var curColorVal = AcColor.FromColorIndex(AcColorMethod.ByAci, initialIndex).ColorValue;
            var curBox = new System.Windows.Controls.Border { Width = 32, Height = 32, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(curColorVal.R, curColorVal.G, curColorVal.B)), BorderBrush = System.Windows.Media.Brushes.White, BorderThickness = new System.Windows.Thickness(1) };
            curBox.CornerRadius = new System.Windows.CornerRadius(4);
            var newBox = new System.Windows.Controls.Border { Width = 32, Height = 32, Background = curBox.Background, BorderBrush = System.Windows.Media.Brushes.White, BorderThickness = new System.Windows.Thickness(1) };
            newBox.CornerRadius = new System.Windows.CornerRadius(4);

            var previewBorder = new System.Windows.Controls.Border { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 35, 38)), BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 58, 63)), BorderThickness = new System.Windows.Thickness(1), Margin = new System.Windows.Thickness(15, 0, 15, 8), Padding = new System.Windows.Thickness(12) };
            previewBorder.CornerRadius = new System.Windows.CornerRadius(8);
            var previewPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
            previewPanel.Children.Add(new System.Windows.Controls.StackPanel { Children = { curBox, new System.Windows.Controls.TextBlock { Text = $"当前: {initialIndex}", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(143, 154, 163)), FontSize = 13, FontWeight = System.Windows.FontWeights.SemiBold, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, Margin = new System.Windows.Thickness(0, 4, 0, 0) } } });
            previewPanel.Children.Add(new System.Windows.Controls.TextBlock { Text = "→", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(143, 154, 163)), FontSize = 22, VerticalAlignment = System.Windows.VerticalAlignment.Center, Margin = new System.Windows.Thickness(25, 0, 25, 12) });

            var changeTxt = new System.Windows.Controls.TextBlock { Text = $"变更: {initialIndex}", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 237, 242)), FontSize = 13, FontWeight = System.Windows.FontWeights.SemiBold, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, Margin = new System.Windows.Thickness(0, 4, 0, 0) };
            previewPanel.Children.Add(new System.Windows.Controls.StackPanel { Children = { newBox, changeTxt } });
            previewBorder.Child = previewPanel;
            System.Windows.Controls.Grid.SetRow(previewBorder, 1); root.Children.Add(previewBorder);

            // Content
            var scroll = new System.Windows.Controls.ScrollViewer { VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto, Margin = new System.Windows.Thickness(15, 0, 5, 0) };
            ApplyChromeScrollStyle(scroll);

            var contentStack = new System.Windows.Controls.StackPanel();

            Action<string, short[], double> addRow = (label, ids, size) => {
                var row = new System.Windows.Controls.WrapPanel { Margin = new System.Windows.Thickness(0, 2, 0, 2) };
                row.Children.Add(new System.Windows.Controls.TextBlock { Text = label, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(143, 154, 163)), FontSize = 10, Width = 55, VerticalAlignment = System.Windows.VerticalAlignment.Center });
                foreach (var id in ids) row.Children.Add(CreateColorCell(id, state, allCells, newBox, changeTxt, size));
                contentStack.Children.Add(row);
            };

            // Row 1: Standard
            addRow("标准色", Enumerable.Range(1, 9).Select(x => (short)x).ToArray(), 32);
            contentStack.Children.Add(new System.Windows.Controls.Border { Height = 1, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 58, 63)), Margin = new System.Windows.Thickness(0, 5, 0, 5) });

            // Row 2: Grayscale (Moved up)
            addRow("灰度", Enumerable.Range(250, 6).Select(x => (short)x).ToArray(), 32);
            contentStack.Children.Add(new System.Windows.Controls.Border { Height = 1, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 58, 63)), Margin = new System.Windows.Thickness(0, 5, 0, 5) });

            // Index colors
            for (int s = 10; s <= 240; s += 10) {
                addRow($"{s}-{s+9}", Enumerable.Range(s, 10).Select(x => (short)x).ToArray(), 30);
            }

            scroll.Content = contentStack;
            System.Windows.Controls.Grid.SetRow(scroll, 2); root.Children.Add(scroll);

            // Footer
            var footer = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new System.Windows.Thickness(15, 10, 15, 15) };
            var cancelBtn = new System.Windows.Controls.Button { Content = "取消", Width = 70, Height = 30, Background = System.Windows.Media.Brushes.Transparent, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(78, 91, 100)), BorderThickness = new System.Windows.Thickness(1), Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(143, 154, 163)), Margin = new System.Windows.Thickness(0, 0, 10, 0) };
            var okBtn = new System.Windows.Controls.Button { Content = "确定", Width = 80, Height = 30, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 132, 214)), BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 132, 214)), BorderThickness = new System.Windows.Thickness(1), Foreground = System.Windows.Media.Brushes.White, FontWeight = System.Windows.FontWeights.SemiBold };

            okBtn.Template = CreateRoundedButtonTemplate(6);
            cancelBtn.Template = CreateRoundedButtonTemplate(6);

            okBtn.Click += (s, e) => { dlg.DialogResult = true; dlg.Close(); };
            cancelBtn.Click += (s, e) => dlg.Close();

            footer.Children.Add(cancelBtn); footer.Children.Add(okBtn);
            System.Windows.Controls.Grid.SetRow(footer, 3); root.Children.Add(footer);

            mainBorder.Child = root;
            dlg.Content = mainBorder;

            if (allCells.ContainsKey(initialIndex)) { allCells[initialIndex].BorderBrush = System.Windows.Media.Brushes.White; allCells[initialIndex].BorderThickness = new System.Windows.Thickness(1.5); }
            return dlg.ShowDialog() == true ? AcColor.FromColorIndex(AcColorMethod.ByAci, state.SelectedIndex) : null;
        }

        private static System.Windows.Controls.Border CreateColorCell(short index, ColorState state, Dictionary<short, System.Windows.Controls.Border> allCells, System.Windows.Controls.Border previewBox, System.Windows.Controls.TextBlock previewTxt, double size)
        {
            var cv = AcColor.FromColorIndex(AcColorMethod.ByAci, index).ColorValue;
            var cell = new System.Windows.Controls.Border { Width = size, Height = size, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(cv.R, cv.G, cv.B)), Margin = new System.Windows.Thickness(2), BorderBrush = System.Windows.Media.Brushes.Transparent, BorderThickness = new System.Windows.Thickness(1), Cursor = System.Windows.Input.Cursors.Hand };
            cell.CornerRadius = new System.Windows.CornerRadius(4);
            short capturedIndex = index;
            cell.MouseLeftButtonDown += (s, e) => {
                foreach (var c in allCells.Values) { c.BorderBrush = System.Windows.Media.Brushes.Transparent; c.BorderThickness = new System.Windows.Thickness(1); }
                cell.BorderBrush = System.Windows.Media.Brushes.White; cell.BorderThickness = new System.Windows.Thickness(1.5);
                state.SelectedIndex = capturedIndex; previewBox.Background = cell.Background; previewTxt.Text = $"变更: {state.SelectedIndex}";
            };
            allCells[index] = cell; return cell;
        }

        private static System.Windows.Controls.ControlTemplate CreateRoundedButtonTemplate(double radius)
        {
            string xaml = $@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'>
                <Border Background='{{TemplateBinding Background}}' BorderBrush='{{TemplateBinding BorderBrush}}' BorderThickness='{{TemplateBinding BorderThickness}}' CornerRadius='{radius}'>
                    <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
                </Border></ControlTemplate>";
            return (System.Windows.Controls.ControlTemplate)System.Windows.Markup.XamlReader.Parse(xaml);
        }

        private static void ApplyChromeScrollStyle(System.Windows.Controls.ScrollViewer scroll)
        {
            string xaml = @"
            <Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ScrollBar'>
                <Setter Property='Width' Value='4'/>
                <Setter Property='Height' Value='4'/>
                <Setter Property='Background' Value='Transparent'/>
                <Setter Property='Template'>
                    <Setter.Value>
                        <ControlTemplate TargetType='ScrollBar'>
                            <Grid Background='Transparent'>
                                <Track Name='PART_Track' IsDirectionReversed='true'>
                                    <Track.Thumb>
                                        <Thumb>
                                            <Thumb.Template>
                                                <ControlTemplate TargetType='Thumb'>
                                                    <Border Background='#666666' CornerRadius='2'/>
                                                </ControlTemplate>
                                            </Thumb.Template>
                                        </Thumb>
                                    </Track.Thumb>
                                </Track>
                            </Grid>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>";
            scroll.Resources.Add(typeof(System.Windows.Controls.Primitives.ScrollBar), (System.Windows.Style)System.Windows.Markup.XamlReader.Parse(xaml));
        }
    }
}
