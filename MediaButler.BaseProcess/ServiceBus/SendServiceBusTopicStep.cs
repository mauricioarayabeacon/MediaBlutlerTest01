using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess.ServiceBus
{
    class SendMessageTopicStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServicesContext;
        private ServiceBusData myServiceBusData;
        private ExtendedDataAsset myData;
        public override void HandleCompensation(MediaButler.Common.workflow.ChainRequest request)
        {
            //Standar Step Compesnation, only LOG
            myRequest = (ButlerProcessRequest)request;
            string errorTxt = string.Format("[{0}] process Type {1} instance {2} has not compensation method", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
            Trace.TraceWarning(errorTxt);
        }

        private void SendMessage(string jsonMessage)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(myServiceBusData.connectionString);
            // Create the topic if it does not exist already
            if (!namespaceManager.TopicExists(myServiceBusData.topicText))
            {
                namespaceManager.CreateTopic(myServiceBusData.topicText);
            }
            if (!namespaceManager.SubscriptionExists(myServiceBusData.topicText, myServiceBusData.SubscriptionName))
            {
                namespaceManager.CreateSubscription(myServiceBusData.topicText, myServiceBusData.SubscriptionName);
            }
            TopicClient Client = TopicClient.CreateFromConnectionString(myServiceBusData.connectionString, myServiceBusData.topicText);
            Client.Send(new BrokeredMessage(jsonMessage));
        }
        private void MapInfo()
        {
            myData = new ExtendedDataAsset();
            var theAsset = (from m in _MediaServicesContext.Assets select m).Where(m => m.Id == myRequest.AssetId).FirstOrDefault();

            myData.AssetId = theAsset.Id;
            myData.AlternateId = theAsset.AlternateId;

            var assetFilesALL = theAsset.AssetFiles.ToList();

            foreach (ILocator locator in theAsset.Locators)
            {
                if (locator.Type == LocatorType.OnDemandOrigin)
                {
                    var ismfile = assetFilesALL.Where(f => f.Name.ToLower().EndsWith(".ism", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    myData.Smooth = locator.Path + ismfile.Name + "/manifest";
                    myData.HLS = locator.Path + ismfile.Name + "/manifest(format=m3u8-aapl)";
                    myData.DASH = locator.Path + ismfile.Name + "/manifes(format=mpd-time-csf)";
                    myData.OriginalName = ismfile.Name.Substring(0, ismfile.Name.Length - 4);
                    // Parse name into fields. Send to Cms parsed data.
                    ParseNameIntoExtendedData(myData);
                }
            }

            if (!(myRequest.ThumbNailAsset == null)) // if there are thumbnails associated
            {
                IAsset thmbAsset = myRequest.ThumbNailAsset;
                myData.ThumbNails.AssetId = thmbAsset.Id;
                assetFilesALL = thmbAsset.AssetFiles.ToList();

                foreach (ILocator locator in thmbAsset.Locators)
                {
                    if (locator.Type == LocatorType.Sas)
                    {
                        var jpgFiles = assetFilesALL.Where(f => f.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)).ToList();
                        var jpgPrefix = locator.Path.Substring(0, locator.Path.IndexOf("?"));
                        var jpgPostFix = locator.Path.Substring(locator.Path.IndexOf("?"));
                        foreach (var assetfilejpg in jpgFiles)
                        {
                            myData.ThumbNails.ThumbNailsUris.Add(jpgPrefix + "/" + assetfilejpg.Name + jpgPostFix);
                        }
                    }
                }
            }
        }

        private void ParseNameIntoExtendedData(ExtendedDataAsset myData)
        {
            try
            {
                myData.Area = myData.OriginalName.Substring(0, 2);
                myData.Year = myData.OriginalName.Substring(3, 4);
                myData.SeasonEpisode = myData.OriginalName.Substring(8, 6);
                myData.SubProgram = myData.OriginalName.Substring(15, 5);
                myData.Event = myData.OriginalName.Substring(21, 6);
                myData.EventType = myData.OriginalName.Substring(28, 4);
                myData.Sequential = myData.OriginalName.Substring(33, 4);
                myData.VideoTitle = myData.OriginalName.Substring(38);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error parsing data en SendServicebusTopicStep::ParseNameIntoExtendedData - Operation continues. \r\n Error:{0}", ex.Message);
            }
        }

        public override void HandleExecute(MediaButler.Common.workflow.ChainRequest request)
        {
            //Standard Init Step activities
            myRequest = (ButlerProcessRequest)request;
            _MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            //Read ServiceBus configuration from Step configuration
            myServiceBusData = Newtonsoft.Json.JsonConvert.DeserializeObject<ServiceBusData>(this.StepConfiguration);
            //Map info to output
            MapInfo();
            string jsonMessage = Newtonsoft.Json.JsonConvert.SerializeObject(this.myData);
            //Send Message
            this.SendMessage(jsonMessage);
            //step finish

        }
    }

}
