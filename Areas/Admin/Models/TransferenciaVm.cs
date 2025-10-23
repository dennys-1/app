namespace TiendaPc.Areas.Admin.Models
{
    public class TransferenciaVm
    {
        public int IdAlmacenOrigen { get; set; }
        public int IdAlmacenDestino { get; set; }
        public List<ItemTransferenciaVm> Items { get; set; } = new();
    }

    public class ItemTransferenciaVm
    {
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
    }
}
