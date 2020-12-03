namespace Nebula.Data
{
    public class reCAPTCHAVerificationOptions
    {
        public string Secret { get; set; }
    }

    public class SwapOptions
    {
        public string ethUrl { get; set; }
        public string ethContract { get; set; }
        public string ethPvk { get; set; }
        public string ethPub { get; set; }
        public string lyrPvk { get; set; }
        public string lyrPub { get; set; }
        public string ethScanApiKey { get; set; }
    }
}
