namespace WoWRegeneration.Repositories
{
    public class WoW54818414 : WoWRepository
    {
        public override string GetBaseUrl()
        {
            return "http://dist.blizzard.com.edgesuite.net/wow-pod-retail/EU/15890.direct/";
        }

        public override string GetMFilName()
        {
            return "wow-18414-E68C6C849BBD16D2A8A153AFC865062F.mfil";
        }
    }
}
