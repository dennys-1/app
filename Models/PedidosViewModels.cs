namespace TiendaPc.Models;

public class PedidoListItemVm
{
    public int IdPedido { get; set; }
    public string NumeroPedido { get; set; } = "-";
    public string Estado { get; set; } = "Pendiente";
    public decimal Total { get; set; }
    public DateTimeOffset CreadoEn { get; set; }

    // Extras opcionales
    public string? ReferenciaPago { get; set; }
    public string? Tracking { get; set; }
}

public class PedidoDetalleVm
{
    public int IdPedido { get; set; }
    public string NumeroPedido { get; set; } = "-";
    public string Estado { get; set; } = "Pendiente";
    public decimal Subtotal { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public List<PedidoDetalleItemVm> Items { get; set; } = new();

    // Pago
    public string? ReferenciaPago { get; set; }
    public DateTimeOffset? PagadoEn { get; set; }

    // Envío
    public string? Courier { get; set; }
    public string? Tracking { get; set; }
    public DateTimeOffset? EnviadoEn { get; set; }
}

public class PedidoDetalleItemVm
{
    public string Producto { get; set; } = "";
    public string Sku { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal TotalLinea { get; set; }
}
