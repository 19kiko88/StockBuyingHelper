namespace StockBuingHelper.Web.Dtos.Request
{
    public class ResGetVtiDataDto
    {
        public int vtiIndex { get; set; }
        public bool queryEtfs { get; set; } = false;
    }
}
