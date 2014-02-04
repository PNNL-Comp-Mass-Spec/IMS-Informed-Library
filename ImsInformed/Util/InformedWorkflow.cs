using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DeconTools.Backend;
using DeconTools.Backend.Core;
using DeconTools.Backend.ProcessingTasks;
using DeconTools.Backend.ProcessingTasks.FitScoreCalculators;
using DeconTools.Backend.ProcessingTasks.PeakDetectors;
using DeconTools.Backend.ProcessingTasks.ResultValidators;
using DeconTools.Backend.ProcessingTasks.TargetedFeatureFinders;
using DeconTools.Backend.ProcessingTasks.TheorFeatureGenerator;
using ImsInformed.Domain;
using ImsInformed.Parameters;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Sequence;
using MathNet.Numerics.Interpolation;
using MultiDimensionalPeakFinding;
using MultiDimensionalPeakFinding.PeakCorrelation;
using MultiDimensionalPeakFinding.PeakDetection;
using UIMFLibrary;

namespace ImsInformed.Util
{
	public class InformedWorkflow
	{
		private readonly DataReader _uimfReader;
		private readonly InformedParameters _parameters;
		private readonly SavitzkyGolaySmoother _smoother;
		private readonly IterativeTFF _msFeatureFinder;
		private readonly ITheorFeatureGenerator _theoreticalFeatureGenerator;
		private readonly LeftOfMonoPeakLooker _leftOfMonoPeakLooker;
		private readonly ChromPeakDetector _peakDetector;
		private readonly PeakLeastSquaresFitter _isotopicPeakFitScoreCalculator;

		private readonly IInterpolation _netAlignment;

		private readonly double _numFrames;
		private readonly double _numScans;

		private Stopwatch _buildWatershedStopWatch;
		private Stopwatch _smoothStopwatch;
		private Stopwatch _featureFindStopWatch;
		private double _featureFindCount;
		private double _pointCount;

		public InformedWorkflow(string uimfFileLocation, InformedParameters parameters, IInterpolation netAlignment) : this(uimfFileLocation, parameters)
		{
			_netAlignment = netAlignment;
		}

		public InformedWorkflow(string uimfFileLocation, InformedParameters parameters)
		{
			_buildWatershedStopWatch = new Stopwatch();
			_smoothStopwatch = new Stopwatch();
			_featureFindStopWatch = new Stopwatch();
			_featureFindCount = 0;
			_pointCount = 0;

			_uimfReader = new DataReader(uimfFileLocation);
			_parameters = parameters;
			_smoother = new SavitzkyGolaySmoother(parameters.NumPointForSmoothing, 2);
			_theoreticalFeatureGenerator = new JoshTheorFeatureGenerator();
			_leftOfMonoPeakLooker = new LeftOfMonoPeakLooker();
			_peakDetector = new ChromPeakDetector(0.0001, 0.0001);
			_isotopicPeakFitScoreCalculator = new PeakLeastSquaresFitter();

			IterativeTFFParameters msFeatureFinderParameters = new IterativeTFFParameters
			{
			    MinimumRelIntensityForForPeakInclusion = 0.0001,
			    PeakDetectorMinimumPeakBR = 0.0001,
			    PeakDetectorPeakBR = 5.0002,
			    PeakBRStep = 0.25,
			    PeakDetectorSigNoiseRatioThreshold = 0.0001,
				ToleranceInPPM = parameters.MassToleranceInPpm
			};
			_msFeatureFinder = new IterativeTFF(msFeatureFinderParameters);
			_numFrames = _uimfReader.GetGlobalParameters().NumFrames;
			_numScans = _uimfReader.GetFrameParameters(1).Scans;
		}

