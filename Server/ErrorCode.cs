namespace GameRules.Scripts.Server
{
    public enum ErrorCode
    {
        NotInitialize = 0,
        None = 200,
        UnknownError = 520,
        NotInternet = 4,
        BadJson = 400,
        WaitUpdateAccess,
        TokenExpired,
        InvalidToken
    }

    public enum ItemErrorCode
    {
        UnknownError,
        None,
        NotInternet,
        not_enough_balance,
        session_not_initialized,
        exchange_not_available
    }
}