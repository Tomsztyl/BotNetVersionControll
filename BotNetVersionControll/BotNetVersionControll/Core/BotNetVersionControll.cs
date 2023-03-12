using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace BotNetVersionControll.Core
{
    internal class BotNetVersionControll
    {
        private const string NAME_FILE_DATA = "BotVersionControll.json";

        public static Process BotProccesRun = new Process();
        public static BotProperties BotPropertiesData { get; private set; } = new BotProperties();
        public static bool VersionUpdating { get; private set; } = false;

        #region Behaviour App

        static void Main( string[] args )
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler( TaskTry );
            new BotNetVersionControll().InitializeBot().GetAwaiter().GetResult();
        }

        private static async void TaskTry( object sender, EventArgs e )
        {
            await OnExitApp();
        }

        private static async Task OnExitApp()
        {
            AppDomain.CurrentDomain.ProcessExit -= new EventHandler( TaskTry );
            await TryKillBotAssisant();
            await OnSaveApp();
            Debuger.PrintLog( "EXIT!", ETypeLog.Warning );
            Thread.Sleep( 1000 );
        }

        private static async Task OnSaveApp()
        {
            Debuger.PrintLog( "SAVE APP!", ETypeLog.Warning );
            await SavePropertiesBot();
        }

        #endregion

        private async Task InitializeBot()
        {
            bool isLoaded = TryLoadFileData();
            if( !isLoaded )
            {
                return;
            }
            await TryRunnProcess();
            await UpdateFeatching();
        }

        private static async Task SavePropertiesBot()
        {
            string prefix = string.Empty;
            if( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
            {
                prefix = "/";
            }
            else
            {
                prefix = "\\";
            }

            string path = Directory.GetCurrentDirectory();
            string formatPath = string.Format( "{0}{1}{2}", path, prefix, NAME_FILE_DATA );
            File.Delete( @formatPath );

            FileStream fileCreated = File.Create( @formatPath );
            fileCreated.Close();
            string json = JsonConvert.SerializeObject( BotPropertiesData, Formatting.Indented );
            File.WriteAllText( @formatPath, json );
            string pathCreated = string.Format( "[{0}] UPDATE FILE DATA PATH: {1}", NAME_FILE_DATA, @formatPath );
            Debuger.PrintLog( pathCreated, ETypeLog.Succes );
        }

        private static bool TryLoadFileData()
        {
            string prefix = string.Empty;
            if( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
            {
                prefix = "/";
            }
            else
            {
                prefix = "\\";
            }

            string path = Directory.GetCurrentDirectory();
            string formatPath = string.Format( "{0}{1}{2}", path, prefix, NAME_FILE_DATA );
            if( !File.Exists( formatPath ) )
            {
                FileStream fileCreated = File.Create( @formatPath );
                fileCreated.Close();
                string json = JsonConvert.SerializeObject( BotPropertiesData, Formatting.Indented );
                File.WriteAllText( @formatPath, json );
                Debuger.PrintLog( "File not exist!", ETypeLog.Warning );
                string log = string.Format( "Complete the file [{0}] that was created in the same path and run again!", NAME_FILE_DATA );
                string pathCreated = string.Format( "[{0}] PATH: {1}", NAME_FILE_DATA, @formatPath );
                Debuger.PrintLog( log, ETypeLog.Warning );
                Debuger.PrintLog( pathCreated, ETypeLog.Warning );
                Debuger.PrintLog( "Shut down app", ETypeLog.Warning );
                return false;
            }
            else
            {
                string loadText = File.ReadAllText( @formatPath );
                BotPropertiesData = JsonConvert.DeserializeObject<BotProperties>( loadText );
                string pathCreated = string.Format( "[{0}] PATH: {1}", NAME_FILE_DATA, @formatPath );
                Debuger.PrintLog( pathCreated, ETypeLog.Succes );
                Debuger.PrintLog( "File loaded!", ETypeLog.Succes );
                return true;
            }
        }

        public async Task UpdateFeatching()
        {
            TryUpdateFeatching();
            await Task.Delay( BotPropertiesData.TimeUpdateFiles * 1000 );
            await UpdateFeatching();
        }

        private void TryUpdateFeatching()
        {
            if( VersionUpdating || !BotPropertiesData.FeatchingCurrentVersion )
            {
                return;
            }

            string versionFromServer = GetVerstionFromServer();

            if( versionFromServer != BotPropertiesData.BotAssistantVersion )
            {
                Debuger.PrintLog( string.Format( "Latest version found, starting update: [{0}] to [{1}] "
                    , BotPropertiesData.BotAssistantVersion, versionFromServer ), ETypeLog.Warning );
                VersionUpdating = true;
                TryDonwloadVersion( versionFromServer );
            }
        }

        private void TryDonwloadVersion( string version )
        {
            string urlDonwload = string.Format( "{0}{1}.zip", BotPropertiesData.LinkArchiveMainPath, version );
            string localPath = string.Format( "{0}bot_version_{1}.zip", BotPropertiesData.MainDirectoryBotPath, version );
            Uri siteUri = new Uri( urlDonwload );
            using( var client = new WebClient() )
            {
                client.DownloadProgressChanged += HandleDownloadProgressChanged;
                client.DownloadFileCompleted += HandleDonwloadFileCompleted;
                client.DownloadFileAsync( siteUri, localPath );
            }
        }

        private void HandleDonwloadFileCompleted( object sender, AsyncCompletedEventArgs e )
        {
            VersionUpdating = false;

            if( e.Cancelled )
            {
                Debuger.PrintLog( "File download cancelled.", ETypeLog.Error );
            }

            if( e.Error != null )
            {
                Debuger.PrintLog( e.Error.ToString(), ETypeLog.Error );
            }

            if( !e.Cancelled && e.Error == null )
            {
                Debuger.PrintLog( "File downloaded!", ETypeLog.Succes );
                BotPropertiesData.BotAssistantVersion = GetVerstionFromServer();
            }
        }

        private void HandleDownloadProgressChanged( object sender, DownloadProgressChangedEventArgs e )
        {
            string formatProgress = string.Format( "{0}    downloaded {1} of {2} bytes. {3} % complete...",
                (string)e.UserState,
                e.BytesReceived,
                e.TotalBytesToReceive,
                e.ProgressPercentage );
            Debuger.PrintLog( formatProgress );
        }

        private static async Task TryKillBotAssisant()
        {
            ProcessStartInfo processStartInfo = BotProccesRun.StartInfo;
            if( !string.IsNullOrEmpty( processStartInfo.FileName ) )
            {
                if( !BotProccesRun.HasExited )
                {
                    Debuger.PrintLog( "Kill " + BotProccesRun, ETypeLog.Warning );
                    BotProccesRun.Kill();
                }
            }
        }

        private string GetVerstionFromServer()
        {
            var webRequest = WebRequest.Create( @BotPropertiesData.LinkVersion );

            using( var response = webRequest.GetResponse() )
            using( var content = response.GetResponseStream() )
            using( var reader = new StreamReader( content ) )
            {
                var strContent = reader.ReadToEnd();
                string afterTrim = strContent.Trim( '\n' );
                return afterTrim;
            }
        }

        private async Task TryRunnProcess()
        {
            BotProccesRun.StartInfo.FileName = BotPropertiesData.PathFilesBotRun;
            BotProccesRun.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            BotProccesRun.Start();
        }
    }
}