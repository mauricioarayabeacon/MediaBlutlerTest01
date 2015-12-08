using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess.ServiceBus
{
    public class ExtendedDataAsset : ExportDataAsset
    {
        public ExtendedDataAsset()
        {
            ThumbNails = new ThumbNailData();
        }
        // Original Name of the file without extension
        public string OriginalName { get; set; }
        // Video Area (Rodeo, Caballos, Tierra Chilena)
        public string Area { get; set; }
        // Year of the Video
        public string Year { get; set; }
        // Season&Episode. Ej: S03E01
        public string SeasonEpisode { get; set; }
        // Subprogram allow to categorize on Championship or Serie
        public string SubProgram { get; set; }
        // Event allow to create a subcategory inside subprogram or define a single event
        public string Event { get; set; }
        // EventType: indicate if it is Main Event, HighLights or Other
        public string EventType { get; set; }
        // If everything above is the same, we can differentiate with Sequential. E.g. various highlights for same event
        public string Sequential { get; set; }
        // Title
        public string VideoTitle { get; set; }

        public ThumbNailData ThumbNails { get; set; }

    }

    public class ThumbNailData
    {
        public ThumbNailData()
        {
            AssetId = "";
            ThumbNailsUris = new List<string>();
        }
        public string AssetId { get; set; }
        public List<string> ThumbNailsUris { get; set; }
    }
}
