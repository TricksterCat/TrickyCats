using GameRules.Scripts.Server;

namespace GameRules.Server.Responses
{
    public struct AuthResponse
    {
        public bool IsSuccess => ErrorCode == ErrorCode.None;
        
        public readonly ErrorCode ErrorCode;
        
        public readonly string AccessToken;
        public readonly string RefreshToken;

        public AuthResponse(ErrorCode errorCode, string accessToken, string refreshToken) : this()
        {
            ErrorCode = errorCode;
            AccessToken = accessToken;
            RefreshToken = refreshToken;
        }

        public AuthResponse(ErrorCode errorCode) : this()
        {
            ErrorCode = errorCode;
        }
        
        
        //Возможно стоит добавить купленные скины и отключена ли реклама...
    }
}