		public ChargeStateCorrelationResult RunInformedWorkflow(ImsTarget target)
		{
			// Get empirical formula
			Composition targetComposition = target.Composition;

			double targetMass = targetComposition.GetMass();
			double targetNet = target.NormalizedElutionTime;
			double targetNetMin = targetNet - _parameters.NetTolerance;
			double targetNetMax = targetNet + _parameters.NetTolerance;

			double reverseAlignedNetMin = targetNetMin;
			double reverseAlignedNetMax = targetNetMax;

			if (_netAlignment != null)
			{
				double reverseAlignedNet = GetReverseAlignedNet(targetNet);
				reverseAlignedNetMin = reverseAlignedNet - _parameters.NetTolerance;
				reverseAlignedNetMax = reverseAlignedNet + _parameters.NetTolerance;
			}

			int scanLcSearchMin = (int)Math.Floor(reverseAlignedNetMin * _numFrames);
			int scanLcSearchMax = (int)Math.Ceiling(reverseAlignedNetMax * _numFrames);

			for (int chargeState = 1; chargeState <= _parameters.ChargeStateMax; chargeState++)
			{
				// Calculate Target m/z
				var targetIon = new Ion(targetComposition, chargeState);
				double targetMz = targetIon.GetMz();

				//Console.WriteLine("Targeting " + targetMz);

				// Find XIC Features
				IEnumerable<FeatureBlob> featureBlobs = FindFeatures(targetMz, scanLcSearchMin, scanLcSearchMax);

				// Filter away small XIC peaks
				featureBlobs = FeatureDetection.FilterFeatureList(featureBlobs, 0.25);

				if(!featureBlobs.Any())
				{
					ImsTargetResult result = new ImsTargetResult
					{
						ChargeState = chargeState,
						FailureReason = FailureReason.XicNotFound
					};

					target.ResultList.Add(result);

					continue;
				}

				// Generate Theoretical Isotopic Profile
				float[] theoreticalIsotopicProfile = targetComposition.GetApproximatedIsotopomerEnvelop(5);

				// Check each XIC Peak found
				foreach (var featureBlob in featureBlobs)
				{
					// Setup result object
					ImsTargetResult result = new ImsTargetResult
					{
						ChargeState = chargeState,
						FailureReason = FailureReason.None
					};

					target.ResultList.Add(result);

					FeatureBlobStatistics statistics = featureBlob.CalculateStatistics();
					int unsaturatedIsotope = 0;
					FeatureBlob isotopeFeature = null;

					int scanLcMin = statistics.ScanLcMin;
					int scanLcMax = statistics.ScanLcMax;
					int scanImsMin = statistics.ScanImsMin;
					int scanImsMax = statistics.ScanImsMax;

					// TODO: Verify that there are no peaks at isotope #s 0.5 and 1.5?? (If we filter on drift time, this shouldn't actually be necessary)
					// TODO: Could do this at time of checking to left. 0.5 would need to be <= 0 and >= 1

					// Find an unsaturated peak in the isotopic profile
					for (int i = 1; i < 10; i++)
					{
						if (!statistics.IsSaturated) break;

						// Target isotope m/z
						double isotopeTargetMz = targetIon.GetIsotopeMz(i);

						// Find XIC Features
						IEnumerable<FeatureBlob> newFeatureBlobs = FindFeatures(isotopeTargetMz, scanLcMin - 10, scanLcMax + 10);

						// If no feature, then get out
						if (!newFeatureBlobs.Any())
						{
							statistics = null;
							break;
						}

						bool foundFeature = false;
						foreach (var newFeatureBlob in newFeatureBlobs.OrderByDescending(x => x.PointList.Count))
						{
							var newStatistics = newFeatureBlob.CalculateStatistics();
							if(newStatistics.ScanImsRep <= scanImsMax && newStatistics.ScanImsRep >= scanImsMin && newStatistics.ScanLcRep <= scanLcMax && newStatistics.ScanLcRep >= scanLcMin)
							{
								isotopeFeature = newFeatureBlob;
								foundFeature = true;
								break;
							}
						}

						if(!foundFeature)
						{
							statistics = null;
							break;
						}

						statistics = isotopeFeature.CalculateStatistics();
						unsaturatedIsotope = i;
					}

					// Bad Feature, so get out
					if (statistics == null)
					{
						result.FailureReason = FailureReason.IsotopicProfileNotFound;
						continue;
					}

					// TODO: Calculate accurate NET and drift time using quadratic equation
					int scanLcRep = statistics.ScanLcRep + 1;
					int scanImsRep = statistics.ScanImsRep;

					// Calculate NET using aligned data if applicable
					double net = scanLcRep / _numFrames;
					if (_netAlignment != null)
					{
						net = _netAlignment.Interpolate(net);
					}

					FeatureBlob featureToUseForResult = unsaturatedIsotope > 0 ? isotopeFeature : featureBlob;

					// Set data to result
					result.FeatureBlobStatistics = statistics;
					result.IsSaturated = unsaturatedIsotope > 0;
					result.ScanLcRep = statistics.ScanLcRep;
					result.NormalizedElutionTime = net;
					result.DriftTime = _uimfReader.ConvertScanNumberToDriftTime(statistics.ScanLcRep, statistics.ScanImsRep);
					result.XicFeature = featureToUseForResult;

					// Don't consider bogus results
					if (scanImsRep < 5 || scanImsRep > _numScans - 5)
					{
						result.FailureReason = FailureReason.DriftTimeError;
						continue;
					}

					// Don't consider bogus results
					if (scanLcRep < 3 || scanLcRep > _numFrames - 4)
					{
						result.FailureReason = FailureReason.ElutionTimeError;
						continue;
					}

					// TODO: Mass Alignment???

					// Filter by NET
					if (net > targetNetMax || net < targetNetMin)
					{
						result.FailureReason = FailureReason.ElutionTimeError;
						continue;
					}

					//Console.WriteLine(target.Peptide + "\t" + targetMass + "\t" + targetMz + "\t" + scanLcRep);

					// Find Isotopic Profile
					double[] observedIsotopicProfile = GetIsotopicProfile(targetIon, scanLcRep, scanImsRep, 5);
					double isotopicFitScore = IsotopicProfileUtil.GetFit(theoreticalIsotopicProfile, observedIsotopicProfile);

					// No need to move on if the isotopic profile is not found
					if (observedIsotopicProfile[0] < 1 || isotopicFitScore > 0.95)
					{
						result.FailureReason = FailureReason.IsotopicProfileNotFound;
						continue;
					}

					// Add data to result
					result.IsotopicProfile = observedIsotopicProfile;

					// TODO: Calculate a mono mass and error? Or should I not since the isotopic profile was created by looking at specific isotopes?
					//result.MonoisotopicMass = observedIsotopicProfile.MonoIsotopicMass;
					//result.PpmError = Math.Abs(PeptideUtil.PpmError(targetMass, observedIsotopicProfile.MonoIsotopicMass));

					// If the mass error is too high, then ignore
					//if (result.PpmError > _parameters.MassToleranceInPpm)
					//{
					//    result.FailureReason = FailureReason.MassError;
					//    continue;
					//}

					// Correct for Saturation if needed
					if (unsaturatedIsotope > 0)
					{
						IsotopicProfileUtil.AdjustSaturatedIsotopicProfile(observedIsotopicProfile, theoreticalIsotopicProfile, unsaturatedIsotope);
					}

					//WriteMSPeakListToFile(observedIsotopicProfile.Peaklist, targetMz);

					// Filter out flagged results
					double isotopeLeftOfMonoMz = targetIon.GetIsotopeMz(-1);
					List<IntensityPoint> pointList = _uimfReader.GetXic(isotopeLeftOfMonoMz, _parameters.MassToleranceInPpm, scanLcRep - 1, scanLcRep + 1, scanImsRep - 2, scanImsRep + 2, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);
					double intensityOfPeakToLeft = pointList.Sum(x => x.Intensity);
					if(intensityOfPeakToLeft > (observedIsotopicProfile[0] * 0.5))
					{
						result.FailureReason = FailureReason.PeakToLeft;
						continue;
					}

					// Calculate isotopic fit score
					if(unsaturatedIsotope > 0)
					{
						int unsaturatedScanLc = FindFrameNumberUseForIsotopicProfile(targetMz, scanLcRep, scanImsRep);

						if (unsaturatedScanLc > 0)
						{
							// Use the unsaturated profile if we were able to get one
							double[] unsaturatedIsotopicProfile = GetIsotopicProfile(targetIon, unsaturatedScanLc, scanImsRep, 5);
							isotopicFitScore = IsotopicProfileUtil.GetFit(theoreticalIsotopicProfile, unsaturatedIsotopicProfile);
						}
					}

					// Add data to result
					result.IsotopicFitScore = isotopicFitScore;

					// Filter out bad isotopic fit scores
					if (isotopicFitScore > _parameters.IsotopicFitScoreMax && unsaturatedIsotope == 0)
					{
						result.FailureReason = FailureReason.IsotopicFitScoreError;
						continue;
					}

					//Console.WriteLine(chargeState + "\t" + unsaturatedIsotope + "\t" + statistics.ScanLcMin + "\t" + statistics.ScanLcMax + "\t" + statistics.ScanLcRep + "\t" + statistics.ScanImsMin + "\t" + statistics.ScanImsMax + "\t" + statistics.ScanImsRep + "\t" + isotopicFitScore.ToString("0.0000") + "\t" + result.NormalizedElutionTime.ToString("0.0000") + "\t" + result.DriftTime.ToString("0.0000"));
				}

				// TODO: Isotope Correlation (probably not going to do because of saturation issues)
			}

			// Charge State Correlation (use first unsaturated XIC feature)
			List<ChargeStateCorrelationResult> chargeStateCorrelationResultList = new List<ChargeStateCorrelationResult>();
			ChargeStateCorrelationResult bestCorrelationResult = null;
			double bestCorrelationSum = -1;

			List<ImsTargetResult> resultList = target.ResultList.Where(x => x.FailureReason == FailureReason.None).OrderBy(x => x.IsotopicFitScore).ToList();
			int numResults = resultList.Count;

			for (int i = 0; i < numResults; i++)
			{
				ImsTargetResult referenceResult = resultList[i];

				ChargeStateCorrelationResult chargeStateCorrelationResult = new ChargeStateCorrelationResult(target, referenceResult);
				chargeStateCorrelationResultList.Add(chargeStateCorrelationResult);

				for (int j = i + 1; j < numResults; j++)
				{
					ImsTargetResult testResult = resultList[j];
					double correlation = FeatureCorrelator.CorrelateFeaturesUsingLc(referenceResult.XicFeature, testResult.XicFeature);
					chargeStateCorrelationResult.CorrelationMap.Add(testResult, correlation);
					//Console.WriteLine(referenceResult.FeatureBlobStatistics.ScanLcRep + "\t" + referenceResult.FeatureBlobStatistics.ScanImsRep + "\t" + testResult.FeatureBlobStatistics.ScanLcRep + "\t" + testResult.FeatureBlobStatistics.ScanImsRep + "\t" + correlation);
				}

				List<ImsTargetResult> possibleBestResultList;
				double correlationSum = chargeStateCorrelationResult.GetBestCorrelation(out possibleBestResultList);

				if(correlationSum > bestCorrelationSum)
				{
					bestCorrelationSum = correlationSum;
					bestCorrelationResult = chargeStateCorrelationResult;
				}
			}

			// TODO: Score Target

			// TODO: Quantify Target (return isotopic profile abundance)

			return bestCorrelationResult;
		}

