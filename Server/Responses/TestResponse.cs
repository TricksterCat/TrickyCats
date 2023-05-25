using GameRules.Scripts.Server;

namespace GameRules.Server.Responses
{
    public struct TestResponse
    {
        public bool IsSuccess => ErrorCode == ErrorCode.None;
        
        public readonly ErrorCode ErrorCode;
        public readonly string Result;

        public TestResponse(ErrorCode errorCode, string result = null)
        {
            ErrorCode = errorCode;
            Result = result;
        }

        public TestResponse(ErrorCode errorCode) : this()
        {
            ErrorCode = errorCode;
        }
    }
}