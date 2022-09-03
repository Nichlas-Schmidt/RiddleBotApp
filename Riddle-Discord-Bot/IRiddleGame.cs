namespace Riddle_Discord_Bot
{
    public interface IRiddleGame
    {
        Riddle? ActiveRiddle { get; }

        void ClearUsedRiddles();
        bool Guess(string input);
        List<Riddlewinner> LeaderboardRead();
        void LeaderboardWrite(string winner);
        bool LoadActiveRiddle();
        bool LoadAllRiddles();
        bool LoadAllUsedRiddles();
        string LoadJsonFile(string file_name);
        bool LoadLeaderboard();
        void MoveActiveToUsed();
        void ReloadRiddles();
        int Remaining();
        bool SetNextActive();
        bool StartGame();
        void Startup();
        void StopGame();
        void SyncActive();
        void SyncUsed();
        void WriteToJson(string file_name, string content);
    }
}