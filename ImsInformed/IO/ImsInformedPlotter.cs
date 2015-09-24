// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImsInformedPlotter.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the ImsInformedPlotter type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    using ImsInformed.Domain.DataAssociation;
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Statistics;

    using OxyPlot;
    using OxyPlot.Axes;
    using OxyPlot.Series;
    using OxyPlot.Wpf;

    using HorizontalAlignment = OxyPlot.HorizontalAlignment;
    using LinearAxis = OxyPlot.Axes.LinearAxis;
    using LinearColorAxis = OxyPlot.Axes.LinearColorAxis;
    using LineAnnotation = OxyPlot.Annotations.LineAnnotation;
    using LineSeries = OxyPlot.Series.LineSeries;
    using ScatterSeries = OxyPlot.Series.ScatterSeries;
    using SvgExporter = OxyPlot.Wpf.SvgExporter;

    /// <summary>
    /// The ims informed plotter.
    /// </summary>
    internal class ImsInformedPlotter
    {
        public ImsInformedPlotter()
        {
        
        }

        /// <summary>
        /// The plot association hypothesis.
        /// </summary>
        /// <param name="hypothesis">
        /// The hypothesis.
        /// </param>
        /// <param name="plotLocation">
        /// The plot location.
        /// </param>
        [STAThread]
        public void PlotAssociationHypothesis(AssociationHypothesis hypothesis, string plotLocation, string datasetName, string targetDescriptor, IDictionary<string, IList<ObservedPeak>> preFilteredPeaks)
        {
            //int width = 450;
            //int height = 256;

            int width = 900 / 4 * 3;
            int height = 512 / 4 * 3;
            PlotModel associationHypothsisPlot = this.AssociationHypothesisPlot(hypothesis, datasetName, targetDescriptor);
            associationHypothsisPlot = this.AnnotateRemovedPeaks(associationHypothsisPlot, preFilteredPeaks);
            this.PlotDiagram(plotLocation, associationHypothsisPlot, width, height);
        }

        private PlotModel AnnotateRemovedPeaks(PlotModel associationHypothsisPlot, IDictionary<string, IList<ObservedPeak>> preFilteredPeaks)
        {
            Func<ObservedPeak, ScatterPoint> fitPointMap = obj =>
            {
                ObservedPeak observation = obj;
                ContinuousXYPoint xyPoint = observation.ToContinuousXyPoint();
                double size = MapToPointSize(observation);
                ScatterPoint sp = new ScatterPoint(xyPoint.X, xyPoint.Y, size);
                return sp;
            };

            foreach (KeyValuePair<string, IList<ObservedPeak>> pair in preFilteredPeaks)
            {
                string rejectionReason = pair.Key;
                IEnumerable<ObservedPeak> peaks = pair.Value;

                ScatterSeries series = new ScatterSeries
                {
                    Title = rejectionReason
                };
                
                associationHypothsisPlot.Series.Add(series);
                series.Points.AddRange(peaks.Select(x => fitPointMap(x))); 
            }

            return associationHypothsisPlot;
        }

        /// <summary>
        /// The plot mobility fit.
        /// </summary>
        /// <param name="fitline">
        /// The fitline.
        /// </param>
        /// <param name="plotLocation">
        /// The plot location.
        /// </param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        [STAThread]
        public void PlotMobilityFit(FitLine fitline, string plotLocation, int width, int height)
        {
            this.PlotDiagram(plotLocation, this.MobilityFitLinePlot(fitline), width, height);
        }

        /// <summary>
        /// The plot diagram.
        /// </summary>
        /// <param name="fileLocation">
        /// The png location.
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void PlotDiagram(string fileLocation, PlotModel model, int width, int height)
        {
            string extension = Path.GetExtension(fileLocation);
            if (extension == null)
            {
                throw new FileFormatException("Please specify file extension of the result picture file.");
            }
            else if (extension.ToLower() == ".png")
            {
                int resolution = 300;
                RenderTargetBitmap image = new RenderTargetBitmap(width, height, resolution, resolution, PixelFormats.Pbgra32);
                DrawingVisual drawVisual = new DrawingVisual();
                DrawingContext drawContext = drawVisual.RenderOpen();
                
                // Output the graph models to a context
                var oe = PngExporter.ExportToBitmap(model, width, height, OxyColors.White);
                drawContext.DrawImage(oe, new Rect(0, 0, width, height));

                drawContext.Close();
                image.Render(drawVisual);

                PngBitmapEncoder png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(image));
                using (Stream stream = File.Create(fileLocation))
                {
                    png.Save(stream);
                }
            }
            else if (extension.ToLower() == ".svg")
            {
                using (var stream = File.Create(fileLocation))
                {
                    var exporter = new SvgExporter() { Width = width, Height = height};
                    exporter.Export(model, stream);
                }
            }
            else
            {
                throw new FormatException(string.Format("Does not support plotting for picture format {0}.", extension));
            }
        }

        /// <summary>
        /// The association hypothesis plot.
        /// </summary>
        /// <param name="hypothesis">
        /// The hypothesis.
        /// </param>
        /// <param name="datasetName">
        /// The dataset name.
        /// </param>
        /// <param name="targetDescriptor">
        /// The target descriptor.
        /// </param>
        /// <returns>
        /// The <see cref="PlotModel"/>.
        /// </returns>
        private PlotModel AssociationHypothesisPlot(AssociationHypothesis hypothesis, string datasetName, string targetDescriptor, bool plotXAxisFromZero = false)
        {
            PlotModel model = new PlotModel();

            model.LegendBorderThickness = 0;
            model.LegendOrientation = LegendOrientation.Vertical;
            model.LegendPlacement = LegendPlacement.Inside;
            model.LegendPosition = LegendPosition.LeftTop;

            model.TitlePadding = 0;
            // model.Title = "Association Hypothesis Plot";
            // model.Subtitle = datasetName + "_" + targetDescriptor;

            model.Axes.Add(
                new LinearAxis
                    {
                        Title = "IMS arrival time (seconds)",
                        MajorGridlineStyle = LineStyle.Solid,
                        Position = AxisPosition.Left,
                    });

            model.Axes.Add(
                new LinearAxis
                    {
                        Title = "λ (Volts^-1))",
                        Position = AxisPosition.Bottom,
                        MajorGridlineStyle = LineStyle.Solid,
                    });

            // Add all the points
            IEnumerable<ObservedPeak> onTrackPeaks = hypothesis.OnTrackObservations;
            IEnumerable<ObservedPeak> offTrackPeaks = hypothesis.AllObservations.Where(x => !hypothesis.IsOnTrack(x));

            Func<ObservedPeak, ScatterPoint> fitPointMap = obj =>
            {
                ObservedPeak observation = obj;
                ContinuousXYPoint xyPoint = observation.ToContinuousXyPoint();
                double size = MapToPointSize(observation);
                ScatterPoint sp = new ScatterPoint(xyPoint.X, xyPoint.Y, size);
                return sp;
            };

            var ontrackSeries= new ScatterSeries
            {
                Title = "[Peaks On Tracks]",
                MarkerFill = OxyColors.BlueViolet
            };


            var offtrackSeries= new ScatterSeries
            {
                Title = "[Peaks Off Tracks]",
                MarkerFill = OxyColors.Red
            };

            ontrackSeries.Points.AddRange(onTrackPeaks.Select(x => fitPointMap(x))); 

            offtrackSeries.Points.AddRange(offTrackPeaks.Select(x => fitPointMap(x))); 

            model.Series.Add(ontrackSeries);

            model.Series.Add(offtrackSeries);

            var allTracks = hypothesis.Tracks;

            // Add the tracks as linear axes
            int count = 1;
            foreach (var track in allTracks)
            {
                FitLine fitline = track.FitLine;
                LineAnnotation annotation = new LineAnnotation();
                annotation.Slope = fitline.Slope;
                annotation.Intercept = fitline.Intercept;
                annotation.TextPadding = 3;
                annotation.TextMargin = 2;
                annotation.Text = string.Format("Conformer {0} - mz: {1:F2}; Isotopic Score: {2:F2}; Track Probability: {3:F2};", 
                    count, track.AverageMzInDalton, track.TrackStatistics.IsotopicScore, track.TrackProbability);
                count++;
                model.Annotations.Add(annotation);
                //Func<object, DataPoint> lineMap = obj =>
                //{
                //    ObservedPeak observation = (ObservedPeak)obj;
                //    ContinuousXYPoint xyPoint = observation.ToContinuousXyPoint();
                //    double x = xyPoint.X;
                //    double y = fitline.ModelPredictX2Y(x);
                //    DataPoint sp = new DataPoint(x, y);
                //    return sp;
                //};

                //model.Series.Add(new LineSeries()
                //{
                //    Mapping = lineMap,
                //    ItemsSource = track.ObservedPeaks,
                //    Color = OxyColors.Purple
                //});
            }

            return model;
        }

        private static double MapToPointSize(ObservedPeak observation)
        {
            double size =  8 * observation.Statistics.IntensityScore;
            return size < 2 ? 2 : size;
        }

        /// <summary>
        /// The mobility fit FitLine plot.
        /// </summary>
        /// <param name="fitline">
        /// The fitline.
        /// </param>
        /// <returns>
        /// The <see cref="PlotModel"/>.
        /// </returns>
        private PlotModel MobilityFitLinePlot(FitLine fitline)
        {

            IEnumerable<ContinuousXYPoint> fitPointList = fitline.FitPointCollection;
            IEnumerable<ContinuousXYPoint> outlierList = fitline.OutlierCollection;
            Func<object, ScatterPoint> fitPointMap = obj => 
            {
                ContinuousXYPoint point = (ContinuousXYPoint)obj;
                double size = 5;
                double color = 0;
                ScatterPoint sp = new ScatterPoint(point.X, point.Y, size, color);
                return sp;
            };

            Func<object, ScatterPoint> OutlierPointMap = obj => 
            {
                ContinuousXYPoint point = (ContinuousXYPoint)obj;
                double size = 5;
                double color = 1;
                ScatterPoint sp = new ScatterPoint(point.X, point.Y, size, color);
                return sp;
            };

            PlotModel model = new PlotModel();
            model.TitlePadding = 0;
            model.Title = "Mobility Fit FitLine";

            ScatterSeries fitPointSeries = new ScatterSeries
            {
                Mapping = fitPointMap,
                ItemsSource = fitPointList,
            };

            ScatterSeries outlierSeries = new ScatterSeries
            {
                Mapping = OutlierPointMap,
                ItemsSource = outlierList,
            };

            Func<object, DataPoint> lineMap = obj => 
            {
                ContinuousXYPoint point = (ContinuousXYPoint)obj;
                double x = point.X;
                double y = fitline.ModelPredictX2Y(x);
                DataPoint sp = new DataPoint(x, y);
                return sp;
            };

            LineSeries fitlineSeries = new LineSeries()
            {
                Mapping = lineMap,
                ItemsSource = fitPointList,
                Color = OxyColors.Purple
            };

            var yAxis = new LinearAxis()
            {
                Title = "IMS scan time (milliseconds)",
                MajorGridlineStyle = LineStyle.Solid,
                Position = AxisPosition.Left,
                
                //MajorStep = 100.0,
                //MinorStep = 50.0,
                //Minimum = 0,
                //Maximum = 360,
                //FilterMinValue = 0,
                //FilterMaxValue = 360,
            };

            var xAxis = new LinearAxis()
            {
                Title = "Pressure / (Temperature * Voltage) (1 / V))",
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Solid,
                //MajorStep = 100.0,
                //MinorStep = 50.0,
                //Minimum = 1000,
                //Maximum = 2000,
                //MinimumRange = 100.0,
                //FilterMinValue = 1000,
                //FilterMaxValue = 2000,
            };

            model.Axes.Add(yAxis);
            model.Axes.Add(xAxis);
            model.Series.Add(fitPointSeries);
            model.Series.Add(outlierSeries);
            model.Series.Add(fitlineSeries);
            return model;
        }
    }
}
