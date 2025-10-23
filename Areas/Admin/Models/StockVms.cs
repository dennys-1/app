using System.ComponentModel.DataAnnotations;

namespace TiendaPc.Areas.Admin.Models
{
    // Para la grilla de /Admin/Stock/Index
    public class StockRowVm
    {
        public int IdProducto { get; set; }
        public string Producto { get; set; } = "";
        public string Sku { get; set; } = "";
        public int IdAlmacen { get; set; }
        public string Almacen { get; set; } = "";
        public int Cantidad { get; set; }
    }

    // Para la pantalla de Ajustar stock
    public class AjusteVm
    {
        // Contexto del ajuste
        public int IdProducto { get; set; }
        public string Producto { get; set; } = "";
        public string Sku { get; set; } = "";
        public int IdAlmacen { get; set; }
        public string Almacen { get; set; } = "";

        // Lectura
        public int CantidadActual { get; set; }

        // Lo que el usuario escribe
        [Required(ErrorMessage = "Ingresa la variación (puede ser negativa).")]
        public int Delta { get; set; }

        public string? Motivo { get; set; }
    }
}
