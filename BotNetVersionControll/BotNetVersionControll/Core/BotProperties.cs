namespace BotNetVersionControll.Core
{
    internal class BotProperties
    {
        public string LinkVersion { get; set; } = "http://192.168.1.166/VersionFile.txt";
        public int TimeUpdateFiles { get; set; } = 7;
        public string LinkFileVersion { get; set; } = "";
        public string PathFilesBot { get; set; } = "D:\\ProjectsApp\\BotNet\\BotAssistant_Net\\BotAssistant_Net\\bin\\Debug\\net6.0\\BotAssistant_Net.exe";
        public string BotAssistantVersion { get; set; } = "0.0.1";
    }
}