		public void ExtractData(IEnumerable<ImsTarget> targetList)
		{
			Stopwatch fastWatch = new Stopwatch();
			Stopwatch slowWatch = new Stopwatch();

			double totalChargeStateTargets = 0;

			foreach (var target in targetList)
			{
				// Get empirical formula
				Composition targetComposition = target.Composition;

				double targetNet = target.NormalizedElutionTime;
				double targetNetMin = targetNet - _parameters.NetTolerance;
				double targetNetMax = targetNet + _parameters.NetTolerance;

				double reverseAlignedNetMin = targetNetMin;
				double reverseAlignedNetMax = targetNetMax;

				if (_netAlignment != null)
				{
					double reverseAlignedNet = GetReverseAlignedNet(targetNet);
					reverseAlignedNetMin = reverseAlignedNet - _parameters.NetTolerance;
					reverseAlignedNetMax = reverseAlignedNet + _parameters.NetTolerance;
				}

				int scanLcSearchMin = (int)Math.Floor(reverseAlignedNetMin * _numFrames);
				int scanLcSearchMax = (int)Math.Ceiling(reverseAlignedNetMax * _numFrames);

				for (int chargeState = 1; chargeState <= _parameters.ChargeStateMax; chargeState++)
				{
					// Calculate Target m/z
					var targetIon = new Ion(targetComposition, chargeState);
					double targetMz = targetIon.GetMz();

					if (targetMz > 2500) continue;

					// Generate Chromatogram Fast
					fastWatch.Start();
					List<IntensityPoint> intensityPointList = _uimfReader.GetXic(targetMz, _parameters.MassToleranceInPpm, scanLcSearchMin, scanLcSearchMax, 0, 359, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);
					fastWatch.Stop();

					// Generate Chromatogram Slow
					slowWatch.Start();
					FrameParameters frameParameters = _uimfReader.GetFrameParameters(1);
					double slope = frameParameters.CalibrationSlope;
					double intercept = frameParameters.CalibrationIntercept;
					double binWidth = _uimfReader.GetGlobalParameters().BinWidth;
					float tofCorrectionTime = _uimfReader.GetGlobalParameters().TOFCorrectionTime;

					double mzTolerance = targetMz / 1000000 * _parameters.MassToleranceInPpm;
					double lowMz = targetMz - mzTolerance;
					double highMz = targetMz + mzTolerance;

					int startBin = (int)Math.Floor(DataReader.GetBinClosestToMZ(slope, intercept, binWidth, tofCorrectionTime, lowMz)) - 1;
					int endBin = (int)Math.Ceiling(DataReader.GetBinClosestToMZ(slope, intercept, binWidth, tofCorrectionTime, highMz)) + 1;

					int[][][] frameIntensities = _uimfReader.GetIntensityBlock(scanLcSearchMin, scanLcSearchMax, DataReader.FrameType.MS1, 0, 359, startBin, endBin);
					slowWatch.Stop();

					totalChargeStateTargets++;
				}
			}

			double fastTimePerTarget = fastWatch.ElapsedMilliseconds / totalChargeStateTargets;
			double slowTimePerTarget = slowWatch.ElapsedMilliseconds / totalChargeStateTargets;

			Console.WriteLine("Num Targets = " + targetList.Count());
			Console.WriteLine("Num CS Targets = " + totalChargeStateTargets);
			Console.WriteLine("Fast = " + fastTimePerTarget + " ms per target.");
			Console.WriteLine("Slow = " + slowTimePerTarget + " ms per target.");
		}

