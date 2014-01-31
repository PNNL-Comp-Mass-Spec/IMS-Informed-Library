using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.Interpolation.Algorithms;

namespace ImsInformed.IO
{
	public class AlignmentImporter
	{
		public static IInterpolation ReadFile(string fileLocation)
		{
			FileInfo fileInfo = new FileInfo(fileLocation);

			List<double> xValues = new List<double>();
			List<double> yValues = new List<double>();

			using (TextReader reader = new StreamReader(fileInfo.FullName))
			{
				string line;
				while((line = reader.ReadLine()) != null)
				{
					string[] splitLine = line.Split(',');

					double xValue = double.Parse(splitLine[0]);
					double yValue = double.Parse(splitLine[1]);

					xValues.Add(xValue);
					yValues.Add(yValue);
				}
			}

			IInterpolation interpolation = new LinearSplineInterpolation(xValues, yValues);

			return interpolation;
		}
	}
}
