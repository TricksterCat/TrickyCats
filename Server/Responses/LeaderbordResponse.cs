using GameRules.Scripts.Server;
using GameRules.UI.Leaderboards;

namespace GameRules.Server.Responses
{
    public struct LeaderboardResponse
    {
        public bool IsSuccess => ErrorCode == ErrorCode.None;
        
        public readonly ErrorCode ErrorCode;

        public string PlayerRank;
        public string TotalPlayers;
        
        public readonly Leaderboard.UserScore[] Scores;

        public LeaderboardResponse(ErrorCode errorCode, Leaderboard.UserScore[] scores, string playerRank, string totalPlayers)
        {
            ErrorCode = errorCode;
            Scores = scores;

            PlayerRank = playerRank;
            TotalPlayers = totalPlayers;
        }

        public LeaderboardResponse(ErrorCode errorCode) : this()
        {
            ErrorCode = errorCode;
            Scores = new Leaderboard.UserScore[0];
            
            PlayerRank = string.Empty;
            TotalPlayers = string.Empty;
        }
    }
}