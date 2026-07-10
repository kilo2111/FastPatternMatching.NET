using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using FastestImageMatching.Core;
using FastestImageMatching.Models;

namespace FastestImageMatching.UI
{
    public partial class MainWindow : Window
    {
        private Mat sourceImageMat;
        private Mat templateImageMat;
        private PatternMatcher matcher;

        public MainWindow()
        {
            InitializeComponent();
            matcher = new PatternMatcher();
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            TargetNumberSlider.ValueChanged += (s, e) => 
                TargetNumberValue.Text = ((int)TargetNumberSlider.Value).ToString();
            ScoreThresholdSlider.ValueChanged += (s, e) => 
                ScoreThresholdValue.Text = ScoreThresholdSlider.Value.ToString("F2");
            AngleSlider.ValueChanged += (s, e) => 
                AngleValue.Text = ((int)AngleSlider.Value).ToString();
            OverlapSlider.ValueChanged += (s, e) => 
                OverlapValue.Text = OverlapSlider.Value.ToString("F2");
        }

        private void MatchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sourceImageMat == null || templateImageMat == null)
                {
                    StatusText.Text = "Please load both source and template images";
                    return;
                }

                StatusText.Text = "Matching in progress...";

                // Create config from UI values
                var config = new MatchConfig
                {
                    TargetNumber = (int)TargetNumberSlider.Value,
                    ScoreThreshold = ScoreThresholdSlider.Value,
                    ToleranceAngle = AngleSlider.Value,
                    MaxOverlapRatio = OverlapSlider.Value
                };

                // Learn template
                matcher.LearnTemplate(templateImageMat);

                // Perform matching with rotation if angle > 0
                List<MatchResult> results;
                if (config.ToleranceAngle > 0)
                {
                    results = matcher.MatchWithRotation(sourceImageMat, config);
                }
                else
                {
                    results = matcher.Match(sourceImageMat, config);
                }

                // Display results
                DisplayResults(results);
                StatusText.Text = $"Found {results.Count} matches";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SourceImage.Source = null;
            TemplateImage.Source = null;
            ResultsList.Items.Clear();
            StatusText.Text = "Cleared";
            
            sourceImageMat?.Dispose();
            templateImageMat?.Dispose();
            sourceImageMat = null;
            templateImageMat = null;
        }

        private void DisplayResults(List<MatchResult> results)
        {
            ResultsList.Items.Clear();
            foreach (var result in results)
            {
                ResultsList.Items.Add(new ResultItem
                {
                    Index = ResultsList.Items.Count,
                    Score = result.Score,
                    Angle = result.Angle,
                    PosX = result.Location.X,
                    PosY = result.Location.Y
                });
            }
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    LoadImage(files[0]);
                }
            }
        }

        private void LoadImage(string filePath)
        {
            try
            {
                Mat image = Cv2.ImRead(filePath, ImreadModes.Color);
                if (image.Empty())
                {
                    StatusText.Text = "Failed to load image";
                    return;
                }

                // Determine if it's source or template based on first load
                if (sourceImageMat == null)
                {
                    sourceImageMat = image.Clone();
                    Mat display = new Mat();
                    Cv2.Resize(sourceImageMat, display, new Size(400, 350));
                    SourceImage.Source = display.ToBitmapSource();
                    display?.Dispose();
                    StatusText.Text = "Source image loaded";
                }
                else if (templateImageMat == null)
                {
                    templateImageMat = image.Clone();
                    Mat display = new Mat();
                    Cv2.Resize(templateImageMat, display, new Size(400, 350));
                    TemplateImage.Source = display.ToBitmapSource();
                    display?.Dispose();
                    StatusText.Text = "Template image loaded";
                }
                image?.Dispose();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error loading image: {ex.Message}";
            }
        }

        private class ResultItem
        {
            public int Index { get; set; }
            public double Score { get; set; }
            public double Angle { get; set; }
            public double PosX { get; set; }
            public double PosY { get; set; }

            public override string ToString() => 
                $"#{Index}: Score={Score:F4} Angle={Angle:F1}° Pos=({PosX:F0},{PosY:F0})";
        }
    }
}
