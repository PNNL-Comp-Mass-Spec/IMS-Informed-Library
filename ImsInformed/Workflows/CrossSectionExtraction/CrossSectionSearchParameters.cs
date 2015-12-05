// The Software was produced by Battelle under Contract No. DE-AC05-76RL01830
// with the Department of Energy.  The U.S. Government is granted for itself and others 
// acting on its behalf a nonexclusive, paid-up, irrevocable worldwide license in this data 
// to reproduce, prepare derivative works, distribute copies to the public, perform 
// publicly and display publicly, and to permit others to do so.  The specific term of the 
// license can be identified by inquiry made to Battelle or DOE.  
// 
// NEITHER THE UNITED STATES NOR THE UNITED STATES DEPARTMENT OF ENERGY, 
// NOR ANY OF THEIR EMPLOYEES, MAKES ANY WARRANTY, EXPRESS OR IMPLIED,
// OR ASSUMES ANY LEGAL LIABILITY OR RESPONSIBILITY FOR THE ACCURACY, 
// COMPLETENESS OR USEFULNESS OF ANY DATA, APPARATUS, PRODUCT OR PROCESS
// DISCLOSED, OR REPRESENTS THAT ITS USE WOULD NOT INFRINGE PRIVATELY OWNED 
// RIGHTS.
namespace ImsInformed.Workflows.CrossSectionExtraction
{
    using ImsInformed.Statistics;
    using ImsInformed.Util;

    /// <summary>
    /// The molecule workflow parameters.
    /// </summary>
    public class CrossSectionSearchParameters
    {
        public const double DefaultDriftTimeToleranceInMs = 0.5;
        public const double DefaultMzWindowHalfWidthInPpm = 250;
        public const int DefaultNumPointForSmoothing = 11;
        public const double DefaultFeatureFilterLevel = 0.25;
        public const double DefaultAbsoluteIntensityThreshold = 0.00; 
        public const double DefaultRelativeIntensityPercentageThreshold = 10; 
        public const double DefaultPeakShapeThreshold = 0.4;
        public const double DefaultIsotopicThreshold = 0.4;
        public const int DefaultMaxOutliers = 1;
        public const PeakDetectorEnum DefaultPeakDetectorSelection =  PeakDetectorEnum.WaterShed;
        public const FitlineEnum DefaultRegressionSelection =  FitlineEnum.IterativelyBiSquareReweightedLeastSquares;
        public const double DefaultMinR2 = 0.96;
        public const string DefaultGraphicsExtension = "svg";

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSectionSearchParameters"/> class.
        /// </summary>
        public CrossSectionSearchParameters() : this(DefaultDriftTimeToleranceInMs, 
            DefaultMzWindowHalfWidthInPpm, 
            DefaultNumPointForSmoothing, 
            DefaultFeatureFilterLevel, 
            DefaultAbsoluteIntensityThreshold, 
            DefaultPeakShapeThreshold, 
            DefaultIsotopicThreshold, 
            DefaultMaxOutliers, 
            DefaultPeakDetectorSelection, 
            DefaultRegressionSelection,
            DefaultMinR2,
            DefaultRelativeIntensityPercentageThreshold, 
            DefaultGraphicsExtension)
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSectionSearchParameters"/> class.
        /// </summary>
        /// <param name="driftTimeToleranceInMs">
        /// The drift Time Tolerance In Ms.
        /// </param>
        /// <param name="mzWindowHalfWidthInPpm">
        /// The mass tolerance in ppm.
        /// </param>
        /// <param name="numPointForSmoothing">
        /// The num point for smoothing.
        /// </param>
        /// <param name="featureFilterLevel">
        /// The feature filter level.
        /// </param>
        /// <param name="absoluteIntensityThreshold">
        /// The intensity threshold.
        /// </param>
        /// <param name="peakShapeThreshold">
        /// The peak shape threshold.
        /// </param>
        /// <param name="isotopicThreshold">
        /// The isotopic threshold.
        /// </param>
        /// <param name="maxOutliers">
        /// The min fit points.
        /// </param>
        /// <param name="peakDetectorSelection">
        /// The peak Detector Selection.
        /// </param>
        /// <param name="regressionSelection"></param>
        /// <param name="minR2">
        /// The min R 2.
        /// </param>
        /// <param name="relativeIntensityPercentageThreshold"></param>
        /// <param name="graphicsExtension"></param>
        public CrossSectionSearchParameters(double driftTimeToleranceInMs, 
            double mzWindowHalfWidthInPpm, 
            int numPointForSmoothing, 
            double featureFilterLevel, 
            double absoluteIntensityThreshold, 
            double peakShapeThreshold, 
            double isotopicThreshold, 
            int maxOutliers, 
            PeakDetectorEnum peakDetectorSelection, 
            FitlineEnum regressionSelection,
            double minR2, 
            double relativeIntensityPercentageThreshold, 
            string graphicsExtension)
        {
            this.DriftTimeToleranceInMs = driftTimeToleranceInMs;
            this.NumPointForSmoothing = numPointForSmoothing;
            this.MzWindowHalfWidthInPpm = mzWindowHalfWidthInPpm;
            this.FeatureFilterLevel = featureFilterLevel;
            this.AbsoluteIntensityThreshold = absoluteIntensityThreshold;
            this.PeakShapeThreshold = peakShapeThreshold;
            this.IsotopicThreshold = isotopicThreshold;
            this.MaxOutliers = maxOutliers;
            this.PeakDetectorSelection = peakDetectorSelection;
            this.RegressionSelection = regressionSelection;
            this.MinR2 = minR2;
            this.RelativeIntensityPercentageThreshold = relativeIntensityPercentageThreshold;
            this.GraphicsExtension = graphicsExtension;
        }

        /// <summary>
        /// Gets or sets the mass tolerance in ppm.
        /// </summary>
        public double MzWindowHalfWidthInPpm { get; private set; }

        /// <summary>
        /// Gets or sets the feature filter level.
        /// </summary>
        public double FeatureFilterLevel { get; private set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double AbsoluteIntensityThreshold { get; private set; }

         /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double RelativeIntensityPercentageThreshold { get; private set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double PeakShapeThreshold { get; private set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double IsotopicThreshold { get; private set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public int MaxOutliers { get; private set; }

        /// <summary>
        /// Gets or sets the number point for smoothing.
        /// </summary>
        public int NumPointForSmoothing { get; private set; }

        /// <summary>
        /// Gets or sets the scan window width.
        /// </summary>
        public double DriftTimeToleranceInMs { get; private set; }

        /// <summary>
        /// Gets the peak detector selection.
        /// </summary>
        public PeakDetectorEnum PeakDetectorSelection { get; private set; }

        public FitlineEnum RegressionSelection { get; private set; }

        /// <summary>
        /// Gets the graphics format for the QC plots.
        /// </summary>
        public string GraphicsExtension{ get; private set; }

        /// <summary>
        /// The min r 2.
        /// </summary>
        public double MinR2 { get; private set; }
    }
}
