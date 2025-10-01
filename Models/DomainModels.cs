
namespace TiendaPc.Models;

public record Marca(int IdMarca, string Nombre);
public record Categoria(int IdCategoria, string Nombre);

public class Producto {
    public int IdProducto { get; set; }
    public string Sku { get; set; } = default!;
    public string Nombre { get; set; } = default!;
    public int IdMarca { get; set; }
    public int IdCategoria { get; set; }
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public decimal? Costo { get; set; }
    public bool Activo { get; set; } = true;
    public string? Especificaciones { get; set; } // JSON
}

public class ImagenProducto {
    public int IdImagen { get; set; }
    public int IdProducto { get; set; }
    public string Url { get; set; } = default!;
    public bool Principal { get; set; }
}

public class Almacen {
    public int IdAlmacen { get; set; }
    public string Nombre { get; set; } = default!;
    public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
}

public class Stock {
    public int IdProducto { get; set; }
    public int IdAlmacen { get; set; }
    public int Cantidad { get; set; }
}

public class ReglaPrecio {
    public int IdRegla { get; set; }
    public string Nombre { get; set; } = default!;
    public string Tipo { get; set; } = default!;
    public DateTimeOffset Inicio { get; set; }
    public DateTimeOffset Fin { get; set; }
    public bool Activo { get; set; } = true;
}
public class ReglaPrecioDetalle {
    public int IdDetalle { get; set; }
    public int IdRegla { get; set; }
    public int IdProducto { get; set; }
    public decimal? PrecioPromocional { get; set; }
    public int? CantMin { get; set; }
    public int? CantMax { get; set; }
    public decimal? PrecioB2B { get; set; }
}

public class Cliente {
    public int IdCliente { get; set; }
    public string TipoDoc { get; set; } = default!;
    public string NroDoc { get; set; } = default!;
    public string Nombre { get; set; } = default!;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
}

public class Proforma {
    public int IdProforma { get; set; }
    public int? IdCliente { get; set; }
    public DateTimeOffset CreadaEn { get; set; } = DateTimeOffset.UtcNow;
    public decimal Subtotal { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "Borrador";
}
public class ItemProforma {
    public int IdItem { get; set; }
    public int IdProforma { get; set; }
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class Carrito {
    public Guid IdCarrito { get; set; }
    public string? IdUsuario { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;
}
public class ItemCarrito {
    public int IdItem { get; set; }
    public Guid IdCarrito { get; set; }
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class Pedido {
    public int IdPedido { get; set; }
    public string? NumeroPedido { get; set; }
    public string IdUsuario { get; set; } = default!;
    public decimal Subtotal { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;
}
public class ItemPedido {
    public int IdItem { get; set; }
    public int IdPedido { get; set; }
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal TotalLinea { get; set; }
}

public class Proveedor {
    public int IdProveedor { get; set; }
    public string Ruc { get; set; } = default!;
    public string RazonSocial { get; set; } = default!;
    public string? Contacto { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
}
public class ProveedorProducto {
    public int IdProveedor { get; set; }
    public int IdProducto { get; set; }
    public string? CodigoProveedor { get; set; }
    public decimal? CostoUltima { get; set; }
    public int? PlazoDias { get; set; }
}

public class OrdenCompra {
    public int IdOc { get; set; }
    public string? NumeroOc { get; set; }
    public int IdProveedor { get; set; }
    public int IdAlmacen { get; set; }
    public DateTime FechaEmision { get; set; } = DateTime.UtcNow.Date;
    public DateTime? FechaEntrega { get; set; }
    public string Moneda { get; set; } = "PEN";
    public decimal Subtotal { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "Borrador";
}
public class ItemOrdenCompra {
    public int IdItem { get; set; }
    public int IdOc { get; set; }
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal TotalLinea { get; set; }
}

public class RecepcionCompra {
    public int IdRecepcion { get; set; }
    public int IdOc { get; set; }
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
    public string? GuiaRemision { get; set; }
    public string? Observacion { get; set; }
}
public class ItemRecepcionCompra {
    public int IdItem { get; set; }
    public int IdRecepcion { get; set; }
    public int IdProducto { get; set; }
    public int CantidadRecibida { get; set; }
}

public class DocumentoCompra {
    public int IdDoc { get; set; }
    public int IdRecepcion { get; set; }
    public string Tipo { get; set; } = default!;
    public string Serie { get; set; } = default!;
    public string Numero { get; set; } = default!;
    public DateTime FechaEmision { get; set; }
    public string Moneda { get; set; } = "PEN";
    public decimal Subtotal { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
}

public class MovimientoInventario {
    public int IdMov { get; set; }
    public int IdAlmacen { get; set; }
    public int IdProducto { get; set; }
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
    public string Tipo { get; set; } = default!;
    public string? Referencia { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public string? Observacion { get; set; }
}

public class CostoProducto {
    public int IdProducto { get; set; }
    public int IdAlmacen { get; set; }
    public decimal CostoPromedio { get; set; }
}
