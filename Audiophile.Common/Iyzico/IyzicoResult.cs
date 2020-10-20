namespace Audiophile.Common.Iyzico
{
    public class IyzicoResult<T>
    {
        public bool IsSuccess { get; set; }
        public string ErrorCode { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
}