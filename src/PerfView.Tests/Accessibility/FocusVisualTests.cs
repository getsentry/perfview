using PerfView;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using REghZyFramework.Themes;
using Xunit;

namespace PerfViewTests.Accessibility
{
    public class FocusVisualTests
    {
        private const double MinimumContrastRatio = 3.0;

        [WpfFact]
        public void AccessibleFocusVisualMeetsContrastRequirements()
        {
            AssertThemeFocusVisual(
                "LightTheme.xaml",
                Colors.White,
                GetThemeColor("LightTheme.xaml", "ContainerBackground"),
                GetThemeColor("LightTheme.xaml", "ControlDefaultBackground"));

            AssertThemeFocusVisual(
                "DarkTheme.xaml",
                GetThemeColor("DarkTheme.xaml", "ContainerBackground"),
                GetThemeColor("DarkTheme.xaml", "ControlDefaultBackground"));
        }

        [WpfFact]
        public void ReportedControlsUseAccessibleFocusVisual()
        {
            ResourceDictionary theme = LoadTheme("LightTheme.xaml");

            MainWindow mainWindow = null;
            try
            {
                App.CommandLineArgs = new CommandLineArgs();
                App.CommandProcessor = new CommandProcessor();
                mainWindow = new MainWindow(true);
                mainWindow.Resources.MergedDictionaries.Add(theme);

                Style expectedStyle = (Style)theme["AccessibleFocusVisual"];
                Assert.Same(expectedStyle, mainWindow.Body.FocusVisualStyle);

                StatusBar statusBar = mainWindow.StatusBar;
                Assert.Same(expectedStyle, ((TextBox)statusBar.FindName("m_StatusMessage")).FocusVisualStyle);
                Assert.Same(expectedStyle, ((Button)statusBar.FindName("m_LogButton")).FocusVisualStyle);
                Assert.Same(expectedStyle, ((Button)statusBar.FindName("m_CancelButton")).FocusVisualStyle);

                Hyperlink welcomeLink = mainWindow.Body.Document.Blocks
                    .OfType<Paragraph>()
                    .SelectMany(paragraph => paragraph.Inlines)
                    .OfType<Hyperlink>()
                    .First();
                Assert.Same(expectedStyle, welcomeLink.FocusVisualStyle);
            }
            finally
            {
                mainWindow?.Close();
            }
        }

        #region private

        private static void AssertThemeFocusVisual(string themeName, params Color[] adjacentColors)
        {
            ResourceDictionary theme = LoadTheme(themeName);
            SolidColorBrush focusBrush = (SolidColorBrush)theme["AccessibleFocusVisualBrush"];
            Style focusStyle = (Style)theme["AccessibleFocusVisual"];
            Setter templateSetter = focusStyle.Setters
                .OfType<Setter>()
                .Single(setter => setter.Property == Control.TemplateProperty);
            Rectangle focusRectangle = (Rectangle)((ControlTemplate)templateSetter.Value).LoadContent();

            Assert.True(focusRectangle.StrokeThickness >= 2);
            Assert.Equal(focusBrush.Color, ((SolidColorBrush)focusRectangle.Stroke).Color);

            foreach (Color adjacentColor in adjacentColors)
            {
                double contrastRatio = GetContrastRatio(focusBrush.Color, adjacentColor);
                Assert.True(
                    contrastRatio >= MinimumContrastRatio,
                    $"{themeName} focus color {focusBrush.Color} has a contrast ratio of only {contrastRatio:F3}:1 against {adjacentColor}.");
            }
        }

        private static Color GetThemeColor(string themeName, string resourceKey)
        {
            return ((SolidColorBrush)LoadTheme(themeName)[resourceKey]).Color;
        }

        private static ResourceDictionary LoadTheme(string themeName)
        {
            if (themeName == "LightTheme.xaml")
            {
                var theme = new LightTheme();
                theme.InitializeComponent();
                return theme;
            }

            if (themeName == "DarkTheme.xaml")
            {
                var theme = new DarkTheme();
                theme.InitializeComponent();
                return theme;
            }

            throw new ArgumentException($"Unknown theme '{themeName}'.", nameof(themeName));
        }

        private static double GetContrastRatio(Color first, Color second)
        {
            double firstLuminance = GetRelativeLuminance(first);
            double secondLuminance = GetRelativeLuminance(second);
            return (Math.Max(firstLuminance, secondLuminance) + 0.05) /
                (Math.Min(firstLuminance, secondLuminance) + 0.05);
        }

        private static double GetRelativeLuminance(Color color)
        {
            return (0.2126 * GetLinearChannel(color.R)) +
                (0.7152 * GetLinearChannel(color.G)) +
                (0.0722 * GetLinearChannel(color.B));
        }

        private static double GetLinearChannel(byte channel)
        {
            double value = channel / 255.0;
            return value <= 0.04045
                ? value / 12.92
                : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        #endregion
    }
}
