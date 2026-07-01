using System;
using System.Collections.Generic;
using System.Linq;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BS.CAD.Tools.Views
{
    public static class DarkComboBoxDialog
    {
        private static System.Windows.Style LoadStyle(string xaml)
        {
            return (System.Windows.Style)System.Windows.Markup.XamlReader.Parse(xaml);
        }

        private static readonly System.Windows.Style ComboItemStyle = LoadStyle(@"
<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ComboBoxItem'>
    <Setter Property='Foreground' Value='#E7EDF2'/>
    <Setter Property='Background' Value='#161B21'/>
    <Setter Property='Padding' Value='10,7'/>
    <Setter Property='HorizontalContentAlignment' Value='Left'/>
    <Setter Property='Template'>
        <Setter.Value>
            <ControlTemplate TargetType='ComboBoxItem'>
                <Border x:Name='Bd' Background='{TemplateBinding Background}' SnapsToDevicePixels='True'>
                    <ContentPresenter Margin='{TemplateBinding Padding}'/>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property='IsHighlighted' Value='True'>
                        <Setter TargetName='Bd' Property='Background' Value='#263140'/>
                    </Trigger>
                    <Trigger Property='IsSelected' Value='True'>
                        <Setter TargetName='Bd' Property='Background' Value='#2E3E57'/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>");

        private static readonly System.Windows.Style ScrollThumbStyle = LoadStyle(@"
<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Thumb'>
    <Setter Property='Template'>
        <Setter.Value>
            <ControlTemplate TargetType='Thumb'>
                <Border Background='#4A5563' BorderBrush='#687484' BorderThickness='1' CornerRadius='4'/>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>");

        private static readonly System.Windows.Style ComboScrollStyle = LoadStyle(@"
<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ScrollBar'>
    <Setter Property='Width' Value='13'/>
    <Setter Property='Background' Value='#151B22'/>
    <Setter Property='Template'>
        <Setter.Value>
            <ControlTemplate TargetType='ScrollBar'>
                <Grid Background='{TemplateBinding Background}'>
                    <Track x:Name='PART_Track' IsDirectionReversed='True' Margin='2'>
                        <Track.DecreaseRepeatButton><RepeatButton Command='ScrollBar.PageUpCommand' Opacity='0' Focusable='False'/></Track.DecreaseRepeatButton>
                        <Track.Thumb><Thumb Style='{StaticResource DarkScrollBarThumbStyleKey}'/></Track.Thumb>
                        <Track.IncreaseRepeatButton><RepeatButton Command='ScrollBar.PageDownCommand' Opacity='0' Focusable='False'/></Track.IncreaseRepeatButton>
                    </Track>
                </Grid>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>");

        private static readonly System.Windows.Style ComboStyle = LoadStyle(@"
<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ComboBox'>
    <Setter Property='Height' Value='34'/>
    <Setter Property='FontSize' Value='12'/>
    <Setter Property='Foreground' Value='#E7EDF2'/>
    <Setter Property='Background' Value='#14191F'/>
    <Setter Property='BorderBrush' Value='#38424D'/>
    <Setter Property='BorderThickness' Value='1'/>
    <Setter Property='HorizontalContentAlignment' Value='Stretch'/>
    <Setter Property='ItemContainerStyle' Value='{StaticResource DarkComboItemStyleKey}'/>
    <Setter Property='Template'>
        <Setter.Value>
            <ControlTemplate TargetType='ComboBox'>
                <Grid>
                    <Border x:Name='OuterBorder'
                            Background='{TemplateBinding Background}'
                            BorderBrush='{TemplateBinding BorderBrush}'
                            BorderThickness='{TemplateBinding BorderThickness}'
                            CornerRadius='8'/>
                    <ContentPresenter Margin='10,0,38,0'
                                      VerticalAlignment='Center'
                                      HorizontalAlignment='Left'
                                      Content='{TemplateBinding SelectionBoxItem}'
                                      ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}'
                                      ContentStringFormat='{TemplateBinding SelectionBoxItemStringFormat}'/>
                    <Border Width='34'
                            HorizontalAlignment='Right'
                            CornerRadius='0,8,8,0'
                            Background='#1D2430'
                            BorderBrush='#38424D'
                            BorderThickness='1,0,0,0'>
                        <TextBlock Text='&#x25BC;' FontSize='10'
                                   Foreground='#8F9AA3'
                                   HorizontalAlignment='Center'
                                   VerticalAlignment='Center'/>
                    </Border>
                    <ToggleButton HorizontalAlignment='Stretch' VerticalAlignment='Stretch'
                                  Focusable='False'
                                  IsChecked='{Binding IsDropDownOpen, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}'>
                        <ToggleButton.Template>
                            <ControlTemplate TargetType='ToggleButton'>
                                <Border Background='Transparent'/>
                            </ControlTemplate>
                        </ToggleButton.Template>
                    </ToggleButton>
                    <Popup x:Name='Popup' Placement='Bottom' AllowsTransparency='True'
                           Focusable='False'
                           IsOpen='{TemplateBinding IsDropDownOpen}'
                           PopupAnimation='Slide'>
                        <Border MinWidth='{Binding ActualWidth, RelativeSource={RelativeSource TemplatedParent}}'
                                MaxHeight='240' Margin='0,4,0,0'
                                Background='#12171D' BorderBrush='#38424D'
                                BorderThickness='1' CornerRadius='10'>
                            <ScrollViewer SnapsToDevicePixels='True'>
                                <ScrollViewer.Resources>
                                    <Style TargetType='ScrollBar' BasedOn='{StaticResource DarkComboScrollStyleKey}'/>
                                </ScrollViewer.Resources>
                                <StackPanel IsItemsHost='True' KeyboardNavigation.DirectionalNavigation='Contained'/>
                            </ScrollViewer>
                        </Border>
                    </Popup>
                </Grid>
                <ControlTemplate.Triggers>
                    <Trigger Property='IsMouseOver' Value='True'>
                        <Setter TargetName='OuterBorder' Property='BorderBrush' Value='#4A5A6D'/>
                    </Trigger>
                    <Trigger Property='IsKeyboardFocusWithin' Value='True'>
                        <Setter TargetName='OuterBorder' Property='BorderBrush' Value='#5A91FF'/>
                    </Trigger>
                    <Trigger Property='IsEnabled' Value='False'>
                        <Setter TargetName='OuterBorder' Property='Opacity' Value='0.55'/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>");

        public static string? Select(string title, string prompt, List<string> items, string defaultItem = "")
        {
            var (action, item) = Show(title, prompt, items, defaultItem, false, "确定");
            return action == "ok" ? item : null;
        }

        public static (string Action, string? SelectedItem) Show(
            string title, string prompt, List<string> items,
            string defaultItem = "", bool showDelete = false,
            string okButtonText = "确定")
        {
            var dlg = new System.Windows.Window
            {
                Width = 460, Height = 220,
                WindowStyle = System.Windows.WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                ResizeMode = System.Windows.ResizeMode.NoResize,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false
            };
            dlg.PreviewKeyDown += (s, ke) =>
            {
                if (ke.Key == System.Windows.Input.Key.Escape) dlg.Close();
            };

            try { new System.Windows.Interop.WindowInteropHelper(dlg).Owner = AcadApp.MainWindow.Handle; }
            catch { dlg.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen; }

            var rd = new System.Windows.ResourceDictionary();
            rd["DarkComboItemStyleKey"] = ComboItemStyle;
            rd["DarkScrollBarThumbStyleKey"] = ScrollThumbStyle;
            rd["DarkComboScrollStyleKey"] = ComboScrollStyle;
            dlg.Resources.MergedDictionaries.Add(rd);

            var outerBorder = new System.Windows.Controls.Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(29, 34, 40)),
                CornerRadius = new System.Windows.CornerRadius(12),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(61, 69, 75)),
                BorderThickness = new System.Windows.Thickness(1)
            };

            var root = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(24, 18, 24, 16) };

            root.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = title,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 246, 250)),
                FontSize = 18, FontWeight = System.Windows.FontWeights.SemiBold,
                Margin = new System.Windows.Thickness(0, 0, 0, 4)
            });

            root.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = prompt,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 180, 192)),
                FontSize = 12, Margin = new System.Windows.Thickness(0, 0, 0, 12)
            });

            var combo = new System.Windows.Controls.ComboBox
            {
                Style = ComboStyle,
                ItemContainerStyle = ComboItemStyle,
                Height = 36,
                FontSize = 13
            };
            foreach (var item in items) combo.Items.Add(item);

            if (items.Contains(defaultItem))
                combo.SelectedItem = defaultItem;
            else if (items.Count > 0)
                combo.SelectedIndex = 0;

            combo.Resources.MergedDictionaries.Add(rd);
            root.Children.Add(combo);

            var btnStyle = CreateButtonStyle();
            var btnStack = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new System.Windows.Thickness(0, 16, 0, 0)
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
                    Foreground = System.Windows.Media.Brushes.White,
                    FontWeight = System.Windows.FontWeights.SemiBold,
                    Margin = new System.Windows.Thickness(0, 0, 8, 0),
                    Style = btnStyle
                };
                deleteBtn.Click += (s, e) =>
                {
                    resultAction = "delete";
                    resultItem = combo.SelectedItem as string;
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
                Content = okButtonText,
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
                resultItem = combo.SelectedItem as string;
                dlg.DialogResult = true;
                dlg.Close();
            };
            btnStack.Children.Add(okBtn);

            root.Children.Add(btnStack);
            outerBorder.Child = root;
            dlg.Content = outerBorder;
            dlg.MouseLeftButtonDown += (s, e) => { try { dlg.DragMove(); } catch { } };
            combo.Focus();

            return dlg.ShowDialog() == true
                ? (resultAction ?? "ok", resultItem ?? (combo.SelectedItem as string))
                : ("cancel", null);
        }

        private static System.Windows.Style CreateButtonStyle()
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
            var bf = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.Border));
            bf.SetValue(System.Windows.Controls.Border.BackgroundProperty,
                new System.Windows.TemplateBindingExtension(System.Windows.Controls.Button.BackgroundProperty));
            bf.SetValue(System.Windows.Controls.Border.BorderBrushProperty,
                new System.Windows.TemplateBindingExtension(System.Windows.Controls.Button.BorderBrushProperty));
            bf.SetValue(System.Windows.Controls.Border.BorderThicknessProperty,
                new System.Windows.TemplateBindingExtension(System.Windows.Controls.Button.BorderThicknessProperty));
            bf.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new System.Windows.CornerRadius(8));
            var cp = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.ContentPresenter));
            cp.SetValue(System.Windows.Controls.ContentPresenter.HorizontalAlignmentProperty,
                System.Windows.HorizontalAlignment.Center);
            cp.SetValue(System.Windows.Controls.ContentPresenter.VerticalAlignmentProperty,
                System.Windows.VerticalAlignment.Center);
            bf.AppendChild(cp);
            var template = new System.Windows.Controls.ControlTemplate(typeof(System.Windows.Controls.Button));
            template.VisualTree = bf;
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Button.TemplateProperty, template));
            return style;
        }
    }
}
