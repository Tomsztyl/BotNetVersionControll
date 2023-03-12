namespace BotNetVersionControll.Core
{
    internal class BotProperties
    {
        public string LinkVersion { get; set; } = "http://192.168.1.166/VersionsZip/VersionFile.txt";
        public string LinkArchiveMainPath { get; set; } = "http://192.168.1.166/VersionsZip/";
        public int TimeUpdateFiles { get; set; } = 7;
        public string PathFilesBotRun { get; set; } = "D:\\ProjectsApp\\BotNet\\BotAssistant_Net\\BotAssistant_Net\\bin\\Debug\\net6.0\\BotAssistant_Net.exe";
        public string BotAssistantVersion { get; set; } = "0.0.1";
        public string MainDirectoryBotPath { get; set; } = "D:\\ProjectsApp\\BotNet\\BotAssistant_Net\\BotAssistant_Net\\bin\\Debug\\";
        public bool FeatchingCurrentVersion { get; set; } = true;
    }
}