		private IEnumerable<FeatureBlob> FindFeatures(double targetMz)
		{
			// Generate Chromatogram
			List<IntensityPoint> intensityPointList = _uimfReader.GetXic(targetMz, _parameters.MassToleranceInPpm, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

			// Smooth Chromatogram
			IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityPointList);
			_smoother.Smooth(ref pointList);

			// Peak Find Chromatogram
			IEnumerable<FeatureBlob> featureBlobs = FeatureDetection.DoWatershedAlgorithm(pointList);

			return featureBlobs;
		}

		private IEnumerable<FeatureBlob> FindFeatures(double targetMz, int scanLcMin, int scanLcMax)
		{
			// Generate Chromatogram
			List<IntensityPoint> intensityPointList = _uimfReader.GetXic(targetMz, _parameters.MassToleranceInPpm, scanLcMin, scanLcMax, 0, 360, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

			if (intensityPointList == null || intensityPointList.Count == 0)
			{
				return new List<FeatureBlob>();
			}
		
			//WritePointsToFile(intensityPointList, targetMz);	

			// Smooth Chromatogram
			//_buildWatershedStopWatch.Start();
			IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityPointList);
			//_buildWatershedStopWatch.Stop();

			//_smoothStopwatch.Start();
			_smoother.Smooth(ref pointList);
			//_smoothStopwatch.Stop();

			// Peak Find Chromatogram
			//_featureFindStopWatch.Start();
			IEnumerable<FeatureBlob> featureBlobs = FeatureDetection.DoWatershedAlgorithm(pointList);
			//_featureFindStopWatch.Stop();

			//_featureFindCount++;
			//_pointCount += pointList.Count();

			return featureBlobs;
		}

