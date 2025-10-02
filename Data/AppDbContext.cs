using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata; // para PropertySaveBehavior

using Microsoft.EntityFrameworkCore;
using TiendaPc.Models;

namespace TiendaPc.Data;

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    // DbSets de negocio
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<ImagenProducto> ImagenesProducto => Set<ImagenProducto>();
    public DbSet<Almacen> Almacenes => Set<Almacen>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<ReglaPrecio> ReglasPrecio => Set<ReglaPrecio>();
    public DbSet<ReglaPrecioDetalle> ReglasPrecioDetalle => Set<ReglaPrecioDetalle>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Proforma> Proformas => Set<Proforma>();
    public DbSet<ItemProforma> ItemsProforma => Set<ItemProforma>();
    public DbSet<Carrito> Carritos => Set<Carrito>();
    public DbSet<ItemCarrito> ItemsCarrito => Set<ItemCarrito>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItemsPedido => Set<ItemPedido>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<ProveedorProducto> ProveedoresProducto => Set<ProveedorProducto>();
    public DbSet<OrdenCompra> OrdenesCompra => Set<OrdenCompra>();
    public DbSet<ItemOrdenCompra> ItemsOrdenCompra => Set<ItemOrdenCompra>();
    public DbSet<RecepcionCompra> RecepcionesCompra => Set<RecepcionCompra>();
    public DbSet<ItemRecepcionCompra> ItemsRecepcionCompra => Set<ItemRecepcionCompra>();
    public DbSet<DocumentoCompra> DocumentosCompra => Set<DocumentoCompra>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<CostoProducto> CostosProducto => Set<CostoProducto>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // *** MUY IMPORTANTE para que Identity funcione ***
        base.OnModelCreating(mb);

        // Mapear tablas con snake_case
        mb.Entity<Marca>().ToTable("marca").HasKey(x => x.IdMarca);
        mb.Entity<Marca>().Property(x => x.IdMarca).HasColumnName("id_marca");
        mb.Entity<Marca>().Property(x => x.Nombre).HasColumnName("nombre");

        mb.Entity<Categoria>().ToTable("categoria").HasKey(x => x.IdCategoria);
        mb.Entity<Categoria>().Property(x => x.IdCategoria).HasColumnName("id_categoria");
        mb.Entity<Categoria>().Property(x => x.Nombre).HasColumnName("nombre");

        mb.Entity<Producto>().ToTable("producto").HasKey(x => x.IdProducto);
        mb.Entity<Producto>().Property(x => x.IdProducto).HasColumnName("id_producto");
        mb.Entity<Producto>().Property(x => x.Sku).HasColumnName("sku");
        mb.Entity<Producto>().Property(x => x.Nombre).HasColumnName("nombre");
        mb.Entity<Producto>().Property(x => x.IdMarca).HasColumnName("id_marca");
        mb.Entity<Producto>().Property(x => x.IdCategoria).HasColumnName("id_categoria");
        mb.Entity<Producto>().Property(x => x.Descripcion).HasColumnName("descripcion");
        mb.Entity<Producto>().Property(x => x.Precio).HasColumnName("precio");
        mb.Entity<Producto>().Property(x => x.Costo).HasColumnName("costo");
        mb.Entity<Producto>().Property(x => x.Activo).HasColumnName("activo");
        mb.Entity<Producto>().Property(x => x.Especificaciones).HasColumnName("especificaciones");

        mb.Entity<ImagenProducto>().ToTable("imagen_producto").HasKey(x => x.IdImagen);
        mb.Entity<ImagenProducto>().Property(x => x.IdImagen).HasColumnName("id_imagen");
        mb.Entity<ImagenProducto>().Property(x => x.IdProducto).HasColumnName("id_producto");
        mb.Entity<ImagenProducto>().Property(x => x.Url).HasColumnName("url");
        mb.Entity<ImagenProducto>().Property(x => x.Principal).HasColumnName("principal");

        mb.Entity<Almacen>().ToTable("almacen").HasKey(x => x.IdAlmacen);
        mb.Entity<Almacen>().Property(x => x.IdAlmacen).HasColumnName("id_almacen");
        mb.Entity<Almacen>().Property(x => x.Nombre).HasColumnName("nombre");
        mb.Entity<Almacen>().Property(x => x.Direccion).HasColumnName("direccion");
        mb.Entity<Almacen>().Property(x => x.Activo).HasColumnName("activo");

        mb.Entity<Stock>().ToTable("stock").HasKey(x => new { x.IdProducto, x.IdAlmacen });
        mb.Entity<Stock>().Property(x => x.IdProducto).HasColumnName("id_producto");
        mb.Entity<Stock>().Property(x => x.IdAlmacen).HasColumnName("id_almacen");
        mb.Entity<Stock>().Property(x => x.Cantidad).HasColumnName("cantidad");

        mb.Entity<ReglaPrecio>().ToTable("regla_precio").HasKey(x => x.IdRegla);
        mb.Entity<ReglaPrecio>().Property(x => x.IdRegla).HasColumnName("id_regla");
        mb.Entity<ReglaPrecio>().Property(x => x.Nombre).HasColumnName("nombre");
        mb.Entity<ReglaPrecio>().Property(x => x.Tipo).HasColumnName("tipo");
        mb.Entity<ReglaPrecio>().Property(x => x.Inicio).HasColumnName("inicio");
        mb.Entity<ReglaPrecio>().Property(x => x.Fin).HasColumnName("fin");
        mb.Entity<ReglaPrecio>().Property(x => x.Activo).HasColumnName("activo");

        mb.Entity<ReglaPrecioDetalle>().ToTable("regla_precio_detalle").HasKey(x => x.IdDetalle);
        mb.Entity<ReglaPrecioDetalle>().Property(x => x.IdDetalle).HasColumnName("id_detalle");
        mb.Entity<ReglaPrecioDetalle>().Property(x => x.IdRegla).HasColumnName("id_regla");
        mb.Entity<ReglaPrecioDetalle>().Property(x => x.IdProducto).HasColumnName("id_producto");
        mb.Entity<ReglaPrecioDetalle>().Property(x => x.PrecioPromocional).HasColumnName("precio_promocional");
        mb.Entity<ReglaPrecioDetalle>().Property(x => x.CantMin).HasColumnName("cant_min");
        mb.Entity<ReglaPrecioDetalle>().Property(x => x.CantMax).HasColumnName("cant_max");
        mb.Entity<ReglaPrecioDetalle>().Property(x => x.PrecioB2B).HasColumnName("precio_b2b");

        mb.Entity<Cliente>().ToTable("cliente").HasKey(x => x.IdCliente);
        mb.Entity<Cliente>().Property(x => x.IdCliente).HasColumnName("id_cliente");
        mb.Entity<Cliente>().Property(x => x.TipoDoc).HasColumnName("tipo_doc");
        mb.Entity<Cliente>().Property(x => x.NroDoc).HasColumnName("nro_doc");
        mb.Entity<Cliente>().Property(x => x.Nombre).HasColumnName("nombre");
        mb.Entity<Cliente>().Property(x => x.Telefono).HasColumnName("telefono");
        mb.Entity<Cliente>().Property(x => x.Email).HasColumnName("email");
        mb.Entity<Cliente>().Property(x => x.Direccion).HasColumnName("direccion");

        mb.Entity<Proforma>().ToTable("proforma").HasKey(x => x.IdProforma);
        mb.Entity<Proforma>().Property(x => x.IdProforma).HasColumnName("id_proforma");
        mb.Entity<Proforma>().Property(x => x.IdCliente).HasColumnName("id_cliente");
        mb.Entity<Proforma>().Property(x => x.CreadaEn).HasColumnName("creada_en");
        mb.Entity<Proforma>().Property(x => x.Subtotal).HasColumnName("subtotal");
        mb.Entity<Proforma>().Property(x => x.Impuesto).HasColumnName("impuesto");
        mb.Entity<Proforma>().Property(x => x.Total).HasColumnName("total");
        mb.Entity<Proforma>().Property(x => x.Estado).HasColumnName("estado");

        mb.Entity<ItemProforma>().ToTable("item_proforma").HasKey(x => x.IdItem);
        mb.Entity<ItemProforma>().Property(x => x.IdItem).HasColumnName("id_item");
        mb.Entity<ItemProforma>().Property(x => x.IdProforma).HasColumnName("id_proforma");
        mb.Entity<ItemProforma>().Property(x => x.IdProducto).HasColumnName("id_producto");
        mb.Entity<ItemProforma>().Property(x => x.Cantidad).HasColumnName("cantidad");
        mb.Entity<ItemProforma>().Property(x => x.PrecioUnitario).HasColumnName("precio_unitario");

        mb.Entity<Carrito>().ToTable("carrito").HasKey(x => x.IdCarrito);
        mb.Entity<Carrito>().Property(x => x.IdCarrito).HasColumnName("id_carrito");
        mb.Entity<Carrito>().Property(x => x.IdUsuario).HasColumnName("id_usuario");
        mb.Entity<Carrito>().Property(x => x.CreadoEn).HasColumnName("creado_en");

        mb.Entity<ItemCarrito>().ToTable("item_carrito").HasKey(x => x.IdItem);
        mb.Entity<ItemCarrito>().Property(x => x.IdItem).HasColumnName("id_item");
        mb.Entity<ItemCarrito>().Property(x => x.IdCarrito).HasColumnName("id_carrito");
        mb.Entity<ItemCarrito>().Property(x => x.IdProducto).HasColumnName("id_producto");
        mb.Entity<ItemCarrito>().Property(x => x.Cantidad).HasColumnName("cantidad");
        mb.Entity<ItemCarrito>().Property(x => x.PrecioUnitario).HasColumnName("precio_unitario");

      // ========== P E D I D O ==========
