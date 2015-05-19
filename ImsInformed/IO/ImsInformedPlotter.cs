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
    using System.Windows.Media.Imaging;

    using ImsInformed.Domain.DataAssociation;
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Stats;

    using OxyPlot;
    using OxyPlot.Axes;
    using OxyPlot.Series;
    using OxyPlot.Wpf;

    using LinearAxis = OxyPlot.Axes.LinearAxis;
    using LinearColorAxis = OxyPlot.Axes.LinearColorAxis;
    using LineSeries = OxyPlot.Series.LineSeries;
    using ScatterSeries = OxyPlot.Series.ScatterSeries;

    /// <summary>
    /// The ims informed plotter.
    /// </summary>
    public class ImsInformedPlotter
    {
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
        public static void PlotAssociationHypothesis(AssociationHypothesis hypothesis, string plotLocation, string datasetName, string targetDescriptor)
        {
            PlotDiagram(plotLocation, AssociationHypothesisPlot(hypothesis, datasetName, targetDescriptor));
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
        [STAThread]
        public static void PlotMobilityFit(FitLine fitline, string plotLocation)
        {
            PlotDiagram(plotLocation, MobilityFitLinePlot(fitline));
        }

        /// <summary>
        /// The plot diagram.
        /// </summary>
        /// <param name="pngLocation">
        /// The png location.
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        private static void PlotDiagram(string pngLocation, PlotModel model)
        {
            int resolution = 96;
            int width = 800;  // 1024 pixels final width
            int height = 512; // 512 pixels final height
            RenderTargetBitmap image = new RenderTargetBitmap(width * 2, height, resolution, resolution, PixelFormats.Pbgra32);
            DrawingVisual drawVisual = new DrawingVisual();
            DrawingContext drawContext = drawVisual.RenderOpen();
            
            // Output the graph models to a context
            var oe = PngExporter.ExportToBitmap(model, width, height, OxyColors.White);
            drawContext.DrawImage(oe, new Rect(0, 0, width, height));

            drawContext.Close();
            image.Render(drawVisual);

            PngBitmapEncoder png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(image));
            using (Stream stream = File.Create(pngLocation))
            {
                png.Save(stream);
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
        private static PlotModel AssociationHypothesisPlot(AssociationHypothesis hypothesis, string datasetName, string targetDescriptor)
        {
            PlotModel model = new PlotModel();
            model.TitlePadding = 0;
            model.Title = "Association Hypothesis Plot";
            model.Subtitle = datasetName + "_" + targetDescriptor;

            model.Axes.Add(
                new LinearAxis
                    {
                        Title = "IMS scan time (seconds)",
                        MajorGridlineStyle = LineStyle.Solid,
                        Position = AxisPosition.Left,
                    });

            model.Axes.Add(
                new LinearAxis
                    {
                        Title = "Pressure / (Temperature * Voltage) (1 / V))",
                        Position = AxisPosition.Bottom,
                        MajorGridlineStyle = LineStyle.Solid,
                    });

            // Add all the points
            ObservedPeak[] allPoints = hypothesis.AllObservations.ToArray();

            Func<object, ScatterPoint> fitPointMap = obj =>
            {
                ObservedPeak observation = (ObservedPeak)obj;
                ContinuousXYPoint xyPoint = observation.ToContinuousXyPoint();
                double size = 6 * observation.Statistics.IntensityScore;
                double color = hypothesis.IsOnTrack(observation) ? 0 : 1;
                ScatterPoint sp = new ScatterPoint(xyPoint.Y, xyPoint.X, size, color);
                return sp;
            };

            model.Axes.Add(new LinearColorAxis()
            {
                Position = AxisPosition.None,
                HighColor = OxyColors.Red,
                LowColor = OxyColors.Blue,
                Minimum = 0.1,
                Maximum = 0.9,
                Key = "outlierAxis",
                Palette =
                    new OxyPalette(
                    OxyColor.FromRgb(255, 0, 0),
                    OxyColor.FromRgb(153, 255, 54))
            });

            model.Series.Add(new ScatterSeries
            {
                Mapping = fitPointMap,
                ItemsSource = allPoints,
                ColorAxisKey = "outlierAxis"
            });

            var allTracks = hypothesis.Tracks;

            // Add the tracks as linear axes
            foreach (var track in allTracks)
            {
                FitLine fitline = track.FitLine;

                Func<object, DataPoint> lineMap = obj =>
                {
                    ObservedPeak observation = (ObservedPeak)obj;
                    ContinuousXYPoint xyPoint = observation.ToContinuousXyPoint();
                    double x = xyPoint.X;
                    double y = fitline.ModelPredictX2Y(x);
                    DataPoint sp = new DataPoint(y, x);
                    return sp;
                };

                model.Series.Add(new LineSeries()
                {
                    Mapping = lineMap,
                    ItemsSource = track.ObservedPeaks,
                    Color = OxyColors.Purple
                });
            }

            return model;
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
        private static PlotModel MobilityFitLinePlot(FitLine fitline)
        {

            IEnumerable<ContinuousXYPoint> fitPointList = fitline.FitPointCollection;
            IEnumerable<ContinuousXYPoint> outlierList = fitline.OutlierCollection;
            Func<object, ScatterPoint> fitPointMap = obj => 
            {
                ContinuousXYPoint point = (ContinuousXYPoint)obj;
                double size = 5;
                double color = 0;
                ScatterPoint sp = new ScatterPoint(point.Y, point.X, size, color);
                return sp;
            };

            Func<object, ScatterPoint> OutlierPointMap = obj => 
            {
                ContinuousXYPoint point = (ContinuousXYPoint)obj;
                double size = 5;
                double color = 1;
                ScatterPoint sp = new ScatterPoint(point.Y, point.X, size, color);
                return sp;
            };

            PlotModel model = new PlotModel();
            model.TitlePadding = 0;
            model.Title = "Mobility Fit FitLine";

            LinearColorAxis outlierAxis = new LinearColorAxis()
            {
                Position = AxisPosition.None,
                HighColor = OxyColors.Red,
                LowColor = OxyColors.Blue,
                Minimum = 0.1,
                Maximum = 0.9,
                Palette = new OxyPalette(OxyColor.FromRgb(255, 0, 0), OxyColor.FromRgb(153, 255, 54))
            };

            ScatterSeries fitPointSeries = new ScatterSeries
            {
                Mapping = fitPointMap,
                ItemsSource = fitPointList,
                ColorAxisKey = "outlierAxis"
            };

            ScatterSeries outlierSeries = new ScatterSeries
            {
                Mapping = OutlierPointMap,
                ItemsSource = outlierList,
                ColorAxisKey = "outlierAxis"
            };

            Func<object, DataPoint> lineMap = obj => 
            {
                ContinuousXYPoint point = (ContinuousXYPoint)obj;
                double x = point.X;
                double y = fitline.ModelPredictX2Y(x);
                DataPoint sp = new DataPoint(y, x);
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
                Title = "IMS scan time (seconds)",
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

            outlierAxis.Key = "outlierAxis";
            model.Axes.Add(outlierAxis);
            model.Axes.Add(yAxis);
            model.Axes.Add(xAxis);
            model.Series.Add(fitPointSeries);
            model.Series.Add(outlierSeries);
            model.Series.Add(fitlineSeries);
            return model;
        }
    }
}
