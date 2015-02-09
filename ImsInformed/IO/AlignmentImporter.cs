using System.Collections.Generic;
using System.IO;

using MathNet.Numerics.Interpolation;

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

			IInterpolation interpolation = new StepInterpolation(xValues.ToArray(), yValues.ToArray());

			return interpolation;
		}
	}
}