mb.Entity<Pedido>().ToTable("pedido").HasKey(x => x.IdPedido);
mb.Entity<Pedido>().Property(x => x.IdPedido).HasColumnName("id_pedido");

// ⚠️ Clave: que EF NO envíe numero_pedido en el INSERT (lo genera la BD)
var numeroPedido = mb.Entity<Pedido>().Property(x => x.NumeroPedido)
    .HasColumnName("numero_pedido")
    .ValueGeneratedOnAdd();   // EF espera que lo genere la BD

// Evitar que EF lo intente guardar antes o después (por si trae valor por defecto)
numeroPedido.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
numeroPedido.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

mb.Entity<Pedido>().Property(x => x.IdUsuario).HasColumnName("id_usuario");
mb.Entity<Pedido>().Property(x => x.Subtotal).HasColumnName("subtotal");
mb.Entity<Pedido>().Property(x => x.Impuesto).HasColumnName("impuesto");
mb.Entity<Pedido>().Property(x => x.Total).HasColumnName("total");
mb.Entity<Pedido>().Property(x => x.Estado).HasColumnName("estado");
mb.Entity<Pedido>().Property(x => x.CreadoEn).HasColumnName("creado_en");


        mb.Entity<ItemPedido>().ToTable("item_pedido").HasKey(x => x.IdItem);
        mb.Entity<ItemPedido>().Property(x => x.IdItem).HasColumnName("id_item");
        mb.Entity<ItemPedido>().Property(x => x.IdPedido).HasColumnName("id_pedido");
        mb.Entity<ItemPedido>().Property(x => x.IdProducto).HasColumnName("id_producto");
        mb.Entity<ItemPedido>().Property(x => x.Cantidad).HasColumnName("cantidad");
        mb.Entity<ItemPedido>().Property(x => x.PrecioUnitario).HasColumnName("precio_unitario");
        mb.Entity<ItemPedido>().Property(x => x.TotalLinea).HasColumnName("total_linea");

        mb.Entity<Proveedor>().ToTable("proveedor").HasKey(x => x.IdProveedor);
        mb.Entity<Proveedor>().Property(x => x.IdProveedor).HasColumnName("id_proveedor");
        mb.Entity<Proveedor>().Property(x => x.Ruc).HasColumnName("ruc");
        mb.Entity<Proveedor>().Property(x => x.RazonSocial).HasColumnName("razon_social");
        mb.Entity<Proveedor>().Property(x => x.Contacto).HasColumnName("contacto");
        mb.Entity<Proveedor>().Property(x => x.Telefono).HasColumnName("telefono");
        mb.Entity<Proveedor>().Property(x => x.Email).HasColumnName("email");
        mb.Entity<Proveedor>().Property(x => x.Direccion).HasColumnName("direccion");
        mb.Entity<Proveedor>().Property(x => x.Activo).HasColumnName("activo");

        mb.Entity<ProveedorProducto>().ToTable("proveedor_producto").HasKey(x => new { x.IdProveedor, x.IdProducto });
        mb.Entity<ProveedorProducto>().Property(x => x.IdProveedor).HasColumnName("id_proveedor");
        mb.Entity<ProveedorProducto>().Property(x => x.IdProducto).HasColumnName("id_producto");
        mb.Entity<ProveedorProducto>().Property(x => x.CodigoProveedor).HasColumnName("codigo_proveedor");
        mb.Entity<ProveedorProducto>().Property(x => x.CostoUltima).HasColumnName("costo_ultima");
        mb.Entity<ProveedorProducto>().Property(x => x.PlazoDias).HasColumnName("plazo_dias");

        mb.Entity<OrdenCompra>().ToTable("orden_compra").HasKey(x => x.IdOc);
        mb.Entity<OrdenCompra>().Property(x => x.IdOc).HasColumnName("id_oc");
        mb.Entity<OrdenCompra>().Property(x => x.NumeroOc).HasColumnName("numero_oc");
        mb.Entity<OrdenCompra>().Property(x => x.IdProveedor).HasColumnName("id_proveedor");
        mb.Entity<OrdenCompra>().Property(x => x.IdAlmacen).HasColumnName("id_almacen");
        mb.Entity<OrdenCompra>().Property(x => x.FechaEmision).HasColumnName("fecha_emision");
        mb.Entity<OrdenCompra>().Property(x => x.FechaEntrega).HasColumnName("fecha_entrega");
        mb.Entity<OrdenCompra>().Property(x => x.Moneda).HasColumnName("moneda");
        mb.Entity<OrdenCompra>().Property(x => x.Subtotal).HasColumnName("subtotal");
        mb.Entity<OrdenCompra>().Property(x => x.Impuesto).HasColumnName("impuesto");
        mb.Entity<OrdenCompra>().Property(x => x.Total).HasColumnName("total");
        mb.Entity<OrdenCompra>().Property(x => x.Estado).HasColumnName("estado");

        mb.Entity<ItemOrdenCompra>().ToTable("item_orden_compra").HasKey(x => x.IdItem);
        mb.Entity<ItemOrdenCompra>().Property(x => x.IdItem).HasColumnName("id_item");
        mb.Entity<ItemOrdenCompra>().Property(x => x.IdOc).HasColumnName("id_oc");
        mb.Entity<ItemOrdenCompra>().Property(x => x.IdProducto).HasColumnName("id_producto");
        mb.Entity<ItemOrdenCompra>().Property(x => x.Cantidad).HasColumnName("cantidad");
        mb.Entity<ItemOrdenCompra>().Property(x => x.PrecioUnitario).HasColumnName("precio_unitario");
        mb.Entity<ItemOrdenCompra>().Property(x => x.TotalLinea).HasColumnName("total_linea");

        mb.Entity<RecepcionCompra>().ToTable("recepcion_compra").HasKey(x => x.IdRecepcion);
        mb.Entity<RecepcionCompra>().Property(x => x.IdRecepcion).HasColumnName("id_recepcion");
        mb.Entity<RecepcionCompra>().Property(x => x.IdOc).HasColumnName("id_oc");
        mb.Entity<RecepcionCompra>().Property(x => x.Fecha).HasColumnName("fecha");
        mb.Entity<RecepcionCompra>().Property(x => x.GuiaRemision).HasColumnName("guia_remision");
        mb.Entity<RecepcionCompra>().Property(x => x.Observacion).HasColumnName("observacion");

        mb.Entity<ItemRecepcionCompra>().ToTable("item_recepcion_compra").HasKey(x => x.IdItem);
        mb.Entity<ItemRecepcionCompra>().Property(x => x.IdItem).HasColumnName("id_item");
        mb.Entity<ItemRecepcionCompra>().Property(x => x.IdRecepcion).HasColumnName("id_recepcion");
        mb.Entity<ItemRecepcionCompra>().Property(x => x.IdProducto).HasColumnName("id_producto");
        mb.Entity<ItemRecepcionCompra>().Property(x => x.CantidadRecibida).HasColumnName("cantidad_recibida");

        mb.Entity<DocumentoCompra>().ToTable("documento_compra").HasKey(x => x.IdDoc);
        mb.Entity<DocumentoCompra>().Property(x => x.IdDoc).HasColumnName("id_doc");
        mb.Entity<DocumentoCompra>().Property(x => x.IdRecepcion).HasColumnName("id_recepcion");
        mb.Entity<DocumentoCompra>().Property(x => x.Tipo).HasColumnName("tipo");
        mb.Entity<DocumentoCompra>().Property(x => x.Serie).HasColumnName("serie");
        mb.Entity<DocumentoCompra>().Property(x => x.Numero).HasColumnName("numero");
        mb.Entity<DocumentoCompra>().Property(x => x.FechaEmision).HasColumnName("fecha_emision");
        mb.Entity<DocumentoCompra>().Property(x => x.Moneda).HasColumnName("moneda");
        mb.Entity<DocumentoCompra>().Property(x => x.Subtotal).HasColumnName("subtotal");
        mb.Entity<DocumentoCompra>().Property(x => x.Impuesto).HasColumnName("impuesto");
        mb.Entity<DocumentoCompra>().Property(x => x.Total).HasColumnName("total");

        mb.Entity<MovimientoInventario>().ToTable("movimiento_inventario").HasKey(x => x.IdMov);
        mb.Entity<MovimientoInventario>().Property(x => x.IdMov).HasColumnName("id_mov");
        mb.Entity<MovimientoInventario>().Property(x => x.IdAlmacen).HasColumnName("id_almacen");
        mb.Entity<MovimientoInventario>().Property(x => x.IdProducto).HasColumnName("id_producto");
        mb.Entity<MovimientoInventario>().Property(x => x.Fecha).HasColumnName("fecha");
        mb.Entity<MovimientoInventario>().Property(x => x.Tipo).HasColumnName("tipo");
        mb.Entity<MovimientoInventario>().Property(x => x.Referencia).HasColumnName("referencia");
        mb.Entity<MovimientoInventario>().Property(x => x.Cantidad).HasColumnName("cantidad");
        mb.Entity<MovimientoInventario>().Property(x => x.CostoUnitario).HasColumnName("costo_unitario");
        mb.Entity<MovimientoInventario>().Property(x => x.Observacion).HasColumnName("observacion");

        mb.Entity<CostoProducto>().ToTable("costo_producto").HasKey(x => new { x.IdProducto, x.IdAlmacen });
        mb.Entity<CostoProducto>().Property(x => x.IdProducto).HasColumnName("id_producto");
        mb.Entity<CostoProducto>().Property(x => x.IdAlmacen).HasColumnName("id_almacen");
        mb.Entity<CostoProducto>().Property(x => x.CostoPromedio).HasColumnName("costo_promedio");
    }
}
