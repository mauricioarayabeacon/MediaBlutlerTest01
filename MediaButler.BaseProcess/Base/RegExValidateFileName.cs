using MediaButler.Common;
using MediaButler.Common.HostWatcher;
using MediaButler.Common.ResourceAccess;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess
{
    /// <summary>
    /// This class validates the first available file with the regular expression that obtains from
    /// parameter data. 
    /// v1.0 - 12/2015 It only works with single file check (not with control file) and it must be
    /// configured before MultiMezzanine file in the Chain.
    /// </summary>
    class RegExValidateFileNameStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServicesContext;
        private readonly object RegexOptions;
        private ValidateStepData myValidateStepData;

        private void ValidateAsset()
        {
            // This step has to be properly configured with a StepConfiguration tag in the chain configuration
            // and an entry for this Step in the Configuration Table. Else it will fail.
            if (String.IsNullOrEmpty(this.StepConfiguration))
            {
                Trace.TraceWarning(String.Format("RegExValidateFileName::ValidateAsset(), Step has not been properly configured. You have to provide RegExPattern on configuration Table"));
                throw new InvalidOperationException("RegExValidateFileName::ValidateAsset(), Step has not been properly configured. You have to provide RegExPattern on configuration Table");
            }

            // Obtain Step Configuration
            myValidateStepData = Newtonsoft.Json.JsonConvert.DeserializeObject<ValidateStepData>(this.StepConfiguration);


            // This version does not support Control File processing
            if (!string.IsNullOrEmpty(myRequest.ButlerRequest.ControlFileUri))
            {
                Trace.TraceWarning(String.Format("RegExValidateFileName::ValidateAsset(), ControlFile Uri has been provided and this is unsupported. Control File Name:{0}", myRequest.ButlerRequest.ControlFileUri));
                throw new InvalidOperationException("RegExValidateFileName::ValidateAsset(), ControlFile Uri has been provided and this is unsupported");
            }

            // Get the Mp4 reference
            Uri MezzanineFileUri = null;
            string mp4URL = myRequest.ButlerRequest.MezzanineFiles.Where(af => af.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (!string.IsNullOrEmpty(mp4URL))
            {
                //MP4
                MezzanineFileUri = new Uri(mp4URL);
            }
            else
            {
                //first file
                MezzanineFileUri = new Uri(myRequest.ButlerRequest.MezzanineFiles.FirstOrDefault());
            }

            int segmentscount = MezzanineFileUri.Segments.Count() - 1;
            string AssetName = Uri.UnescapeDataString(MezzanineFileUri.Segments[segmentscount]);


            Regex rgx = new Regex(myValidateStepData.RegExPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            string nameToValidate = AssetName.Substring(0, AssetName.Length - 4);
            MatchCollection matches = rgx.Matches(nameToValidate);

            if (matches.Count != 1)
            {

                string errorMessage = String.Format("RegExValidateFileName::ValidateAsset(), File did not match regular expression validation. \r\n Asset Name: {0} \r\n Regular Expression: {1}", nameToValidate, myValidateStepData.RegExPattern);
                MoveOffendingFileToFailed(MezzanineFileUri, errorMessage);
                Trace.TraceWarning(errorMessage);
                throw new InvalidOperationException(errorMessage);

            }
        }
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;

            _MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);

            ValidateAsset();
        }
        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            //Standard Step Compensation, just log 
            myRequest = (ButlerProcessRequest)request;

            string errorTxt = string.Format("[{0}] process Type {1} instance {2} has not compensation method", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
            Trace.TraceWarning(errorTxt);
        }

        private void MoveOffendingFileToFailed(Uri mezzanineFileUri, string errorMessage)
        {
            string storageAccountString = CloudConfigurationManager.GetSetting(Configuration.ButlerStorageConnectionConfigurationKey);
            CloudStorageAccount account = CloudStorageAccount.Parse(storageAccountString);

            CloudBlockBlob baseBlob = new CloudBlockBlob(mezzanineFileUri, account.Credentials);
            CloudBlobContainer container = baseBlob.Container;
            string directoryTo = Configuration.DirectoryFailed;
            string timestampFileAppend = DateTime.Now.ToString("yyyyMMddHHmmssff");
            // Substitute / with - in date to avoid file being treated as series of dirs
            timestampFileAppend = timestampFileAppend.Replace('/', '-');

            CloudBlockBlob fileToMove = new CloudBlockBlob(mezzanineFileUri, account.Credentials);
            var blobContinuationToken = new BlobContinuationToken();

            string blobTarget = BlobUtilities.AdjustPath(fileToMove, directoryTo);
            int trimEnd = blobTarget.LastIndexOf('.');

            string blobTargetFileExt = blobTarget.Substring(trimEnd, blobTarget.Length - trimEnd);
            blobTarget = string.Concat(blobTarget.Substring(0, trimEnd), ".", timestampFileAppend, blobTargetFileExt);
            BlobUtilities.RenameBlobWithinContainer(container, BlobUtilities.ExtractBlobPath(fileToMove), blobTarget);

            // write log file
            string blobUriString = BlobUtilities.AdjustPath(baseBlob, directoryTo);
            // remove file ext
            // append .log
            blobUriString = string.Concat(blobUriString, ".", timestampFileAppend, ".log");
            CloudBlockBlob logBlob = container.GetBlockBlobReference(blobUriString);
            logBlob.Properties.ContentType = "text/plain";
            logBlob.UploadText(errorMessage);

        }

        public class ValidateStepData
        {
            public string RegExPattern { get; set; }
        }
    }
}
