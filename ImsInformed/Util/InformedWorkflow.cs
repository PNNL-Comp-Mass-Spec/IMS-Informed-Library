using System;
using System.Collections.Generic;
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

		private readonly int _numFrames;

		public InformedWorkflow(string uimfFileLocation, InformedParameters parameters)
		{
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
			    PeakDetectorSigNoiseRatioThreshold = 0.0001
			};
			_msFeatureFinder = new IterativeTFF(msFeatureFinderParameters);
			_numFrames = _uimfReader.GetGlobalParameters().NumFrames;
		}

		public ChargeStateCorrelationResult RunInformedWorkflow(ImsTarget target)
		{
			// Get empirical formula
			Composition targetComposition = target.Composition;
			double targetMass = target.Mass;
			string empiricalFormula = target.EmpiricalFormula;

			// Figure out frame range
			double targetNet = target.NormalizedElutionTime;
			double targetNetMin = targetNet - _parameters.NetTolerance;
			double targetNetMax = targetNet + _parameters.NetTolerance;
			int targetScanLcMin = (int) Math.Floor(targetNetMin*_numFrames);
			int targetScanLcMax = (int) Math.Ceiling(targetNetMax*_numFrames);

			for (int chargeState = 1; chargeState <= _parameters.ChargeStateMax; chargeState++)
			{
				// Calculate Target m/z
				var targetIon = new Ion(targetComposition, chargeState);
				double targetMz = targetIon.GetMz();
				double minMzForSpectrum = targetMz - 2;
				double maxMzForSpectrum = targetMz + 5;

				//Console.WriteLine("Targeting " + targetMz);

				// Generate Theoretical Isotopic Profile
				IsotopicProfile theoreticalIsotopicProfile = _theoreticalFeatureGenerator.GenerateTheorProfile(empiricalFormula, chargeState);
				List<Peak> theoreticalIsotopicProfilePeakList = theoreticalIsotopicProfile.Peaklist.Cast<Peak>().ToList();

				// Find XIC Features
				IEnumerable<FeatureBlob> featureBlobs = FindFeatures(targetMz, targetScanLcMin, targetScanLcMax);

				// Filter away small XIC peaks
				featureBlobs = FeatureDetection.FilterFeatureList(featureBlobs, 0.95);

				// TODO: NET Alignment???

				// TODO: Mass Alignment???

				// TODO: Calculate accurate NET and drift time using quadratic equation

				// TODO: Filter by NET

				// TODO: Filter by Drift Time (what if we don't have an observed drift time for this charge state)

				// Check each XIC Peak found
				foreach (var featureBlob in featureBlobs)
				{
					FeatureBlobStatistics statistics = featureBlob.CalculateStatistics();
					int unsaturatedIsotope = 0;
					FeatureBlob isotopeFeature = null;

					int scanLcMin = statistics.ScanLcMin;
					int scanLcMax = statistics.ScanLcMax;
					int scanImsMin = statistics.ScanImsMin;
					int scanImsMax = statistics.ScanImsMax;

					// TODO: Verify that there are no peaks at isotope #s 0.5 and 1.5?? (If we filter on drift time, this shouldn't actually be necessary)

					// Find an unsaturated peak in the isotopic profile
					for (int i = 1; i < 10; i++)
					{
						if (!statistics.IsSaturated) break;

						// Target isotope m/z
						double isotopeTargetMz = targetIon.GetIsotopeMz(i);

						// Find XIC Features
						IEnumerable<FeatureBlob> newFeatureBlobs = FindFeatures(isotopeTargetMz);

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
					if (statistics == null) continue;

					int scanLcRep = statistics.ScanLcRep;
					int scanImsRep = statistics.ScanImsRep;

					// Get Mass Spectrum Data
					XYData massSpectrum = GetMassSpectrum(scanLcRep, scanImsRep, minMzForSpectrum, maxMzForSpectrum);
					List<Peak> massSpectrumPeakList = _peakDetector.FindPeaks(massSpectrum);

					// Find Isotopic Profile
					List<Peak> massSpectrumPeaks;
					IsotopicProfile observedIsotopicProfile = _msFeatureFinder.IterativelyFindMSFeature(massSpectrum, theoreticalIsotopicProfile, out massSpectrumPeaks);

					// No need to move on if the isotopic profile is not found
					if (observedIsotopicProfile == null) continue;

					// If not enough peaks to reach unsaturated isotope, no need to move on
					if (observedIsotopicProfile.Peaklist.Count <= unsaturatedIsotope) continue;

					// Correct for Saturation if needed
					if (unsaturatedIsotope > 0)
					{
						IsotopicProfileUtil.AdjustSaturatedIsotopicProfile(observedIsotopicProfile, theoreticalIsotopicProfile, unsaturatedIsotope);
					}

					// Filter out flagged results
					MSPeak peakToLeft = _leftOfMonoPeakLooker.LookforPeakToTheLeftOfMonoPeak(observedIsotopicProfile.getMonoPeak(), observedIsotopicProfile.ChargeState, massSpectrumPeaks);
					if (peakToLeft != null) continue;

					double isotopicFitScore;

					// Calculate isotopic fit score
					if(unsaturatedIsotope > 0)
					{
						int unsaturatedScanLc = FindFrameNumberUseForIsotopicProfile(targetMz, scanLcRep, scanImsRep);

						if (unsaturatedScanLc > 0)
						{
							// Use the unsaturated profile if we were able to get one
							XYData unsaturatedMassSpectrum = GetMassSpectrum(unsaturatedScanLc, scanImsRep, minMzForSpectrum, maxMzForSpectrum);
							List<Peak> unsaturatedMassSpectrumPeakList = _peakDetector.FindPeaks(unsaturatedMassSpectrum);
							isotopicFitScore = _isotopicPeakFitScoreCalculator.GetFit(theoreticalIsotopicProfilePeakList, unsaturatedMassSpectrumPeakList, 0.15, _parameters.MassToleranceInPpm);
						}
						else
						{
							// Use the saturated profile
							isotopicFitScore = _isotopicPeakFitScoreCalculator.GetFit(theoreticalIsotopicProfilePeakList, massSpectrumPeakList, 0.15, _parameters.MassToleranceInPpm);
						}
					}
					else
					{
						isotopicFitScore = _isotopicPeakFitScoreCalculator.GetFit(theoreticalIsotopicProfilePeakList, massSpectrumPeakList, 0.15, _parameters.MassToleranceInPpm);
					}

					// Filter out bad isotopic fit scores
					if (isotopicFitScore > _parameters.IsotopicFitScoreMax && unsaturatedIsotope == 0) continue;

					FeatureBlob featureToUseForResult = unsaturatedIsotope > 0 ? isotopeFeature : featureBlob;

					ImsTargetResult result = new ImsTargetResult
					{
					    ChargeState = chargeState,
					    FeatureBlobStatistics = statistics,
					    IsSaturated = unsaturatedIsotope > 0,
					    IsotopicFitScore = isotopicFitScore,
					    IsotopicProfile = observedIsotopicProfile,
					    MassSpectrum = massSpectrum,
					    MonoisotopicMass = observedIsotopicProfile.MonoIsotopicMass,
					    PpmError = PeptideUtil.PpmError(targetMass, observedIsotopicProfile.MonoIsotopicMass),
					    NormalizedElutionTime = _uimfReader.ConvertFrameNumberToNormalizedElutionTime(statistics.ScanLcRep),
					    DriftTime = _uimfReader.ConvertScanNumberToDriftTime(statistics.ScanLcRep, statistics.ScanImsRep),
					    XicFeature = featureToUseForResult
					};

					target.ResultList.Add(result);

					//Console.WriteLine(chargeState + "\t" + unsaturatedIsotope + "\t" + statistics.ScanLcMin + "\t" + statistics.ScanLcMax + "\t" + statistics.ScanLcRep + "\t" + statistics.ScanImsMin + "\t" + statistics.ScanImsMax + "\t" + statistics.ScanImsRep + "\t" + isotopicFitScore.ToString("0.0000") + "\t" + result.NormalizedElutionTime.ToString("0.0000") + "\t" + result.DriftTime.ToString("0.0000"));
				}

				// TODO: Isotope Correlation (probably not going to do because of saturation issues)
			}

			// Charge State Correlation (use first unsaturated XIC feature)
			List<ChargeStateCorrelationResult> chargeStateCorrelationResultList = new List<ChargeStateCorrelationResult>();
			ChargeStateCorrelationResult bestCorrelationResult = null;
			double bestCorrelationSum = -1;

			List<ImsTargetResult> resultList = target.ResultList.OrderBy(x => x.IsotopicFitScore).ToList();
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

			// Smooth Chromatogram
			IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityPointList);
			_smoother.Smooth(ref pointList);

			// Peak Find Chromatogram
			IEnumerable<FeatureBlob> featureBlobs = FeatureDetection.DoWatershedAlgorithm(pointList);

			return featureBlobs;
		}

		private int FindFrameNumberUseForIsotopicProfile(double targetMz, int scanLcRep, int scanImsRep)
		{
			int returnScanLc = -1;
			int scanLcToTry = scanLcRep;

			while (returnScanLc < 0)
			{
				scanLcToTry--;

				// Generate Chromatogram
				List<IntensityPoint> intensityPointList = _uimfReader.GetXic(targetMz, _parameters.MassToleranceInPpm, scanLcToTry, scanLcToTry, scanImsRep, scanImsRep, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

				bool foundSaturated = intensityPointList.Any(intensityPoint => intensityPoint.IsSaturated);

				if(!foundSaturated) returnScanLc = scanLcToTry;
			}

			return returnScanLc;
		}

		private XYData GetMassSpectrum(int scanLcRep, int scanImsRep, double minMzForSpectrum, double maxMzForSpectrum)
		{
			double[] mzArray;
			int[] intensityArray;

			_uimfReader.GetSpectrum(scanLcRep, scanLcRep, DataReader.FrameType.MS1, scanImsRep, scanImsRep, minMzForSpectrum, maxMzForSpectrum, out mzArray, out intensityArray);
			double[] intensityArrayAsDoubles = XYData.ConvertIntsToDouble(intensityArray);
			XYData massSpectrum = new XYData();
			massSpectrum.SetXYValues(ref mzArray, ref intensityArrayAsDoubles);

			return massSpectrum;
		}
	}
}
