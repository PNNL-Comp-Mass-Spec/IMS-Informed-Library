// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RetentionMobilitySwappedMzMLExporter.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the RetentionMobilitySwappedMzMLExporter type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.IO
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;

    using ImsInformed.Domain.DirectInjection;

    using UIMFLibrary;

    /// <summary>
    /// The retention mobility swapped mzML exporter.
    /// </summary>
    public class RetentionMobilitySwappedMzMLExporter
    {
        private string dateTime;
        private int scans;
        private int bins;

        private double calibrationSlope;
        private double calibrationIntercept;
        private double binWidth;
        private double tofCorrectionTime;

        public bool ExportMzML(string sourceUIMFPath, string outputPath, VoltageGroup voltageGroup, DataReader originalUIMF, bool averageNotSum)
        {
            FrameParams frameParam = originalUIMF.GetFrameParams(voltageGroup.FirstFrameNumber);
            GlobalParams globalParams = originalUIMF.GetGlobalParams();

            this.scans = (int)frameParam.GetValueDouble(FrameParamKeyType.Scans);
            this.bins = (int)globalParams.GetValueDouble(GlobalParamKeyType.Bins);

            this.calibrationSlope = frameParam.GetValueDouble(FrameParamKeyType.CalibrationSlope);
            this.calibrationIntercept = frameParam.GetValueDouble(FrameParamKeyType.CalibrationIntercept);
            this.binWidth = globalParams.GetValueDouble(GlobalParamKeyType.BinWidth);
            this.tofCorrectionTime = globalParams.GetValueDouble(GlobalParamKeyType.TOFCorrectionTime);

            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            
            this.dateTime = globalParams.GetValue(GlobalParamKeyType.DateStarted);
            string datasetName = Path.GetFileNameWithoutExtension(outputPath);

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            using (XmlWriter writer = XmlWriter.Create(outputPath, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("mzML", "http://psi.hupo.org/ms/mzml");
                writer.WriteAttributeString("id", datasetName);
                writer.WriteAttributeString("version", "1.1.0");
                writer.WriteAttributeString("xmlns", "xls", string.Empty, "http://www.w3.org/2001/XMLSchema-instance");
                writer.WriteAttributeString("xmlns", "schemaLocation", string.Empty, "http://psi.hupo.org/ms/mzml http://psidev.info/files/ms/mzML/xsd/mzML1.1.0.xsd");
                this.WriteCVList(writer);
                this.WriteFileDescription(writer, sourceUIMFPath);
                this.WriteSoftwareList(writer);
                this.WriteInstrumentConfigurationList(writer);
                this.WriteDataProcessingList(writer);
                this.WriteRun(writer, outputPath, originalUIMF, voltageGroup);
                writer.WriteEndElement();
                writer.WriteEndDocument();
                return true;
            }
        }

        private void WriteCVList(XmlWriter writer)
        {
            writer.WriteStartElement("cvList");
            writer.WriteAttributeString("count", "2");
            writer.WriteStartElement("cv");
            writer.WriteAttributeString("id", "MS");
            writer.WriteAttributeString("fullName", "Proteomics Standards Initiative MonoisotopicMass Spectrometry Ontology");
            writer.WriteAttributeString("version", "3.53.0");
            writer.WriteAttributeString("URI", "http://psidev.cvs.sourceforge.net/*checkout*/psidev/psi/psi-ms/mzML/controlledVocabulary/psi-ms.obo");
            writer.WriteEndElement();

            writer.WriteStartElement("cv");
            writer.WriteAttributeString("id", "UO");
            writer.WriteAttributeString("fullName", "Unit Ontology");
            writer.WriteAttributeString("version", "12:10:2011");
            writer.WriteAttributeString("URI", "http://obo.cvs.sourceforge.net/*checkout*/obo/obo/ontology/phenotype/unit.obo");
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private void WriteFileDescription(XmlWriter writer, string UIMFPath)
        {
            writer.WriteStartElement("fileDescription");
            writer.WriteStartElement("fileContent");
            writer.WriteStartElement("cvParam");
            writer.WriteAttributeString("cvRef", "MS");
            writer.WriteAttributeString("accession", "MS:1000579");
            writer.WriteAttributeString("name", "MS1 spectrum");
            writer.WriteAttributeString("value", "");
            writer.WriteEndElement();
            writer.WriteStartElement("cvParam");
            writer.WriteAttributeString("cvRef", "MS");
            writer.WriteAttributeString("accession", "MS:1000128");
            writer.WriteAttributeString("name", "profile spectrum");
            writer.WriteAttributeString("value", "");
            writer.WriteEndElement();
            writer.WriteEndElement();


            string fileName = Path.GetFileName(UIMFPath);
            writer.WriteStartElement("sourceFileList");
            writer.WriteAttributeString("count", "1");
            writer.WriteStartElement("sourceFile");
            writer.WriteAttributeString("id", fileName);
            writer.WriteAttributeString("name", fileName);
            writer.WriteAttributeString("location", UIMFPath);
            
            // writer.WriteStartElement("cvParam");
            // writer.WriteAttributeString("cvRef", "MS");
            // writer.WriteAttributeString("accession", "MS:1002532");
            // writer.WriteAttributeString("name", "UIMF nativeID format");
            // writer.WriteAttributeString("value", "");
            // writer.WriteEndElement();

            // writer.WriteStartElement("cvParam");
            // writer.WriteAttributeString("cvRef", "MS");
            // writer.WriteAttributeString("accession", "MS:1002531");
            // writer.WriteAttributeString("name", "UIMF format");
            // writer.WriteAttributeString("value", "");
            // writer.WriteEndElement();

            // Fake it as a thermo file.
            writer.WriteStartElement("cvParam");
            writer.WriteAttributeString("cvRef", "MS");
            writer.WriteAttributeString("accession", "MS:1000768");
            writer.WriteAttributeString("name", "Thermo nativeID format");
            writer.WriteAttributeString("value", "");
            writer.WriteEndElement();

            writer.WriteStartElement("cvParam");
            writer.WriteAttributeString("cvRef", "MS");
            writer.WriteAttributeString("accession", "MS:1000563");
            writer.WriteAttributeString("name", "Thermo RAW file");
            writer.WriteAttributeString("value", "");
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private void WriteSoftwareList(XmlWriter writer)
        {
            writer.WriteStartElement("softwareList");
            writer.WriteAttributeString("count", "0");
            writer.WriteEndElement();
        }

        private void WriteInstrumentConfigurationList(XmlWriter writer)
        {
            writer.WriteStartElement("instrumentConfigurationList");
            writer.WriteAttributeString("count", "1");
            writer.WriteStartElement("instrumentConfiguration");
            writer.WriteAttributeString("id", "IC");
            writer.WriteStartElement("cvParam");
            writer.WriteAttributeString("cvRef", "MS");
            writer.WriteAttributeString("accession", "MS:1000031");
            writer.WriteAttributeString("name", "instrument model");
            writer.WriteAttributeString("value", "");
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private void WriteDataProcessingList(XmlWriter writer)
        {
            writer.WriteStartElement("dataProcessingList");
            writer.WriteAttributeString("count", "0");
            writer.WriteEndElement();
        }

        private void WriteRun(XmlWriter writer, string outputPath, DataReader reader, VoltageGroup voltageGroup)
        {
            writer.WriteStartElement("run");
            string dataset = Path.GetFileNameWithoutExtension(outputPath);
            writer.WriteAttributeString("id", dataset);
            writer.WriteAttributeString("defaultInstrumentConfigurationRef", "IC");
            writer.WriteAttributeString("startTimeStamp", this.dateTime);

            writer.WriteStartElement("spectrumList");
            writer.WriteAttributeString("count", this.scans.ToString(CultureInfo.InvariantCulture));

            int startingFrame = voltageGroup.FirstFrameNumber;
            int endingFrame = voltageGroup.FirstFrameNumber + voltageGroup.AccumulationCount - 1;
            Console.Write("Summing frame[{0} - {1}]...    ", startingFrame, endingFrame);

            double[,] summedIntensities = reader.AccumulateFrameData(startingFrame, endingFrame, false, 1, this.scans, 1, this.bins, -1, -1);

            // Use dirft time scan as LC scan to massage skyline
            for (int lcScan = 1; lcScan <= this.scans; lcScan++)
            {
                float[] mzArray;
                float[] intensityArray;

                this.GetMzIntensityArrayAtScan(summedIntensities, lcScan, out mzArray, out intensityArray);

                double mzLow = mzArray[0];
                double mzHigh = mzArray[mzArray.Count()-1];

                // Write the bins as mass spectrum
                writer.WriteStartElement("spectrum");
                writer.WriteAttributeString("index", String.Format("{0}", lcScan - 1));
                writer.WriteAttributeString("id", String.Format("frame={0} scan={1} frameType={2}", 1, lcScan, 1));
                writer.WriteAttributeString("defaultArrayLength", mzArray.Count().ToString(CultureInfo.InvariantCulture));

                writer.WriteStartElement("cvParam");
                writer.WriteAttributeString("cvRef", "MS");
                writer.WriteAttributeString("accession", "MS:1000511");
                writer.WriteAttributeString("name", "ms level");
                writer.WriteAttributeString("value", "1");
                writer.WriteEndElement();
                
                writer.WriteStartElement("cvParam");
                writer.WriteAttributeString("cvRef", "MS");
                writer.WriteAttributeString("accession", "MS:1000579");
                writer.WriteAttributeString("name", "MS1 spectrum");
                writer.WriteAttributeString("value", "");
                writer.WriteEndElement();

                writer.WriteStartElement("cvParam");
                writer.WriteAttributeString("cvRef", "MS");
                writer.WriteAttributeString("accession", "MS:1000128");
                writer.WriteAttributeString("name", "profile spectrum");
                writer.WriteAttributeString("value", "");
                writer.WriteEndElement();

                writer.WriteStartElement("scanList");
                writer.WriteAttributeString("count", "1");

                writer.WriteStartElement("cvParam");
                writer.WriteAttributeString("cvRef", "MS");
                writer.WriteAttributeString("accession", "MS:1000795");
                writer.WriteAttributeString("name", "no combination");
                writer.WriteAttributeString("value", "");
                writer.WriteEndElement();

                writer.WriteStartElement("scan");
                
                writer.WriteStartElement("cvParam");
                writer.WriteAttributeString("cvRef", "MS");
                writer.WriteAttributeString("accession", "MS:1000016");
                writer.WriteAttributeString("name", "scan start time");
                writer.WriteAttributeString("value", reader.GetDriftTime(voltageGroup.FirstFrameNumber, lcScan, true).ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("unitCvRef", "UO");
                writer.WriteAttributeString("unitAccession", "UO:0000031");
                writer.WriteAttributeString("unitName", "minute");

                writer.WriteEndElement();

                writer.WriteStartElement("scanWindowList");
                writer.WriteAttributeString("count", "1");

                writer.WriteStartElement("scanWindow");

                writer.WriteStartElement("cvParam");
                writer.WriteAttributeString("cvRef", "MS");
                writer.WriteAttributeString("accession", "MS:1000501");
                writer.WriteAttributeString("name", "scan window lower limit");
                writer.WriteAttributeString("value", mzLow.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("unitCvRef", "MS");
                writer.WriteAttributeString("unitAccession", "MS:1000040");
                writer.WriteAttributeString("unitName", "m/z");
                writer.WriteEndElement();

                writer.WriteStartElement("cvParam");
                writer.WriteAttributeString("cvRef", "MS");
                writer.WriteAttributeString("accession", "MS:1000500");
                writer.WriteAttributeString("name", "scan window upper limit");
                writer.WriteAttributeString("value", mzHigh.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("unitCvRef", "MS");
                writer.WriteAttributeString("unitAccession", "MS:1000040");
                writer.WriteAttributeString("unitName", "m/z");
                writer.WriteEndElement();

                // scan window
                writer.WriteEndElement();

                // scan window list
                writer.WriteEndElement();

                // scan 
                writer.WriteEndElement();

                // scan list
                writer.WriteEndElement();

                this.WriteBinaryDataArrays(mzArray, intensityArray, writer);
                
                // spectrum 
                writer.WriteEndElement();
            }

            // spectrum list
            writer.WriteEndElement();

            // run
            writer.WriteEndElement();
        }

        /// <summary>
        /// The get mz intensity array at scan.
        /// </summary>
        /// <param name="intensities">
        /// The intensities.
        /// </param>
        /// <param name="scan">
        /// The scan.
        /// </param>
        /// <param name="mzArray">
        /// The mz array.
        /// </param>
        /// <param name="intensityArray">
        /// The intensity array.
        /// </param>
        private void GetMzIntensityArrayAtScan(double[,] intensities, int scan, out float[] mzArray, out float[] intensityArray)
        {
            List<float> mzList = new List<float>();
            List<float> intensityList = new List<float>();
            float mz = 0;
            for (int bin = 1; bin <= this.bins; bin++) 
            {
                if (intensities[scan - 1, bin - 1] > 0.000001)
                {
                    mz = (float)DataReader.ConvertBinToMZ(
                        this.calibrationSlope,
                        this.calibrationIntercept,
                        this.binWidth,
                        this.tofCorrectionTime,
                        bin);

                    mzList.Add(mz);

                    float intensity = (float)intensities[scan - 1, bin - 1];
                    intensityList.Add(intensity);
                }
            }

            mzArray = mzList.ToArray();
            intensityArray = intensityList.ToArray();
        }

        /// <summary>
        /// The encode 32 bit float array.
        /// </summary>
        /// <param name="mzArray">
        /// The mz array.
        /// </param>
        /// <param name="encodeLength">
        /// The encode length.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private string Encode32bitFloatArray(float[] mzArray, out int encodeLength)
        {
            int precisionBits;
            string type;
            string encoded = MSDataFileReader.clsBase64EncodeDecode.EncodeNumericArray(mzArray, out precisionBits, out type);
            encodeLength = encoded.Count();
            return encoded;
        }

        /// <summary>
        /// The write binary data arrays.
        /// </summary>
        /// <param name="mzArray">
        /// The mz array.
        /// </param>
        /// <param name="intensityArray">
        /// The intensity array.
        /// </param>
        /// <param name="writer">
        /// The writer.
        /// </param>
        private void WriteBinaryDataArrays(float[] mzArray, float[] intensityArray, XmlWriter writer)
        {
            int encodedMzArraySize;
            int encodedIntensityArraySize;

            string encodedMzArray = this.Encode32bitFloatArray(mzArray, out encodedMzArraySize);
            string encodedIntensityArray = this.Encode32bitFloatArray(intensityArray, out encodedIntensityArraySize);

            writer.WriteStartElement("binaryDataArrayList");
            writer.WriteAttributeString("count", "2");

            // The MZ spectrum
            writer.WriteStartElement("binaryDataArray");
            writer.WriteAttributeString("encodedLength", encodedMzArraySize.ToString(CultureInfo.InvariantCulture));

            writer.WriteStartElement("cvParam");
            writer.WriteAttributeString("cvRef", "MS");
            writer.WriteAttributeString("accession", "MS:1000521");
            writer.WriteAttributeString("name", "32-bit float");
            writer.WriteAttributeString("value", string.Empty);
            writer.WriteEndElement();

            writer.WriteStartElement("cvParam");
            writer.WriteAttributeString("cvRef", "MS");
            writer.WriteAttributeString("accession", "MS:1000576");
            writer.WriteAttributeString("name", "no compression");
            writer.WriteAttributeString("value", string.Empty);
            writer.WriteEndElement();

            writer.WriteStartElement("cvParam");
            writer.WriteAttributeString("cvRef", "MS");
            writer.WriteAttributeString("accession", "MS:1000514");
            writer.WriteAttributeString("name", "m/z array");
            writer.WriteAttributeString("value", string.Empty);
            writer.WriteAttributeString("unitCvRef", "MS");
            writer.WriteAttributeString("unitAccession", "MS:1000040");
            writer.WriteAttributeString("unitName", "m/z");
            writer.WriteEndElement();

            writer.WriteElementString("binary", encodedMzArray);

            writer.WriteEndElement();

            // The intensity spectrum
            writer.WriteStartElement("binaryDataArray");
            writer.WriteAttributeString("encodedLength", encodedIntensityArraySize.ToString(CultureInfo.InvariantCulture));

            writer.WriteStartElement("cvParam");
            writer.WriteAttributeString("cvRef", "MS");
            writer.WriteAttributeString("accession", "MS:1000521");
            writer.WriteAttributeString("name", "32-bit float");
            writer.WriteAttributeString("value", string.Empty);
            writer.WriteEndElement();

            writer.WriteStartElement("cvParam");
            writer.WriteAttributeString("cvRef", "MS");
            writer.WriteAttributeString("accession", "MS:1000576");
            writer.WriteAttributeString("name", "no compression");
            writer.WriteAttributeString("value", string.Empty);
            writer.WriteEndElement();

            writer.WriteStartElement("cvParam");
            writer.WriteAttributeString("cvRef", "MS");
            writer.WriteAttributeString("accession", "MS:1000515");
            writer.WriteAttributeString("name", "intensity array");
            writer.WriteAttributeString("value", string.Empty);
            writer.WriteAttributeString("unitCvRef", "MS");
            writer.WriteAttributeString("unitAccession", "MS:1000131");
            writer.WriteAttributeString("unitName", "number of detector counts");
            writer.WriteEndElement();

            writer.WriteElementString("binary", encodedIntensityArray);

            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }
}
