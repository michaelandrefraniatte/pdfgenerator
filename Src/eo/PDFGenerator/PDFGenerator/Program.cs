using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
namespace PDFGenerator
{
    public static class Program
    {
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint TimeBeginPeriod(uint ms);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint TimeEndPeriod(uint ms);
        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        public static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, ref uint CurrentResolution);
        public static uint CurrentResolution = 0;
        public static string username, hash, uniqueid, userchecksumuniqueid;
        private static string actualuniqueid, bdduniqueid, userchecksum;
        const string alphanumeric = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890";
        private static bool authconnecting = false;
        private static FirestoreDb db;
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        public static async Task Main()
        {
            bool runelevated = false;
            bool oneinstanceonly = true;
            string filename = "la.txt";
            try
            {
                TimeBeginPeriod(1);
                NtSetTimerResolution(1, true, ref CurrentResolution);
                SetProcessPriority();
                if (oneinstanceonly)
                {
                    if (AlreadyRunning())
                    {
                        return;
                    }
                }
                if (runelevated)
                {
                    if (!hasAdminRights())
                    {
                        RunElevated();
                        return;
                    }
                }
                if (filename == "la.txt" & File.Exists("la.txt"))
                {
                    IsLocal();
                    if (!authconnecting)
                    {
                        return;
                    }
                }
                else
                {
                    if (filename == "auth.txt" & File.Exists("auth.txt"))
                    {
                        await authConnection();
                        if (!authconnecting)
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }
            catch
            {
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
        public static bool hasAdminRights()
        {
            WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        public static void RunElevated()
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.Verb = "runas";
                processInfo.FileName = Application.ExecutablePath;
                Process.Start(processInfo);
            }
            catch { }
        }
        public static void IsLocal()
        {
            try
            {
                using (StreamReader file = new StreamReader("la.txt"))
                {
                    username = file.ReadLine();
                    hash = file.ReadLine();
                    file.Close();
                }
                String thisprocessname = Process.GetCurrentProcess().ProcessName;
                SHA1 sha1 = SHA1.Create();
                FileStream fs = new FileStream(thisprocessname + ".exe", FileMode.Open, FileAccess.Read);
                string checksum = BitConverter.ToString(sha1.ComputeHash(fs)).Replace("-", "");
                fs.Close();
                uniqueid = getUniqueId();
                userchecksumuniqueid = username + checksum + uniqueid;
                string salt = GetSalt(10);
                string hashedPass = HashPassword(salt, userchecksumuniqueid);
                if (hash != hashedPass)
                    authconnecting = false;
                else
                    authconnecting = true;
            }
            catch
            {
                authconnecting = false;
            }
        }
        private async static Task authConnection()
        {
            try
            {
                using (StreamReader file = new StreamReader("auth.txt"))
                {
                    username = file.ReadLine();
                    hash = file.ReadLine();
                    file.Close();
                }
                String thisprocessname = Process.GetCurrentProcess().ProcessName;
                SHA1 sha1 = SHA1.Create();
                FileStream fs = new FileStream(thisprocessname + ".exe", FileMode.Open, FileAccess.Read);
                string checksum = BitConverter.ToString(sha1.ComputeHash(fs)).Replace("-", "");
                fs.Close();
                userchecksum = username + checksum;
                string salt = GetSalt(10);
                string hashedPass = HashPassword(salt, userchecksum);
                if (hash != hashedPass)
                {
                    authconnecting = false;
                }
                actualuniqueid = getUniqueId();
                if (actualuniqueid == null)
                {
                    authconnecting = false;
                }
                else
                {
                    var jsonString = @"{
                      ""type"": ""service_account"",
                      ""project_id"": ""secureconnection"",
                      ""private_key_id"": ""2f59900649889bb033e0eb9e8f3859b55a89d66e"",
                      ""private_key"": ""-----BEGIN PRIVATE KEY-----\nMIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQDnofkZR7QXSv0Y\ncSKeEFlNlgzrjOFwpJJhdG1DYNOz72WeVFov8yE/R59OWLPcCq2u3Alz4LezXMvO\n3cnIqILIlhdrGqwLo+SWjAVVqad5OISVJMFqVyn58OO06YtGEiVgGShq5DQts8L7\nfV5gaxkkJhsdS5ccxgU5aZxH1RMMfkdHejn9CggJ9B3E1ahMqGNw19YBmsuJWWmD\nkwRVJmDJ0lOKs4bRN6NSkkdHEVseelITfPNVP4MXE68OSvRMYtUTHxxf3mzHVjso\nBk1YBJyUyc7lRCcXmcbsznd9Cl3Q9lHY99zW1njiiU8Sv41UafVEXMBODaP9DOu8\ncSpVxw8hAgMBAAECggEAJI2bgpadqrqt6SWwFFM5JzTBh4nSaRUXd+NIe4RetDut\nr3LZTqLRbCvbIyEoClacZQaamUZCRxRNogYUWfg5t090P6B/EPvapA/8UYc12Lvc\ntFqPXpQwbsh0brAXnJsFeej5HLE0M211nNFXy43AcxjDrkz8+lCc3Ma39QqreGIc\n9dzpQ+inEzRYwf08FEY7o3wSp0QZSfUchHA6LESwttRkRO3gdqS5za1TmX0WkfHY\nnfi5jhvK7bYmyuVsCGCX5jy4mpf8AlDMT6fIdBkzn7WyTvFCr6pbS0R7e7hur2ai\nxEQeAqQu6AtpiYrexV895lPkx74pQNZp2QbQDO4ALwKBgQDziBiPT5fpemzcX474\n3NOCb0qybhXnvhMOdK131MS9PzzbFhu4IGMP4t7dxgnoxtmN9fSQehpkAKdzkKt8\nDwuKm4XL+wI+7GdIfpUjd4Lnfpv+RMJvvNffR5469zDT7EdXKoadvZw7YDQCwVym\nGKDV2YY3SOnQgWNjHpH5nDMBBwKBgQDzfevRhxnqnabh7E/2PRFXInBRoytNmGn3\nuicZsWInqx1vviBjl5Trshfvg2WIDuKQ/QT5y8CcXu9P3Ys7UbFvQR9q43DW1BgN\nIdz8L5Fx3irT39s0GWmZc5SkEWngPGq0yh5/nXQTFcRJ59Zq+9tCjXO23hj9Dklt\n9plnzonslwKBgQDhvVzustvg+6+fEyEHREL3HEyEWxEJEKK/ep41fs+jkNPLTZIC\nOls5JZZqwqD62iBdvAioR9bgrc6KjCa5R4TuRb1fWFw7kY0noNaD2stH5I+awYfu\nZYFBIjTk+a+UMefrP6sq2tDQJRvxFeXYvOmRcSI9auP5d4Z2Iac0VnrczwKBgC/4\n7y0o4QJIbUi1tktdXL0+G8L50t5G2Rnloy58tEn8fKA3ZUo54y1MuUqHKMnVpO3L\n698LNbeZPK0PiQ722W6B9h6pEOJChzqPIWrONGmqy+VShW2OVC/XhcGNbL6xKJTV\n/YxHCUd5UmL9OlF5rYk/NT0iJOo2lmED5NV+682hAoGBANwoQpKbcNFSIxrUXKfN\n1f69DBXYLnPzPzQN1GRm/izpOAfBhMuQkseuABc5oReMSfN1qSEgtxiAIdpUt+we\niuT8xlTwz4Jh8E/ZgO8Dt2DPJoNuZDt4AakG9UIn6uNWDWPvUAp7GpMbjfchLzYD\nmpXuM4Pex1k6+5E1FKeaqeSR\n-----END PRIVATE KEY-----\n"",
                      ""client_email"": ""secureconnection@secureconnection.iam.gserviceaccount.com"",
                      ""client_id"": ""102930494484487968938"",
                      ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
                      ""token_uri"": ""https://oauth2.googleapis.com/token"",
                      ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
                      ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/secureconnection%40secureconnection.iam.gserviceaccount.com""
                    }";
                    var builder = new FirestoreClientBuilder { JsonCredentials = jsonString };
                    db = FirestoreDb.Create("secureconnection", builder.Build());
                    DocumentReference docRef = db.Collection("users").Document(username);
                    DocumentSnapshot document = await docRef.GetSnapshotAsync();
                    Dictionary<string, object> documentDictionary = document.ToDictionary();
                    bdduniqueid = documentDictionary["uniqueid"].ToString();
                    if (bdduniqueid == "")
                    {
                        Dictionary<string, object> user = new Dictionary<string, object>
                        {
                            { "uniqueid", actualuniqueid }
                        };
                        await docRef.SetAsync(user);
                        document = await docRef.GetSnapshotAsync();
                        documentDictionary = document.ToDictionary();
                        bdduniqueid = documentDictionary["uniqueid"].ToString();
                    }
                    if (actualuniqueid == bdduniqueid)
                    {
                        authconnecting = true;
                    }
                    else
                    {
                        authconnecting = false;
                    }
                }
            }
            catch
            {
                authconnecting = false;
            }
        }
        public static string GetSalt(int saltSize)
        {
            float key = 0.6f;
            StringBuilder strB = new StringBuilder("");
            while ((saltSize--) > 0)
                strB.Append(alphanumeric[(int)(key * alphanumeric.Length)]);
            return strB.ToString();
        }
        public static string HashPassword(string salt, string password)
        {
            string mergedPass = string.Concat(salt, password);
            return EncryptUsingMD5(mergedPass);
        }
        public static string EncryptUsingMD5(string inputStr)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(inputStr));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                    sBuilder.Append(data[i].ToString("x2"));
                return sBuilder.ToString();
            }
        }
        public static string getUniqueId()
        {
            try
            {
                string cpuInfo = string.Empty;
                ManagementClass mc = new ManagementClass("win32_processor");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    cpuInfo = mo.Properties["processorID"].Value.ToString();
                    break;
                }
                string drive = "C";
                ManagementObject dsk = new ManagementObject(@"win32_logicaldisk.deviceid=""" + drive + @":""");
                dsk.Get();
                string volumeSerial = dsk["VolumeSerialNumber"].ToString();
                string uuidInfo = string.Empty;
                ManagementClass mcu = new ManagementClass("Win32_ComputerSystemProduct");
                ManagementObjectCollection mocu = mcu.GetInstances();
                foreach (ManagementObject mou in mocu)
                {
                    uuidInfo = mou.Properties["UUID"].Value.ToString();
                    break;
                }
                if (volumeSerial != null & volumeSerial != "" & cpuInfo != null & cpuInfo != "" & uuidInfo != null & uuidInfo != "")
                    return volumeSerial + "-" + cpuInfo + "-" + uuidInfo;
                else
                    return null;
            }
            catch
            {
                return null;
            }
        }
        private static void SetProcessPriority()
        {
            using (Process p = Process.GetCurrentProcess())
            {
                p.PriorityClass = ProcessPriorityClass.RealTime;
            }
        }
        private static bool AlreadyRunning()
        {
            String thisprocessname = Process.GetCurrentProcess().ProcessName;
            Process[] processes = Process.GetProcessesByName(thisprocessname);
            if (processes.Length > 1)
                return true;
            else
                return false;
        }
    }
}