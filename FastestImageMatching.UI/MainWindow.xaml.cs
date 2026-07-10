using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using FastestImageMatching.Core;
using FastestImageMatching.Models;

namespace FastestImageMatching.UI
{
    /// <summary>
    /// Main window for the pattern matching application
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private Mat? sourceImageMat;
        private Mat? templateImageMat;
        private Mat? resultImageMat;
        private PatternMatcher matcher;
        private List<MatchResult> lastResults;

        /// <summary>
        /// Initialize the main window
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            matcher = new PatternMatcher();
            lastResults = new List<MatchResult>();
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

                // Save results
                lastResults = results;

                // Draw results on image
                DisplayResults(results);
                StatusText.Text = $"Found {results.Count} matches in {(config.ToleranceAngle > 0 ? "rotation search" : "fixed orientation")}";
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
            ResultImage.Source = null;
            ResultsList.Items.Clear();
            StatusText.Text = "Cleared";
            
            sourceImageMat?.Dispose();
            templateImageMat?.Dispose();
            resultImageMat?.Dispose();
            sourceImageMat = null;
            templateImageMat = null;
            resultImageMat = null;
            lastResults.Clear();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (resultImageMat == null || resultImageMat.Empty())
            {
                StatusText.Text = "No result image to save. Run matching first.";
                return;
            }

            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|BMP Image (*.bmp)|*.bmp",
                    DefaultExt = ".png",
                    FileName = "match_result.png"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    Cv2.ImWrite(saveDialog.FileName, resultImageMat);
                    StatusText.Text = $"Result saved to: {saveDialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error saving result: {ex.Message}";
            }
        }

        private void DisplayResults(List<MatchResult> results)
        {
            ResultsList.Items.Clear();

            // Draw results on image
            if (sourceImageMat != null)
            {
                resultImageMat?.Dispose();
                resultImageMat = ResultVisualizer.DrawMatches(sourceImageMat, results, templateImageMat);

                // Convert and display
                try
                {
                    Mat displayMat = new Mat();
                    Cv2.Resize(resultImageMat, displayMat, new OpenCvSharp.Size(600, 400));
                    ResultImage.Source = BitmapConverter.ToBitmap(displayMat);
                    displayMat.Dispose();
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"Error displaying result: {ex.Message}";
                }
            }

            // Display results in list
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                string text = $"Match #{i}: Score={result.Score:F4} | Pos=({result.Location.X:F0},{result.Location.Y:F0})";
                if (Math.Abs(result.Angle) > 0.1)
                {
                    text += $" | Angle={result.Angle:F1}°";
                }
                ResultsList.Items.Add(text);
            }
        }

        /// <summary>
        /// Handle drag and drop for image loading
        /// </summary>
        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[]? files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files?.Length > 0)
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
                    Cv2.Resize(sourceImageMat, display, new OpenCvSharp.Size(350, 280));
                    SourceImage.Source = BitmapConverter.ToBitmap(display);
                    display?.Dispose();
                    StatusText.Text = "Source image loaded";
                }
                else if (templateImageMat == null)
                {
                    templateImageMat = image.Clone();
                    Mat display = new Mat();
                    Cv2.Resize(templateImageMat, display, new OpenCvSharp.Size(300, 250));
                    TemplateImage.Source = BitmapConverter.ToBitmap(display);
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
    }
}
