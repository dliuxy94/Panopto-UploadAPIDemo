using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UploadAPIDemo;

namespace UploadAPIDemo
{
    class Program
    {
        private static bool selfSigned = true; // Target server is a self-signed server
        private static bool hasBeenInitialized = false;
        private static long DEFAULT_PARTSIZE = 1048576; // Size of each upload in the multipart upload process
        private static string filePath = "foobar.mp4";
        private static string userID = "foo";
        private static string userKey = "bar";
        private static string folderID = "0xDEADBEEF";
        private static string sessionName = "foobar";


        public static void Main(string[] args)
        {
            if (args.Length != 6 && args.Length != 0)
            {
                Usage();
                Environment.Exit(1);
            }

            if (args.Length == 6)
            {
                Common.SetServer(args[0]);
                filePath = args[1];
                userID = args[2];
                userKey = args[3];
                folderID = args[4];
                sessionName = args[5];
            }

            if (selfSigned)
            {
                // For self-signed servers
                EnsureCertificateValidation();
            }

            string absoluteFilePath = Path.GetFullPath(filePath);

            UploadAPIWrapper.UploadFile(userID, userKey, folderID, sessionName, absoluteFilePath, DEFAULT_PARTSIZE);

            Console.WriteLine();
            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }

        /// <summary>
        /// Display parameters of this program to user
        /// </summary>
        public static void Usage()
        {
            Console.WriteLine("Invalid input.");
            Console.WriteLine(System.AppDomain.CurrentDomain.FriendlyName + " [server] [filePath] [userName] [userPassword] [folderID] [sessionName]");
            Console.WriteLine();
            Console.WriteLine("\tserver: Target server");
            Console.WriteLine("\tfilePath: Target upload file path relative to current directory");
            Console.WriteLine("\tuserName: User name to access the server");
            Console.WriteLine("\tuserPassword: Password for the user");
            Console.WriteLine("\tfolderID: ID of the destination folder");
            Console.WriteLine("\tsessionName: Name of the session");
        }

        //========================= Needed to use self-signed servers

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
