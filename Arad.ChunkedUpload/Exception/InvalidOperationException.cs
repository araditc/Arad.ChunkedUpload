namespace Arad.ChunkedUpload.Exception
{
    public class InvalidOperationException : ApiException
    {
        public InvalidOperationException(string msg) : base(401, msg)
        {

        }
    }
}