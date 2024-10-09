namespace DiaFisTransferEntegrasyonu.Models
{
    public class Customer
    {
        public int Id { get; set; }  // Bizim sistem ID'si
        public int DiaKey { get; set; }  // Dia'dan gelen Key değeri
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? TcNo { get; set; }
        public string? Vkn { get; set; }
    }
}
