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

        #region Behaviour App

        static void Main( string[] args )
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler( OnExitApp );
            new BotNetVersionControll().InitializeBot().GetAwaiter().GetResult();
        }

        private static void OnExitApp( object sender, EventArgs e )
        {
            TryKillBotAssisant();
            OnSaveApp();
            Debuger.PrintLog( "EXIT!", ETypeLog.Warning );
            Thread.Sleep( 1000 );
        }

        private static void OnSaveApp()
        {
            Debuger.PrintLog( "SAVE APP!", ETypeLog.Warning );
        }

        #endregion

        private async Task InitializeBot()
        {
            bool isLoaded = TryLoadFileData();
            if( !isLoaded )
            {
                return;
            }
            TryRunnProcess();
            await UpdateFeatching();
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
            await Task.Delay( BotPropertiesData.TimeUpdateFiles * 1000 );
            TryUpdateFeatching();
            await UpdateFeatching();
        }

        private void TryUpdateFeatching()
        {
            string versionFromServer = GetVerstionFromServer();
            if( versionFromServer != BotPropertiesData.BotAssistantVersion )
            {

            }
        }

        private static bool TryKillBotAssisant()
        {
            if( BotProccesRun != null )
            {
                Debuger.PrintLog( "Kill " + BotProccesRun, ETypeLog.Warning );
                BotProccesRun.Kill();
                return true;
            }
            return false;
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

        private void TryRunnProcess()
        {
            BotProccesRun.StartInfo.FileName = BotPropertiesData.PathFilesBot;
            BotProccesRun.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            BotProccesRun.Start();
        }
    }
}