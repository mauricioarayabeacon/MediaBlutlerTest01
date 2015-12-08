using MediaButler.Common;
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
         
            var accessPolicy = _MediaServiceContext.AccessPolicies.Create(
                myAsset.Name
                , TimeSpan.FromDays(Configuration.daysForWhichSasUrlIsActive)
                , AccessPermissions.Read);

            _MediaServiceContext.Locators.CreateLocator(LocatorType.Sas, myAsset, accessPolicy, DateTime.UtcNow.AddMinutes(-5));

            // Build Locator for jpg files
            IAsset thumbnailAsset =  myRequest.ThumbNailAsset;
            ILocator locator = _MediaServiceContext.Locators.CreateLocator(LocatorType.Sas, thumbnailAsset, accessPolicy);
           
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
