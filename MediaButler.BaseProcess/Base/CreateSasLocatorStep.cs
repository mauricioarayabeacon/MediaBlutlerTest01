using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess
{
    class CreateSasLocatorStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServiceContext;
        private IAsset myAsset;

        private void buildlocator()
        {
            myAsset = _MediaServiceContext.Assets.Where(xx => xx.Id == myRequest.AssetId).FirstOrDefault();
            var daysForWhichStreamingUrlIsActive = 365;

            var accessPolicy = _MediaServiceContext.AccessPolicies.Create(
                myAsset.Name
                , TimeSpan.FromDays(daysForWhichStreamingUrlIsActive)
                , AccessPermissions.Read);

            _MediaServiceContext.Locators.CreateLocator(LocatorType.Sas, myAsset, accessPolicy, DateTime.UtcNow.AddMinutes(-5));

            // Build Locator for jpg files
            IAsset thumbnailAsset =  myRequest.ThumbNailAsset;
            ILocator locator = _MediaServiceContext.Locators.CreateLocator(LocatorType.Sas, thumbnailAsset, accessPolicy);
            // TODO: remove from here
            var jpgFiles = thumbnailAsset.AssetFiles.ToList().
               Where(f => f.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase));

            // As a result, a set of thumbnails at 10%, 20%, …, 90% along 
            // the source timeline were generated.
            foreach (var jpg in jpgFiles)
            {
                UriBuilder ub = new UriBuilder(locator.Path);
                ub.Path += "/" + jpg.Name;
                Console.WriteLine(ub.Uri.ToString());
            }
            // tohere
        }
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            _MediaServiceContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            buildlocator();
        }

        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            Trace.TraceWarning("{0} in process {1} processId {2} has not HandleCompensation", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);

               
        }
    }
}
