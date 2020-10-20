namespace Audiophile.Common
{
    public class UiMessage
    {
        public UiMessage(NotyType type, string message)
        {
            Type = type;
            Message = message;
        }
        public UiMessage(NotyType type, string message, int timeout)
        {
            Type = type;
            Message = message;
            Timeout = timeout;
        }

        public UiMessage(NotyType type, string message, string messageEn, int lang)
        {
            Type = type;
            Message = (lang == 1) ? message : messageEn;
        }
        
        public UiMessage()
        {
            
        }

        public string Title { get; set; }
        public string Message { get; set; }
        public string Class { get; set; }
        public int Timeout { get; set; }
        public NotyType Type { get; set; } /// -1: error /// 0: info /// 1: success
    }

    public enum NotyType
    {
        success,
        error,
        info
    }
}