		public void PrintFeatureFindStatistics()
		{
			double buildWatershedTime = _buildWatershedStopWatch.ElapsedMilliseconds / _featureFindCount;
			double smoothTime = _smoothStopwatch.ElapsedMilliseconds / _featureFindCount;
			double featureFindTime = _featureFindStopWatch.ElapsedMilliseconds / _featureFindCount;

			Console.WriteLine("Num XICs Searched = " + _featureFindCount);
			Console.WriteLine("Num Points Searched = " + _pointCount);
			Console.WriteLine("Build Watershed = " + buildWatershedTime + " ms per XIC.");
			Console.WriteLine("Smooth = " + smoothTime + " ms per XIC.");
			Console.WriteLine("Feature Find = " + featureFindTime + " ms per XIC.");
		}

		private void WritePointsToFile(IEnumerable<IntensityPoint> intensityPointList, double targetMz)
		{
			intensityPointList = intensityPointList.OrderBy(x => x.ScanLc).ThenBy(x => x.ScanIms);
			
			int minScanLc = intensityPointList.First().ScanLc;

			StringBuilder stringBuilder = new StringBuilder();

			using (StreamWriter writer = new StreamWriter("points" + Math.Round(targetMz, 3) + ".csv"))
			{
				int currentScanLc = minScanLc;
				int currentScanIms = 0;

				stringBuilder.Append(",");
				for (int i = 0; i < 360; i++)
				{
					stringBuilder.Append(i + ",");
				}

				stringBuilder.Append("\n" + currentScanLc + ",");

				foreach (var intensityPoint in intensityPointList)
				{
					int pointScanLc = intensityPoint.ScanLc;
					int pointScanIms = intensityPoint.ScanIms;
					double intensity = intensityPoint.Intensity;

					while (pointScanLc > currentScanLc)
					{
						while (currentScanIms < 360)
						{
							stringBuilder.Append("0,");
							currentScanIms++;
						}

						stringBuilder.Append("\n");
						currentScanIms = 0;
						currentScanLc++;

						stringBuilder.Append(currentScanLc + ",");
					}

					while (pointScanIms > currentScanIms)
					{
						stringBuilder.Append("0,");
						currentScanIms++;
					}

					stringBuilder.Append(intensity + ",");
					currentScanIms++;
				}

				while (currentScanIms < 360)
				{
					stringBuilder.Append("0,");
					currentScanIms++;
				}

				stringBuilder.Append("\n");

				writer.Write(stringBuilder.ToString());
			}
		}

