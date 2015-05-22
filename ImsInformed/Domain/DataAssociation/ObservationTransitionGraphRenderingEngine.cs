using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImsInformed.Domain.DataAssociation
{
    using System.IO;

    using QuickGraph.Graphviz;
    using QuickGraph.Graphviz.Dot;

    public class ObservationTransitionGraphRenderingEngine : IDotEngine
    {
        public string Run(GraphvizImageType imageType, string dot, string outputFileName)
        {
            // using (FileStream stream = new FileStream(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None))
            // {
            //     StreamWriter writer = new StreamWriter(stream);
            //     writer.WriteLine(dot);
            // }

            string outputDir = "output";

            File.WriteAllText(Path.Combine(outputDir, outputFileName + ".txt"), dot);

            return outputFileName;
        }
    }
}
