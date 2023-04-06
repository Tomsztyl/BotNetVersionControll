using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using BotNetVersionControll.Core;
using Mono.Unix;
using Newtonsoft.Json;

namespace BotNetVersionControl.Core
{
    internal class BotNetVersionControll
    {
        private const string NAME_FILE_DATA = "BotVersionControll.json";

        public static Process BotProcessRun = new Process();
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
            await UpdateFetching();
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

        public async Task UpdateFetching()
        {
            await TryUpdateFetching();
            await Task.Delay( BotPropertiesData.TimeUpdateFiles * 1000 );
            await UpdateFetching();
        }

        private async Task TryUpdateFetching()
        {
            if( VersionUpdating || !BotPropertiesData.FetchingCurrentVersion )
            {
                return;
            }

            string versionFromServer = GetVersionFromServer();

            if( versionFromServer != BotPropertiesData.BotAssistantVersion )
            {
                await TryKillBotAssisant();
                Debuger.PrintLog( string.Format( "Latest version found, starting update: [{0}] to [{1}] "
                    , BotPropertiesData.BotAssistantVersion, versionFromServer ), ETypeLog.Warning );
                VersionUpdating = true;
                TryDownloadsVersion( versionFromServer );
            }
        }

        private string m_LocalPathDownload = string.Empty;

        private void TryDownloadsVersion( string version )
        {
            if( !Directory.Exists( BotPropertiesData.MainDirectoryBeforeBotPath ) )
            {
                Directory.CreateDirectory( BotPropertiesData.MainDirectoryBeforeBotPath );
            }
            string urlDownloads = string.Format( "{0}{1}.zip", BotPropertiesData.LinkArchiveMainPath, version );
            string localPath = string.Format( "{0}bot_version_{1}.zip", BotPropertiesData.MainDirectoryBeforeBotPath, version );
            m_LocalPathDownload = localPath;
            Uri siteUri = new Uri( urlDownloads );
            using( var client = new WebClient() )
            {
                client.DownloadProgressChanged += HandleDownloadProgressChanged;
                client.DownloadFileCompleted += HandleDownloadsFileCompleted;
                client.DownloadFileAsync( siteUri, localPath );
            }
        }

        private void ExtractZipFile()
        {
            if( Directory.Exists( BotPropertiesData.MainDirectoryBotPath ) )
            {
                System.IO.DirectoryInfo di = new DirectoryInfo( BotPropertiesData.MainDirectoryBotPath );

                foreach( FileInfo file in di.GetFiles() )
                {
                    file.Delete();
                }
                foreach( DirectoryInfo dir in di.GetDirectories() )
                {
                    dir.Delete( true );
                }
                Directory.Delete( BotPropertiesData.MainDirectoryBotPath );
            }

            ZipFile.ExtractToDirectory( m_LocalPathDownload, BotPropertiesData.MainDirectoryBeforeBotPath );
            if( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
            {
                SetPermission( BotPropertiesData.PathFilesBotRun );
            }
      
            TryRunnProcess();
        }

        public static void SetPermission( string path )
        {
            var unixFileInfo = new Mono.Unix.UnixFileInfo( path );
            // set file permission to 644
            unixFileInfo.FileAccessPermissions =
                FileAccessPermissions.AllPermissions;
        }

        private void HandleDownloadsFileCompleted( object sender, AsyncCompletedEventArgs e )
        {
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
                BotPropertiesData.BotAssistantVersion = GetVersionFromServer();
                ExtractZipFile();
            }
            VersionUpdating = false;
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
            ProcessStartInfo processStartInfo = BotProcessRun.StartInfo;
            if( !string.IsNullOrEmpty( processStartInfo.FileName ) )
            {
                if( !BotProcessRun.HasExited )
                {
                    Debuger.PrintLog( "Kill " + BotProcessRun, ETypeLog.Warning );
                    BotProcessRun.Kill();
                }
            }
        }

        private string GetVersionFromServer()
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
            BotProcessRun.StartInfo.FileName = BotPropertiesData.PathFilesBotRun;
            BotProcessRun.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            BotProcessRun.Start();
        }
    }
}