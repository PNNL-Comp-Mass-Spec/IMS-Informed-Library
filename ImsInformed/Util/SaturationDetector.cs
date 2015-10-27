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
namespace ImsInformed.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using DeconTools.Backend;
    using DeconTools.Backend.Core;
    using DeconTools.Backend.ProcessingTasks.PeakDetectors;
    using DeconTools.Backend.ProcessingTasks.TargetedFeatureFinders;
    using DeconTools.Backend.ProcessingTasks.TheorFeatureGenerator;

    using InformedProteomics.Backend.Data.Biology;
    using InformedProteomics.Backend.Data.Composition;

    using MultiDimensionalPeakFinding;
    using MultiDimensionalPeakFinding.PeakDetection;

    using UIMFLibrary;

    internal class SaturationDetector
    {
        private readonly DataReader _uimfReader;
        private readonly SavitzkyGolaySmoother _smoother;
        private readonly ChromPeakDetector _peakDetector;
        private readonly IterativeTFF _msFeatureFinder;
        private readonly ITheorFeatureGenerator _theoreticalFeatureGenerator;

        public SaturationDetector(string uimfFileLocation)
        {
            _uimfReader = new DataReader(uimfFileLocation);
            _smoother = new SavitzkyGolaySmoother(9, 2);
            _peakDetector = new ChromPeakDetector(0.0001, 0.0001);
            _theoreticalFeatureGenerator = new JoshTheorFeatureGenerator();

            if (!_uimfReader.DoesContainBinCentricData())
            {
                DataWriter dataWriter = new DataWriter(uimfFileLocation);
                dataWriter.CreateBinCentricTables();
            }

            IterativeTFFParameters msFeatureFinderParameters = new IterativeTFFParameters
            {
                MinimumRelIntensityForForPeakInclusion = 0.0000000001,
                PeakDetectorMinimumPeakBR = 0,
                PeakDetectorPeakBR = 5.00000000000002,
                PeakBRStep = 0.25,
                PeakDetectorSigNoiseRatioThreshold = 0.00000000001,
                ToleranceInPPM = 50
            };
            _msFeatureFinder = new IterativeTFF(msFeatureFinderParameters);
        }

        public void GetIntensity(string peptideSequence, double ppmTolerance)
        {
            Composition composition;

            if (peptideSequence.Equals("Tetraoctylammonium"))
            {
                composition = new Composition(32, 67, 1, 0, 0);
            }
            else if (peptideSequence.Equals("Tetraoctylammonium Bromide"))
            {
                composition = new Composition(64, 135, 2, 0, 0) + Composition.ParseFromPlainString("Br");
            }
            else
            {
                composition = PeptideUtil.GetCompositionOfPeptide(peptideSequence);
            }
            
            string empiricalFormula = composition.ToPlainString();

            for (int chargeState = 1; chargeState <= 5; chargeState++)
            {
                // Calculate Target m/z
                var targetIon = new Ion(composition, chargeState);
                double targetMz = targetIon.GetMonoIsotopicMz();
                double minMzForSpectrum = targetMz - (3.0 / chargeState);
                double maxMzForSpectrum = targetMz + (10.0 / chargeState);

                Console.WriteLine(peptideSequence + " - +" + chargeState + " - " + targetMz);

                // Generate Theoretical Isotopic Profile
                IsotopicProfile theoreticalIsotopicProfile = _theoreticalFeatureGenerator.GenerateTheorProfile(empiricalFormula, chargeState);
                List<Peak> theoreticalIsotopicProfilePeakList = theoreticalIsotopicProfile.Peaklist.Cast<Peak>().ToList();

                // Find XIC Features
                IEnumerable<FeatureBlob> featureBlobs = FindFeatures(targetMz, ppmTolerance, 1, 1);

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
                    double intensity = statistics.SumIntensities;

                    // Find an unsaturated peak in the isotopic profile
                    for (int i = 1; i < 10; i++)
                    {
                        if (!statistics.IsSaturated) break;

                        // Target isotope m/z
                        double isotopeTargetMz = targetIon.GetIsotopeMz(i);

                        // Find XIC Features
                        IEnumerable<FeatureBlob> newFeatureBlobs = FindFeatures(isotopeTargetMz, ppmTolerance, 1, 1);

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
                            if (newStatistics.ScanImsRep <= scanImsMax && newStatistics.ScanImsRep >= scanImsMin && newStatistics.ScanLcRep <= scanLcMax && newStatistics.ScanLcRep >= scanLcMin)
                            {
                                isotopeFeature = newFeatureBlob;
                                foundFeature = true;
                                break;
                            }
                        }

                        if (!foundFeature)
                        {
                            statistics = null;
                            break;
                        }

                        statistics = isotopeFeature.CalculateStatistics();
                        unsaturatedIsotope = i;
                    }

                    int scanImsRep = statistics.ScanImsRep;

                    // Get ViperCompatibleMass Spectrum Data
                    XYData massSpectrum = GetMassSpectrum(1, scanImsMin, scanImsMax, scanImsRep, minMzForSpectrum, maxMzForSpectrum);
                    //List<Peak> massSpectrumPeakList = _peakDetector.FindPeaks(massSpectrum);
                    //WriteXYDataToFile(massSpectrum, targetMz);

                    // Find Isotopic Profile
                    List<Peak> massSpectrumPeaks;
                    IsotopicProfile observedIsotopicProfile = _msFeatureFinder.IterativelyFindMSFeature(massSpectrum, theoreticalIsotopicProfile, out massSpectrumPeaks);
                    double unsaturatedIntensity = observedIsotopicProfile != null ? observedIsotopicProfile.GetSummedIntensity() : 0;

                    // Correct for Saturation if needed
                    if (unsaturatedIsotope > 0)
                    {
                        IsotopicProfileUtil.AdjustSaturatedIsotopicProfile(observedIsotopicProfile, theoreticalIsotopicProfile, unsaturatedIsotope);
                    }

                    if (observedIsotopicProfile != null && observedIsotopicProfile.MonoIsotopicMass > 1)
                    {
                        Console.WriteLine("ScanIMS = " + scanImsMin + "-" + scanImsMax + "\tImsRep = " + scanImsRep + "\tUncorrectedIntensity = " + unsaturatedIntensity + "\tIntensity = " + observedIsotopicProfile.GetSummedIntensity());
                    }
                }
            }
        }

        public void GetIntensity(double mz, double ppmTolerance)
        {
            // Find XIC Features
            IEnumerable<FeatureBlob> featureBlobs = FindFeatures(mz, ppmTolerance, 1, 1);

            // Check each XIC Peak found
            foreach (var featureBlob in featureBlobs)
            {
                FeatureBlobStatistics statistics = featureBlob.CalculateStatistics();

                if (statistics.IsSaturated)
                {
                    // TODO: Do something
                }

                int scanLcMin = statistics.ScanLcMin;
                int scanLcMax = statistics.ScanLcMax;
                int scanImsMin = statistics.ScanImsMin;
                int scanImsMax = statistics.ScanImsMax;
                double intensity = statistics.SumIntensities;

                Console.WriteLine("ScanLC = " + scanLcMin + "-" + scanLcMax + "\tScanIMS = " + scanImsMin + "-" + scanImsMax + "\tIntensity = " + intensity);
            }
        }

        private IEnumerable<FeatureBlob> FindFeatures(double targetMz, double ppmTolerance, int scanLcMin, int scanLcMax)
        {
            // Generate Chromatogram
            List<IntensityPoint> intensityPointList = _uimfReader.GetXic(targetMz, ppmTolerance, scanLcMin, scanLcMax, 0, 360, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

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

        private XYData GetMassSpectrum(int scanLcRep, int scanImsMin, int scanImsMax, int scanImsRep, double minMzForSpectrum, double maxMzForSpectrum)
        {
            double[] mzArray;
            int[] intensityArray;

            _uimfReader.GetSpectrum(1, 5, DataReader.FrameType.MS1, scanImsMin, scanImsMax, minMzForSpectrum, maxMzForSpectrum, out mzArray, out intensityArray);
            double[] intensityArrayAsDoubles = XYData.ConvertIntsToDouble(intensityArray);
            XYData massSpectrum = new XYData();
            massSpectrum.SetXYValues(ref mzArray, ref intensityArrayAsDoubles);

            return massSpectrum;
        }
    }
}