		private void WriteXYDataToFile(XYData xyData, double targetMz)
		{
			double[] xValues = xyData.Xvalues;
			double[] yValues = xyData.Yvalues;

			using (StreamWriter writer = new StreamWriter("xydata" + Math.Round(targetMz, 3) + ".csv"))
			{
				for(int i = 0; i < xValues.Length; i++)
				{
					writer.WriteLine(Math.Round(xValues[i], 3) + "," + yValues[i]);
				}
			}
		}

		private void WriteMSPeakListToFile(IEnumerable<MSPeak> peakList, double targetMz)
		{
			using (StreamWriter writer = new StreamWriter("mspeaks" + Math.Round(targetMz, 3) + ".csv"))
			{
				foreach (var msPeak in peakList)
				{
					writer.WriteLine(Math.Round(msPeak.XValue, 3) + "," + msPeak.Height);
				}
			}
		}

		private int FindFrameNumberUseForIsotopicProfile(double targetMz, int scanLcRep, int scanImsRep)
		{
			int returnScanLc = -1;
			int scanLcToTry = scanLcRep;

			while (returnScanLc < 0)
			{
				scanLcToTry--;

				// Quit looking if we get to the beginning of the spectrum
				if (scanLcToTry <= 0) break;

				// Generate Chromatogram
				List<IntensityPoint> intensityPointList = _uimfReader.GetXic(targetMz, _parameters.MassToleranceInPpm, scanLcToTry, scanLcToTry, scanImsRep, scanImsRep, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

				bool foundSaturated = intensityPointList.Any(intensityPoint => intensityPoint.IsSaturated);

				if(!foundSaturated) returnScanLc = scanLcToTry;
			}

			// This means searching to the left failed, so search to the right
			if(returnScanLc < 0)
			{
				scanLcToTry = scanLcRep;

				while (returnScanLc < 0)
				{
					scanLcToTry++;

					// Generate Chromatogram
					List<IntensityPoint> intensityPointList = _uimfReader.GetXic(targetMz, _parameters.MassToleranceInPpm, scanLcToTry, scanLcToTry, scanImsRep, scanImsRep, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

					bool foundSaturated = intensityPointList.Any(intensityPoint => intensityPoint.IsSaturated);

					if (!foundSaturated) returnScanLc = scanLcToTry;
				}
			}

			return returnScanLc;
		}

		private XYData GetMassSpectrum(int scanLcRep, int scanImsRep, double minMzForSpectrum, double maxMzForSpectrum)
		{
			double[] mzArray;
			int[] intensityArray;

			_uimfReader.GetSpectrum(scanLcRep - 1, scanLcRep + 1, DataReader.FrameType.MS1, scanImsRep - 2, scanImsRep + 2, minMzForSpectrum, maxMzForSpectrum, out mzArray, out intensityArray);
			double[] intensityArrayAsDoubles = XYData.ConvertIntsToDouble(intensityArray);
			XYData massSpectrum = new XYData();
			massSpectrum.SetXYValues(ref mzArray, ref intensityArrayAsDoubles);

			return massSpectrum;
		}

		private double[] GetIsotopicProfile(Ion ion, int scanLcRep, int scanImsRep, int numIsotopes)
		{
			double[] isotopicProfile = new double[numIsotopes];

			for (int i = 0; i < numIsotopes; i++)
			{
				double targetMz = ion.GetIsotopeMz(i);
				List<IntensityPoint> intensityPointList = _uimfReader.GetXic(targetMz, _parameters.MassToleranceInPpm, scanLcRep - 1, scanLcRep + 1, scanImsRep - 2, scanImsRep + 2, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

				double intensitySum = intensityPointList.Sum(x => x.Intensity);

				// If we reach an isotope with no intensity, no point in still looking
				if (intensitySum < 1) break;

				isotopicProfile[i] = intensitySum;
			}

			return isotopicProfile;
		}

		private double GetReverseAlignedNet(double net)
		{
			double difference = 2;

			for (double d = 0; d <= 1; d += 0.01)
			{
				double alignedNet = _netAlignment.Interpolate(d);
				double newDifference = Math.Abs(net - alignedNet);

				if (newDifference > difference) return d;

				difference = newDifference;
			}

			return 0;
		}
	}
}
