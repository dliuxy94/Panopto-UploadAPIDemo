using Amazon.S3;
using Amazon.S3.Model;
using PublicAPI.REST.V46;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UploadAPIDemo
{
    class Program
    {
        private static bool hasBeenInitialized = false;
        private static long DEFAULT_PARTSIZE = 1048576;
        private static string fileName = "foobar.mp4";
        private static string userID = "foo";
        private static string userKey = "bar";
        private static string folderID = "0xDEADBEEF";
        private static string sessionName = "foobar";


        public static void Main(string[] args)
        {
            if (args.Length != 6)
            {
                usage();
                Environment.Exit(1);
            }

            Common.setServer(args[0]);
            fileName = args[1];
            userID = args[2];
            userKey = args[3];
            folderID = args[4];
            sessionName = args[5];

            EnsureCertificateValidation();

            string videoFilePath = Application.StartupPath + "\\" + fileName;

            uploadFile(userID, userKey, folderID, sessionName, videoFilePath);

            Console.ReadLine();
        }

        /// <summary>
        /// Display parameters of this program to user
        /// </summary>
        public static void usage()
        {
            Console.WriteLine("Invalid input.");
            Console.WriteLine(System.AppDomain.CurrentDomain.FriendlyName + " server fileName userName userPassword folderID sessionName");
        }

        /// <summary>
        /// Upload file to given destination
        /// </summary>
        /// <param name="userName">user name</param>
        /// <param name="userPassword">user password</param>
        /// <param name="folderID">folder to upload to</param>
        /// <param name="sessionName">upload session display name</param>
        /// <param name="filePath">file to upload</param>
        public static void uploadFile (string userName, string userPassword, string folderID, string sessionName, string filePath) 
        {
            Console.WriteLine("Authenticating...");
            string adminAuthCookie = Common.LogonAndGetCookie(userName, userPassword);

            Console.WriteLine("Creating Session...");
            string deliveryID = createSession(adminAuthCookie, folderID, sessionName);

            Console.WriteLine("Creating Upload Request...");
            Upload uploadInfo = createUpload(adminAuthCookie, deliveryID, sessionName);

            Console.WriteLine("Uploading File...");
            uploadFile(uploadInfo.UploadTarget, filePath);

            Console.WriteLine("Finishing Upload...");
            processSession(uploadInfo, adminAuthCookie, deliveryID);        
        }

        /// <summary>
        /// Create a session
        /// </summary>
        /// <param name="authCookie">stored authentication cookie</param>
        /// <param name="parentFolderID">destination folder ID</param>
        /// <param name="sessionName">name of upload session</param>
        /// <returns>Delivery ID of this upload session</returns>
        public static string createSession(string authCookie, string parentFolderID, string sessionName)
        {
            Session body = new Session(sessionName, parentFolderID);
            HttpWebRequest request = Common.CreateRequest(
                "POST", 
                "session", 
                authCookie, 
                Common.SerializeAsJson(body));

            Session response = Common.GetResponse<Session>(
                HttpStatusCode.Created,
                request);

            return response.ID.ToString();
        }

        /// <summary>
        /// Create an upload
        /// </summary>
        /// <param name="authCookie">authorization cookie</param>
        /// <param name="deliveryID">file delivery ID</param>
        /// <param name="sessionName">session display name</param>
        /// <returns>Upload struct containing upload info</returns>
        public static Upload createUpload(string authCookie, string deliveryID, string sessionName)
        {
            Upload upload = Common.CreateRestObject<Upload>(
                authCookie,
                "upload",
                new Upload()
                {
                    SessionID = deliveryID,
                    UploadTarget = sessionName
                });

            return upload;
        }

        /// <summary>
        /// Upload the file to given destination
        /// </summary>
        /// <param name="uploadTarget">Destination of upload</param>
        /// <param name="filePath">file to upload</param>
        public static void uploadFile(string uploadTarget, string filePath)
        {
            AmazonS3Client client = Common.CreateS3Client(uploadTarget);
            Amazon.S3.Model.InitiateMultipartUploadResponse response = Common.OpenUpload(client, uploadTarget, filePath);
            List<UploadPartResponse> partResponse = Common.UploadParts(client, uploadTarget, filePath, response.UploadId, DEFAULT_PARTSIZE);
            Common.CloseUpload(client, uploadTarget, filePath, response.UploadId, partResponse);
        }

        /// <summary>
        /// Finish upload and tells server to start processing session
        /// </summary>
        /// <param name="upload">upload struct containing upload info</param>
        /// <param name="authCookie">authorization cookie</param>
        /// <param name="deliveryID">upload delivery ID</param>
        public static void processSession(Upload upload, string authCookie, string deliveryID)
        {
            Process process = Common.UpdateRestObject<Process>(
                authCookie,
                "upload",
                new Process()
                {
                    SessionID = deliveryID,
                    ID = upload.ID,
                    UploadTarget = upload.UploadTarget,
                    State = 1
                });
        }

        //========================= Helper stuff

        /// <summary>
        /// Object holding information returned by REST API for session
        /// </summary>
        public class Session : BaseObject
        {
            public Session() { }

            public Session(string sessionName, string parentID) 
            {
                Name = sessionName;
                ParentFolderID = parentID;
            }

            public string Name { get; set; }

            public string ParentFolderID { get; set; }
        }

        /// <summary>
        /// Object holding information returned by REST API for upload
        /// </summary>
        public class Upload : BaseObject
        {
            public Upload() { }

            public Upload(string deliveryID, string sessionName)
            {
                SessionID = deliveryID;
                UploadTarget = sessionName;
            }
            
            public string SessionID { get; set; }

            public string UploadTarget { get; set; }
        }

        /// <summary>
        /// Object holding information returned by REST API for concluding upload
        /// </summary>
        public class Process : BaseObject
        {
            public Process() { }

            public Process(string deliveryID, string target, Guid uploadID, int uploadState)
            {
                SessionID = deliveryID;
                UploadTarget = target;
                State = uploadState;
                ID = uploadID;
            }

            public int State { get; set; }

            public string SessionID { get; set; }

            public string UploadTarget { get; set; }
        }

        /// <summary>
        /// Ensures that our custom certificate validation has been applied
        /// </summary>
        public static void EnsureCertificateValidation()
        {
            if (!hasBeenInitialized)
            {
                ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(CustomCertificateValidation);
                hasBeenInitialized = true;
            }
        }

        /// <summary>
        /// Ensures that server certificate is authenticated
        /// </summary>
        private static bool CustomCertificateValidation(object sender, X509Certificate cert, X509Chain chain, System.Net.Security.SslPolicyErrors error)
        {
            return true;
        }
    }
}
