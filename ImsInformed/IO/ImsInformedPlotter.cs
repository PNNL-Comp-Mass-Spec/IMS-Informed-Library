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
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

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
        [STAThread]
        public static void MobilityFitLine2PNG(string outputPath, FitLine line)
        {
            try
            {
                PlotDiagram(outputPath, MobilityFitLinePlot(line));
            }
            catch (Exception e)
            {
                using (StreamWriter stream = new System.IO.StreamWriter(@"c:\outputlog.txt", true) { AutoFlush = true })
                {
                    stream.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// The plot diagram.
        /// </summary>
        /// <param name="PngLocation">
        /// The png location.
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        private static void PlotDiagram(string PngLocation, PlotModel model)
        {
            int resolution = 96;
            int width = 600;  // 1024 pixels final width
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
            using (Stream stream = File.Create(PngLocation))
            {
                png.Save(stream);
            }
        }

        /// <summary>
        /// The mobility fit line plot.
        /// </summary>
        /// <param name="fitline">
        /// The fitline.
        /// </param>
        /// <returns>
        /// The <see cref="PlotModel"/>.
        /// </returns>
        private static PlotModel MobilityFitLinePlot(FitLine fitline)
        {
            IEnumerable<ContinuousXYPoint> pointList = fitline.PointCollection;
            Func<object, ScatterPoint> pointMap = obj => 
            {
                ContinuousXYPoint point = (ContinuousXYPoint)obj;
                double size = 5;
                double color = 0;
                if (point.IsOutlier)
                {
                    color = 1;
                }
                else
                {
                    color = 0;
                }

                ScatterPoint sp = new ScatterPoint(point.y, point.x, size, color);
                return sp;
            };
            PlotModel model = new PlotModel();
            model.TitlePadding = 0;
            model.Title = "Mobility Fit Line";

            LinearColorAxis outlierAxis = new LinearColorAxis()
            {
                Position = AxisPosition.None,
                HighColor = OxyColors.Red,
                LowColor = OxyColors.Blue,
                Minimum = 0.1,
                Maximum = 0.9,
                Palette = new OxyPalette(OxyColor.FromRgb(255, 0, 0), OxyColor.FromRgb(153, 255, 54) )
            };

            ScatterSeries scatterSeries = new ScatterSeries
            {
                Mapping = pointMap,
                ItemsSource = pointList,
                ColorAxisKey = "outlierAxis"
            };

            Func<object, DataPoint> lineMap = obj => 
            {
                ContinuousXYPoint point = (ContinuousXYPoint)obj;
                double x = point.x;
                double y = fitline.ModelPredict(x);
                DataPoint sp = new DataPoint(y, x);
                return sp;
            };

            LineSeries fitlineSeries = new LineSeries()
            {
                Mapping = lineMap,
                ItemsSource = pointList,
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
            model.Series.Add(scatterSeries);
            model.Series.Add(fitlineSeries);
            return model;
        }
    }
}
